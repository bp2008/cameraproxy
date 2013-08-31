using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Web;
using System.Text;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

// Modified Nov 10, 2012 by Brian Pearce

namespace MJpegCameraProxy
{
	public class HttpProcessor
	{
		public TcpClient socket;
		public HttpServer srv;

		private Stream inputStream;
		/// <summary>
		/// Be careful to flush each output stream before using a different one!!
		/// </summary>
		public StreamWriter outputStream;
		/// <summary>
		/// Be careful to flush each output stream before using a different one!!
		/// </summary>
		public BufferedStream rawOutputStream;

		public Cookies requestCookies;
		public Cookies responseCookies = new Cookies();

		public String http_method;
		public String http_url;
		public String http_url_before_querystring;
		public String http_protocol_versionstring;
		public Hashtable httpHeaders = new Hashtable();

		public SortedList<string, string> PostParams = new SortedList<string, string>();
		public SortedList<string, string> QueryString = new SortedList<string, string>();

		private bool responseWritten = false;

		private const int BUF_SIZE = 4096;

		private int isLocalConnection = -1;
		public bool IsLocalConnection
		{
			get
			{
				if (isLocalConnection == -1)
				{
					try
					{
						if (socket != null)
						{
							if (socket.Client.RemoteEndPoint.ToString().StartsWith("192.168."))
								isLocalConnection = 1;
						}
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				return isLocalConnection == 1;
			}
		}

		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

		public HttpProcessor(TcpClient s, HttpServer srv)
		{
			this.socket = s;
			this.srv = srv;
		}

		private string streamReadLine(Stream inputStream)
		{
			int next_char;
			StringBuilder data = new StringBuilder();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1) { break; };
				data.Append(Convert.ToChar(next_char));
			}
			return data.ToString();
		}

		public void process(object objParameter)
		{
			try
			{
				// we can't use a StreamReader for input, because it buffers up extra data on us inside it's
				// "processed" view of the world, and we want the data raw after the headers
				inputStream = new BufferedStream(socket.GetStream());

				// we probably shouldn't be using a streamwriter for all output from handlers either
				rawOutputStream = new BufferedStream(socket.GetStream());
				outputStream = new StreamWriter(rawOutputStream);
				try
				{
					parseRequest();
					readHeaders();
					QueryString = ParseQueryStringArguments(this.http_url);
					requestCookies = Cookies.FromString(httpHeaders.ContainsKey("Cookie") ? httpHeaders["Cookie"].ToString() : "");
					if (http_method.Equals("GET"))
					{
						handleGETRequest();
					}
					else if (http_method.Equals("POST"))
					{
						handlePOSTRequest();
					}
				}
				catch (Exception e)
				{
					Logger.Special(LogType.HttpServer, "Exception: " + e.ToString());
					writeFailure();
				}
				outputStream.Flush();
				rawOutputStream.Flush();
				// bs.Flush(); // flush any remaining output
				inputStream = null; outputStream = null; // bs = null;            
				socket.Close();
			}
			catch (Exception ex)
			{
				if (ex is IOException)
				{
					if (ex.InnerException != null && ex.InnerException is SocketException)
					{
						if (ex.InnerException.Message == "An established connection was aborted by the software in your host machine"
							|| ex.InnerException.Message == "An existing connection was forcibly closed by the remote host")
							return; // Connection aborted by client.
					}
				}
				Logger.Debug(ex);
			}
		}

		//private NetworkCredential ParseAuthorizationCredentials()
		//{
		//    string auth = this.httpHeaders["Authorization"].ToString();
		//    if (auth != null && auth.StartsWith("Basic "))
		//    {
		//        byte[] bytes =  System.Convert.FromBase64String(auth.Substring(6));
		//        string creds = ASCIIEncoding.ASCII.GetString(bytes);

		//    }
		//    return new NetworkCredential();
		//}

		public void parseRequest()
		{
			String request = streamReadLine(inputStream);
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3)
			{
				throw new Exception("invalid http request line: " + request);
			}
			http_method = tokens[0].ToUpper();
			http_url = tokens[1];
			int idxQmark = http_url.IndexOf('?');
			if (idxQmark > -1)
				http_url_before_querystring = http_url.Substring(0, idxQmark);
			else
				http_url_before_querystring = http_url;
			http_protocol_versionstring = tokens[2];

			Logger.Special(LogType.HttpServer, "starting: " + request);
		}

		public void readHeaders()
		{
			Logger.Special(LogType.HttpServer, "readHeaders()");
			String line;
			while ((line = streamReadLine(inputStream)) != null)
			{
				if (line.Equals(""))
				{
					Logger.Special(LogType.HttpServer, "got headers");
					return;
				}

				int separator = line.IndexOf(':');
				if (separator == -1)
				{
					throw new Exception("invalid http header line: " + line);
				}
				String name = line.Substring(0, separator);
				int pos = separator + 1;
				while ((pos < line.Length) && (line[pos] == ' '))
				{
					pos++; // strip any spaces
				}

				string value = line.Substring(pos, line.Length - pos);
				Logger.Special(LogType.HttpServer, "header: " + name + ":" + value);
				httpHeaders[name] = value;
			}
		}

		public void handleGETRequest()
		{
			srv.handleGETRequest(this);
			if (!responseWritten)
				this.writeFailure();
		}
		public void handlePOSTRequest()
		{
			// this post data processing just reads everything into a memory stream.
			// this is fine for smallish things, but for large stuff we should really
			// hand an input stream to the request processor. However, the input stream 
			// we hand him needs to let him see the "end of the stream" at this content 
			// length, because otherwise he won't know when he's seen it all! 

			Logger.Special(LogType.HttpServer, "get post data start");
			int content_len = 0;
			MemoryStream ms = new MemoryStream();
			if (this.httpHeaders.ContainsKey("Content-Length"))
			{
				content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
				if (content_len > MAX_POST_SIZE)
				{
					throw new Exception(
						String.Format("POST Content-Length({0}) too big for this simple server",
						  content_len));
				}
				byte[] buf = new byte[BUF_SIZE];
				int to_read = content_len;
				while (to_read > 0)
				{
					Logger.Special(LogType.HttpServer, "starting Read, to_read=" + to_read);

					int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
					Logger.Special(LogType.HttpServer, "read finished, numread=" + numread);
					if (numread == 0)
					{
						if (to_read == 0)
						{
							break;
						}
						else
						{
							throw new Exception("client disconnected during post");
						}
					}
					to_read -= numread;
					ms.Write(buf, 0, numread);
				}
				ms.Seek(0, SeekOrigin.Begin);
			}
			Logger.Special(LogType.HttpServer, "get post data end");

			string contentType = httpHeaders.ContainsKey("Content-Type") ? this.httpHeaders["Content-Type"].ToString() : null;
			if (contentType != null && contentType.Contains("application/x-www-form-urlencoded"))
			{
				StreamReader sr = new StreamReader(ms);
				string all = sr.ReadToEnd();
				sr.Close();
				PostParams = ParseQueryStringArguments(all, false);
				srv.handlePOSTRequest(this, null);
			}
			else
			{
				srv.handlePOSTRequest(this, new StreamReader(ms));
			}
		}

		public void writeSuccess(string contentType = "text/html", int contentLength = -1, bool closeConnection = true)
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.0 200 OK");
			if (!string.IsNullOrEmpty(contentType))
				outputStream.WriteLine("Content-Type: " + contentType);
			if (contentLength > -1)
				outputStream.WriteLine("Content-Length: " + contentLength);
			string cookieStr = responseCookies.ToString();
			if (!string.IsNullOrEmpty(cookieStr))
				outputStream.WriteLine(cookieStr);
			if (closeConnection)
				outputStream.WriteLine("Connection: close");
			outputStream.WriteLine("");
		}

		public void writeFailure()
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.0 404 File not found");
			outputStream.WriteLine("Connection: close");
			outputStream.WriteLine("");
			outputStream.WriteLine("404 Not Found");
		}

		public void writeRedirect(string redirectToUrl)
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.0 302 Found");
			outputStream.WriteLine("Location: " + redirectToUrl);
		}
		#region Parameter parsing
		public static SortedList<string, string> ParseQueryStringArguments(string queryString, bool requireQuestionMark = true)
		{
			SortedList<string, string> arguments = new SortedList<string, string>();
			int idx = queryString.IndexOf('?');
			if (idx > -1)
				queryString = queryString.Substring(idx + 1);
			else if (requireQuestionMark)
				return arguments;
			idx = queryString.LastIndexOf('#');
			string hash = null;
			if (idx > -1)
			{
				hash = queryString.Substring(idx + 1);
				queryString = queryString.Remove(idx);
			}
			string[] parts = queryString.Split(new char[] { '&' });
			for (int i = 0; i < parts.Length; i++)
			{
				string[] argument = parts[i].Split(new char[] { '=' });
				if (argument.Length == 2)
				{
					string key = HttpUtility.UrlDecode(argument[0]).ToLower();
					string existingValue;
					if (arguments.TryGetValue(key, out existingValue))
						arguments[key] += "," + HttpUtility.UrlDecode(argument[1]);
					else
						arguments[key] = HttpUtility.UrlDecode(argument[1]);
				}
			}
			if (hash != null)
				arguments["#"] = hash;
			return arguments;
		}
		public string GetParam(string key)
		{
			return GetQSParam(key);
		}
		public int GetIntParam(string key, int defaultValue = 0)
		{
			return GetQSIntParam(key, defaultValue);
		}
		public double GetDoubleParam(string key, int defaultValue = 0)
		{
			return GetQSDoubleParam(key, defaultValue);
		}
		public bool GetBoolParam(string key)
		{
			return GetQSBoolParam(key);
		}
		public string GetQSParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (QueryString.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		public int GetQSIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		public double GetQSDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		public bool GetQSBoolParam(string key)
		{
			string param = GetQSParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		public string GetPostParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (PostParams.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		public int GetPostIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		public double GetPostDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		public bool GetPostBoolParam(string key)
		{
			string param = GetPostParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		#endregion
	}

	public abstract class HttpServer
	{

		protected int port;
		TcpListener listener;
		protected bool is_active = true;

		public HttpServer(int port)
		{
			this.port = port;
		}

		int errorCount = 0;
		public void listen()
		{
			try
			{
				listener = new TcpListener(IPAddress.Any, port);
				listener.Start();
				while (is_active)
				{
					try
					{
						TcpClient s = listener.AcceptTcpClient();
						int workerThreads, completionPortThreads;
						ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
						if (workerThreads > 0)
						{
							HttpProcessor processor = new HttpProcessor(s, this);
							ThreadPool.QueueUserWorkItem(processor.process, processor);
						}
						else
						{
							StreamWriter outputStream = new StreamWriter(s.GetStream());
							outputStream.WriteLine("HTTP/1.0 503 Service Unavailable");
							outputStream.WriteLine("Connection: close");
							outputStream.WriteLine("");
							outputStream.WriteLine("Server too busy");
						}
					}
					catch (Exception ex)
					{
						if (++errorCount > 100)
							throw ex;
						Logger.Debug(ex, "Error count: " + errorCount);
					}
				}
			}
			catch (Exception)
			{
			}
			if (listener != null)
				listener.Stop();
		}

		public void stop()
		{
			try
			{
				is_active = false;
				if (listener != null)
					listener.Stop();
				stopServer();
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		public abstract void handleGETRequest(HttpProcessor p);
		public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
		public abstract void stopServer();
	}
}




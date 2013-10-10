using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

// This file has been modified continuously since Nov 10, 2012 by Brian Pearce.
// Based on http://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/


namespace SimpleHttp
{
	public class HttpProcessor
	{
		private const int BUF_SIZE = 4096;
		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

		#region Fields and Properties
		/// <summary>
		/// The underlying tcpClient which handles the network connection.
		/// </summary>
		public TcpClient tcpClient;

		/// <summary>
		/// The HttpServer instance that accepted this request.
		/// </summary>
		public HttpServer srv;

		private Stream inputStream;

		/// <summary>
		/// Be careful to flush each output stream before using a different one!!
		/// 
		/// This stream is for writing text data.
		/// </summary>
		public StreamWriter outputStream;
		/// <summary>
		/// Be careful to flush each output stream before using a different one!!
		/// 
		/// This stream is for writing binary data.
		/// </summary>
		public BufferedStream rawOutputStream;

		/// <summary>
		/// The cookies sent by the remote client.
		/// </summary>
		public Cookies requestCookies;

		/// <summary>
		/// The cookies to send to the remote client.
		/// </summary>
		public Cookies responseCookies = new Cookies();

		/// <summary>
		/// The Http method used.  i.e. "POST" or "GET"
		/// </summary>
		public string http_method;
		/// <summary>
		/// The base Uri for this server, containing its host name and port.
		/// </summary>
		public Uri base_uri_this_server;
		/// <summary>
		/// The requested url.
		/// </summary>
		public Uri request_url;
		/// <summary>
		/// The protocol version string sent by the client.  e.g. "HTTP/1.1"
		/// </summary>
		public string http_protocol_versionstring;
		/// <summary>
		/// The path to and name of the requested page, not including the first '/'
		/// 
		/// For example, if the URL was "/articles/science/moon.html?date=2011-10-21", requestedPage would be "articles/science/moon.html"
		/// </summary>
		public string requestedPage;
		/// <summary>
		/// A string array containing the directories and the page name.
		/// 
		/// For example, if the URL was "/articles/science/moon.html?date=2011-10-21", pathParts would be { "articles", "science", "moon.html" }
		/// </summary>
		public string[] pathParts;
		/// <summary>
		/// A Dictionary mapping http header names to values. Names are all converted to lower case before being added to this Dictionary.
		/// </summary>
		public Dictionary<string, string> httpHeaders = new Dictionary<string, string>();

		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> PostParams = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> RawPostParams = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2"
		/// </summary>
		public SortedList<string, string> QueryString = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2"
		/// </summary>
		public SortedList<string, string> RawQueryString = new SortedList<string, string>();

		/// <summary>
		/// A flag that is set when WriteSuccess(), WriteFailure(), or WriteRedirect() is called.  If the
		/// </summary>
		private bool responseWritten = false;

		/// <summary>
		/// An IP address that starts with these characters is considered local.  Example value: "192.168." 
		/// </summary>
		public string LocalAddressRangePrefix = "192.168.";

		private int isLocalConnection = -1;
		/// <summary>
		/// Returns true if the remote client's ip address starts with the [LocalAddressRangePrefix] field.
		/// </summary>
		public bool IsLocalConnection
		{
			get
			{
				if (isLocalConnection == -1)
				{
					string remoteIp = RemoteIPAddress;
					if (remoteIp.StartsWith(LocalAddressRangePrefix))
						isLocalConnection = 1;
					else if (remoteIp != "")
						isLocalConnection = 0;
				}
				return isLocalConnection == 1;
			}
		}

		private string remoteIPAddress = null;
		/// <summary>
		/// Returns the remote client's IP address, or an empty string if the remote IP address is somehow not available.
		/// </summary>
		public string RemoteIPAddress
		{
			get
			{
				if (!string.IsNullOrEmpty(remoteIPAddress))
					return remoteIPAddress;
				try
				{
					if (tcpClient != null)
						remoteIPAddress = tcpClient.Client.RemoteEndPoint.ToString().Split(':')[0];
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				return remoteIPAddress;
			}
		}

		public readonly bool secure_https;
		private X509Certificate2 ssl_certificate;
		#endregion

		public HttpProcessor(TcpClient s, HttpServer srv, X509Certificate2 ssl_certificate = null)
		{
			this.ssl_certificate = ssl_certificate;
			this.secure_https = ssl_certificate != null;
			this.tcpClient = s;
			this.base_uri_this_server = new Uri("http" + (this.secure_https ? "s" : "") + "://" + s.Client.LocalEndPoint.ToString(), UriKind.Absolute);
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

		/// <summary>
		/// Processes the request.
		/// </summary>
		internal void process(object objParameter)
		{
			Stream tcpStream = null;
			try
			{
				tcpStream = tcpClient.GetStream();
				if (this.secure_https)
				{
					try
					{
						tcpStream = new SslStream(tcpStream, false, null, null, EncryptionPolicy.RequireEncryption);
						((SslStream)tcpStream).AuthenticateAsServer(ssl_certificate);
					}
					catch (Exception ex)
					{
						SimpleHttpLogger.LogVerbose(ex);
						return;
					}
				}
				inputStream = new BufferedStream(tcpStream);
				rawOutputStream = new BufferedStream(tcpStream);
				outputStream = new StreamWriter(rawOutputStream);
				try
				{
					parseRequest();
					readHeaders();
					RawQueryString = ParseQueryStringArguments(this.request_url.Query, preserveKeyCharacterCase: true);
					QueryString = ParseQueryStringArguments(this.request_url.Query);
					requestCookies = Cookies.FromString(GetHeaderValue("Cookie", ""));
					try
					{
						if (http_method.Equals("GET"))
							handleGETRequest();
						else if (http_method.Equals("POST"))
							handlePOSTRequest();
					}
					catch (Exception e)
					{
						if (!isOrdinaryDisconnectException(e))
							SimpleHttpLogger.Log(e);
						writeFailure("500 Internal Server Error");
					}
				}
				catch (Exception e)
				{
					if (!isOrdinaryDisconnectException(e))
						SimpleHttpLogger.LogVerbose(e);
					this.writeFailure("400 Bad Request", "The request cannot be fulfilled due to bad syntax.");
				}
				outputStream.Flush();
				rawOutputStream.Flush();
				inputStream = null; outputStream = null; rawOutputStream = null;
			}
			catch (Exception ex)
			{
				if (!isOrdinaryDisconnectException(ex))
					SimpleHttpLogger.LogVerbose(ex);
			}
			finally
			{
				try
				{
					if (tcpClient != null)
						tcpClient.Close();
				}
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
				try
				{
					if (tcpStream != null)
						tcpStream.Close();
				}
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
			}
		}

		public bool isOrdinaryDisconnectException(Exception ex)
		{
			if (ex is IOException)
			{
				if (ex.InnerException != null && ex.InnerException is SocketException)
				{
					if (ex.InnerException.Message.Contains("An established connection was aborted by the software in your host machine")
						|| ex.InnerException.Message.Contains("An existing connection was forcibly closed by the remote host"))
						return true; // Connection aborted.  This happens often enough that reporting it can be excessive.
				}
			}
			return false;
		}
		// The following function was the start of an attempt to support basic authentication, but I have since decided against it as basic authentication is very insecure.
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

		/// <summary>
		/// Parses the first line of the http request to get the request method, url, and protocol version.
		/// </summary>
		private void parseRequest()
		{
			string request = streamReadLine(inputStream);
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3)
				throw new Exception("invalid http request line: " + request);
			http_method = tokens[0].ToUpper();

			if (tokens[1].StartsWith("http://") || tokens[1].StartsWith("https://"))
				request_url = new Uri(tokens[1]);
			else
				request_url = new Uri(base_uri_this_server, tokens[1]);

			http_protocol_versionstring = tokens[2];
		}

		/// <summary>
		/// Parses the http headers
		/// </summary>
		private void readHeaders()
		{
			String line;
			while ((line = streamReadLine(inputStream)) != "")
			{
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new Exception("invalid http header line: " + line);
				String name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces

				string value = line.Substring(pos, line.Length - pos);
				httpHeaders[name.ToLower()] = value;
			}
		}

		/// <summary>
		/// Asks the HttpServer to handle this request as a GET request.  If the HttpServer does not write a response code header, this will write a generic failure header.
		/// </summary>
		private void handleGETRequest()
		{
			try
			{
				srv.handleGETRequest(this);
			}
			finally
			{
				if (!responseWritten)
					this.writeFailure();
			}
		}
		/// <summary>
		/// This post data processing just reads everything into a memory stream.
		/// This is fine for smallish things, but for large stuff we should really
		/// hand an input stream to the request processor. However, the input stream 
		/// we hand to the user's code needs to see the "end of the stream" at this 
		/// content length, because otherwise it won't know where the end is!
		/// 
		/// If the HttpServer does not write a response code header, this will write a generic failure header.
		/// </summary>
		private void handlePOSTRequest()
		{
			try
			{
				int content_len = 0;
				MemoryStream ms = new MemoryStream();
				string content_length_str = GetHeaderValue("Content-Length");
				if (!string.IsNullOrWhiteSpace(content_length_str))
				{
					if (int.TryParse(content_length_str, out content_len))
					{
						if (content_len > MAX_POST_SIZE)
						{
							this.writeFailure("413 Request Entity Too Large", "Request Too Large");
							SimpleHttpLogger.LogVerbose("POST Content-Length(" + content_len + ") too big for this simple server.  Server can handle up to " + MAX_POST_SIZE);
							return;
						}
						byte[] buf = new byte[BUF_SIZE];
						int to_read = content_len;
						while (to_read > 0)
						{
							int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
							if (numread == 0)
							{
								if (to_read == 0)
									break;
								else
								{
									SimpleHttpLogger.LogVerbose("client disconnected during post");
									return;
								}
							}
							to_read -= numread;
							ms.Write(buf, 0, numread);
						}
						ms.Seek(0, SeekOrigin.Begin);
					}
				}
				else
				{
					this.writeFailure("411 Length Required", "The request did not specify the length of its content.");
					SimpleHttpLogger.LogVerbose("The request did not specify the length of its content.  This server requires that all POST requests include a Content-Length header.");
					return;
				}

				string contentType = GetHeaderValue("Content-Type");
				if (contentType != null && contentType.Contains("application/x-www-form-urlencoded"))
				{
					StreamReader sr = new StreamReader(ms);
					string all = sr.ReadToEnd();
					sr.Close();

					RawPostParams = ParseQueryStringArguments(all, false, true);
					PostParams = ParseQueryStringArguments(all, false);

					srv.handlePOSTRequest(this, null);
				}
				else
				{
					srv.handlePOSTRequest(this, new StreamReader(ms));
				}
			}
			finally
			{
				try
				{
					if (!responseWritten)
						this.writeFailure();
				}
				catch (Exception) { }
			}
		}

		/// <summary>
		/// Writes the response headers for a successful response.  Call this one time before writing your response, after you have determined that the request is valid.
		/// </summary>
		/// <param name="contentType">The MIME type of your response.</param>
		/// <param name="contentLength">(OPTIONAL) The length of your response, in bytes, if you know it.</param>
		public void writeSuccess(string contentType = "text/html", long contentLength = -1, string responseCode = "200 OK", List<KeyValuePair<string, string>> additionalHeaders = null)
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.1 " + responseCode);
			if (!string.IsNullOrEmpty(contentType))
				outputStream.WriteLine("Content-Type: " + contentType);
			if (contentLength > -1)
				outputStream.WriteLine("Content-Length: " + contentLength);
			string cookieStr = responseCookies.ToString();
			if (!string.IsNullOrEmpty(cookieStr))
				outputStream.WriteLine(cookieStr);
			if (additionalHeaders != null)
				foreach (KeyValuePair<string, string> header in additionalHeaders)
					outputStream.WriteLine(header.Key + ": " + header.Value);
			outputStream.WriteLine("Connection: close");
			outputStream.WriteLine("");
		}

		/// <summary>
		/// Writes a failure response header.  Call this one time to return an error response.
		/// </summary>
		/// <param name="code">(OPTIONAL) The http error code (including explanation entity).  For example: "404 Not Found" where 404 is the error code and "Not Found" is the explanation.</param>
		/// <param name="description">(OPTIONAL) A description string to send after the headers as the response.  This is typically shown to the remote user in his browser.  If null, the code string is sent here.  If "", no response body is sent by this function, and you may or may not want to write your own.</param>
		public void writeFailure(string code = "404 Not Found", string description = null)
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.1 " + code);
			outputStream.WriteLine("Connection: close");
			outputStream.WriteLine("");
			if (description == null)
				outputStream.WriteLine(code);
			else if (description != "")
				outputStream.WriteLine(description);
		}

		/// <summary>
		/// Writes a redirect header instructing the remote user's browser to load the URL you specify.  Call this one time and do not write any other data to the response stream.
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		public void writeRedirect(string redirectToUrl)
		{
			responseWritten = true;
			outputStream.WriteLine("HTTP/1.1 302 Found");
			outputStream.WriteLine("Location: " + redirectToUrl);
			outputStream.WriteLine("Connection: close");
			outputStream.WriteLine("");
		}

		/// <summary>
		/// Gets the value of the header, or null if the header does not exist.  The name is case insensitive.
		/// </summary>
		/// <param name="name">The case insensitive name of the header to get the value of.</param>
		/// <returns>The value of the header, or null if the header did not exist.</returns>
		public string GetHeaderValue(string name, string defaultValue = null)
		{
			name = name.ToLower();
			string value;
			if (!httpHeaders.TryGetValue(name, out value))
				value = defaultValue;
			return value;
		}

		#region Parameter parsing
		/// <summary>
		/// Parses the specified query string and returns a sorted list containing the arguments found in the specified query string.  Can also be used to parse the POST request body if the mimetype is "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="queryString"></param>
		/// <param name="requireQuestionMark"></param>
		/// <returns></returns>
		private static SortedList<string, string> ParseQueryStringArguments(string queryString, bool requireQuestionMark = true, bool preserveKeyCharacterCase = false)
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
					string key = HttpUtility.UrlDecode(argument[0]);
					if (!preserveKeyCharacterCase)
						key = key.ToLower();
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

		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetParam(string key)
		{
			return GetQSParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetIntParam(string key, int defaultValue = 0)
		{
			return GetQSIntParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetDoubleParam(string key, int defaultValue = 0)
		{
			return GetQSDoubleParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetBoolParam(string key)
		{
			return GetQSBoolParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetQSParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (QueryString.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetQSIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetQSDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetQSBoolParam(string key)
		{
			string param = GetQSParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetPostParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (PostParams.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetPostIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetPostDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
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
		/// <summary>
		/// If > -1, the server is listening for http connections on this port.
		/// </summary>
		protected readonly int port;
		/// <summary>
		/// If > -1, the server is listening for https connections on this port.
		/// </summary>
		protected readonly int secure_port;
		protected volatile bool stopRequested = false;
		private X509Certificate2 ssl_certificate;
		private Thread thrHttp;
		private Thread thrHttps;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="port">The port number on which to accept regular http connections. If -1, the server will not listen for http connections.</param>
		/// <param name="httpsPort">(Optional) The port number on which to accept https connections. If -1, the server will not listen for https connections.</param>
		/// <param name="cert">(Optional) Certificate to use for https connections.  If null and an httpsPort was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		public HttpServer(int port, int httpsPort = -1, X509Certificate2 cert = null)
		{
			this.port = port;
			this.secure_port = httpsPort;
			this.ssl_certificate = cert;

			if (this.port > 65535 || this.port < -1) this.port = -1;
			if (this.secure_port > 65535 || this.secure_port < -1) this.secure_port = -1;

			if (this.port > -1)
			{
				thrHttp = new Thread(listen);
				thrHttp.Name = "HttpServer Thread";
			}

			if (this.secure_port > -1)
			{
				if (ssl_certificate == null)
				{
					string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
					FileInfo fiExe = new FileInfo(exePath);
					FileInfo fiCert = new FileInfo(fiExe.Directory.FullName + "\\SimpleHttpServer-SslCert.pfx");
					if (fiCert.Exists)
						ssl_certificate = new X509Certificate2(fiCert.FullName, "N0t_V3ry-S3cure#lol");
					else
					{
						using (Pluralsight.Crypto.CryptContext ctx = new Pluralsight.Crypto.CryptContext())
						{
							ctx.Open();

							ssl_certificate = ctx.CreateSelfSignedCertificate(
								new Pluralsight.Crypto.SelfSignedCertProperties
								{
									IsPrivateKeyExportable = true,
									KeyBitLength = 4096,
									Name = new X500DistinguishedName("cn=localhost"),
									ValidFrom = DateTime.Today.AddDays(-1),
									ValidTo = DateTime.Today.AddYears(100),
								});

							byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, "N0t_V3ry-S3cure#lol");
							File.WriteAllBytes(fiCert.FullName, certData);
						}
					}
				}
				thrHttps = new Thread(listen);
				thrHttps.Name = "HttpsServer Thread";
			}
		}

		/// <summary>
		/// Listens for connections, somewhat robustly.  Does not return until the server is stopped or until more than 100 listener restarts occur in a single day.
		/// </summary>
		private void listen(object param)
		{
			bool isSecureListener = (bool)param;

			int errorCount = 0;
			DateTime lastError = DateTime.Now;

			TcpListener listener = null;

			while (!stopRequested)
			{
				bool threwExceptionOuter = false;
				try
				{
					listener = new TcpListener(IPAddress.Any, isSecureListener ? secure_port : port);
					listener.Start();
					while (!stopRequested)
					{
						int innerErrorCount = 0;
						DateTime innerLastError = DateTime.Now;
						try
						{
							TcpClient s = listener.AcceptTcpClient();
							int workerThreads, completionPortThreads;
							ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
							// Here is where we could enforce a minimum number of free pool threads, 
							// if we wanted to ensure better performance.
							if (workerThreads > 0)
							{
								HttpProcessor processor = new HttpProcessor(s, this, isSecureListener ? ssl_certificate : null);
								ThreadPool.QueueUserWorkItem(processor.process);
							}
							else
							{
								try
								{
									StreamWriter outputStream = new StreamWriter(s.GetStream());
									outputStream.WriteLine("HTTP/1.1 503 Service Unavailable");
									outputStream.WriteLine("Connection: close");
									outputStream.WriteLine("");
									outputStream.WriteLine("Server too busy");
								}
								catch (ThreadAbortException ex)
								{
									throw ex;
								}
							}
						}
						catch (ThreadAbortException ex)
						{
							throw ex;
						}
						catch (Exception ex)
						{
							if (DateTime.Now.Hour != innerLastError.Hour || DateTime.Now.DayOfYear != innerLastError.DayOfYear)
							{
								innerLastError = DateTime.Now;
								innerErrorCount = 0;
							}
							if (++innerErrorCount > 10)
								throw ex;
							SimpleHttpLogger.Log(ex, "Inner Error count this hour: " + innerErrorCount);
							Thread.Sleep(1);
						}
					}
				}
				catch (ThreadAbortException) { stopRequested = true; }
				catch (Exception ex)
				{
					if (DateTime.Now.DayOfYear != lastError.DayOfYear || DateTime.Now.Year != lastError.Year)
					{
						lastError = DateTime.Now;
						errorCount = 0;
					}
					if (++errorCount > 100)
						throw ex;
					SimpleHttpLogger.Log(ex, "Restarting listener. Error count today: " + errorCount);
					threwExceptionOuter = true;
				}
				finally
				{
					try
					{
						if (listener != null)
						{
							listener.Stop();
							if (threwExceptionOuter)
								Thread.Sleep(1000);
						}
					}
					catch (ThreadAbortException) { stopRequested = true; }
					catch (Exception) { }
				}
			}
		}

		/// <summary>
		/// Starts listening for connections.
		/// </summary>
		public void Start()
		{
			if (thrHttp != null)
				thrHttp.Start(false);
			if (thrHttps != null)
				thrHttps.Start(true);
		}

		/// <summary>
		/// Stops listening for connections.
		/// </summary>
		public void Stop()
		{
			if (stopRequested)
				return;
			stopRequested = true;
			if (thrHttp != null)
				try
				{
					thrHttp.Abort();
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			if (thrHttps != null)
				try
				{
					thrHttps.Abort();
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			try
			{
				stopServer();
			}
			catch (Exception ex)
			{
				SimpleHttpLogger.Log(ex);
			}
		}

		/// <summary>
		/// Blocks the calling thread until the http listening threads finish or the timeout expires.  Call this after calling Stop() if you need to wait for the listener to clean up, such as if you intend to start another instance of the server using the same port(s).
		/// </summary>
		/// <param name="timeout_milliseconds">Maximum number of milliseconds to wait for the HttpServer Threads to stop.</param>
		public void Join(int timeout_milliseconds = 2000)
		{
			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			int timeToWait = timeout_milliseconds;
			stopwatch.Start();
			if (timeToWait > 0)
			{
				try
				{
					if (thrHttp != null && thrHttp.IsAlive)
						thrHttp.Join(timeToWait);
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			}
			stopwatch.Stop();
			timeToWait = timeout_milliseconds - (int)stopwatch.ElapsedMilliseconds;
			if (timeToWait > 0)
			{
				try
				{
					if (thrHttps != null && thrHttps.IsAlive)
						thrHttps.Join(timeToWait);
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			}
		}

		/// <summary>
		/// Handles an Http GET request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		public abstract void handleGETRequest(HttpProcessor p);
		/// <summary>
		/// Handles an Http POST request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		/// <param name="inputData">The input stream.  If the request's MIME type was "application/x-www-form-urlencoded", the StreamReader will be null and you can obtain the parameter values using p.PostParams, p.GetPostParam(), p.GetPostIntParam(), etc.</param>
		public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
		/// <summary>
		/// This is called when the server is stopping.  Perform any cleanup work here.
		/// </summary>
		public abstract void stopServer();
	}
	#region Helper Classes
	public class Cookie
	{
		public string name;
		public string value;
		public TimeSpan expire;

		public Cookie(string name, string value, TimeSpan expire)
		{
			this.name = name;
			this.value = value;
			this.expire = expire;
		}
	}
	public class Cookies
	{
		SortedList<string, Cookie> cookieCollection = new SortedList<string, Cookie>();
		/// <summary>
		/// Adds a cookie with the specified name and value.  The cookie is set to expire immediately at the end of the browsing session.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		public void Add(string name, string value)
		{
			Add(name, value, TimeSpan.Zero);
		}
		/// <summary>
		/// Adds a cookie with the specified name, value, and lifespan.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		/// <param name="expireTime">The amount of time before the cookie should expire.</param>
		public void Add(string name, string value, TimeSpan expireTime)
		{
			if (name == null)
				return;
			name = name.ToLower();
			cookieCollection[name] = new Cookie(name, value, expireTime);
		}
		/// <summary>
		/// Gets the cookie with the specified name.  If the cookie is not found, null is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public Cookie Get(string name)
		{
			Cookie cookie;
			if (!cookieCollection.TryGetValue(name, out cookie))
				cookie = null;
			return cookie;
		}
		/// <summary>
		/// Gets the value of the cookie with the specified name.  If the cookie is not found, an empty string is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public string GetValue(string name)
		{
			Cookie cookie = Get(name);
			if (cookie == null)
				return "";
			return cookie.value;
		}
		/// <summary>
		/// Returns a string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".
		/// </summary>
		/// <returns>A string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".</returns>
		public override string ToString()
		{
			List<string> cookiesStr = new List<string>();
			foreach (Cookie cookie in cookieCollection.Values)
				cookiesStr.Add("Set-Cookie: " + cookie.name + "=" + cookie.value + (cookie.expire == TimeSpan.Zero ? "" : "; Max-Age=" + (long)cookie.expire.TotalSeconds) + "; Path=/");
			return string.Join("\r\n", cookiesStr);
		}
		/// <summary>
		/// Returns a Cookies instance populated by parsing the specified string.  The string should be the value of the "Cookie" header that was received from the remote client.  If the string is null or empty, an empty cookies collection is returned.
		/// </summary>
		/// <param name="str">The value of the "Cookie" header sent by the remote client.</param>
		/// <returns></returns>
		public static Cookies FromString(string str)
		{
			Cookies cookies = new Cookies();
			if (str == null)
				return cookies;
			str = HttpUtility.UrlDecode(str);
			string[] parts = str.Split(';');
			for (int i = 0; i < parts.Length; i++)
			{
				int idxEquals = parts[i].IndexOf('=');
				if (idxEquals < 1)
					continue;
				string name = parts[i].Substring(0, idxEquals).Trim();
				string value = parts[i].Substring(idxEquals + 1).Trim();
				cookies.Add(name, value);
			}
			return cookies;
		}
	}
	public static class Extensions
	{
		/// <summary>
		/// Returns the date and time formatted for insertion as the expiration date in a "Set-Cookie" header.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static string ToCookieTime(this DateTime time)
		{
			return time.ToString("dd MMM yyyy hh:mm:ss GMT");
		}
	}
	/// <summary>
	/// A class which handles error logging by the http server.  It allows you to (optionally) register an ILogger instance to use for logging.
	/// </summary>
	public static class SimpleHttpLogger
	{
		private static ILogger logger = null;
		private static bool logVerbose = false;
		/// <summary>
		/// (OPTIONAL) Keeps a static reference to the specified ILogger and uses it for http server error logging.  Only one logger can be registered at a time; attempting to register a second logger simply replaces the first one.
		/// </summary>
		/// <param name="loggerToRegister">The logger that should be used when an error message needs logged.  If null, logging will be disabled.</param>
		/// <param name="logVerboseMessages">If true, additional error reporting will be enabled.  These errors include things that can occur frequently during normal operation, so it may be spammy.</param>
		public static void RegisterLogger(ILogger loggerToRegister, bool logVerboseMessages = false)
		{
			logger = loggerToRegister;
			logVerbose = logVerboseMessages;
		}
		/// <summary>
		/// Unregisters the currently registered logger (if any) by calling RegisterLogger(null);
		/// </summary>
		public static void UnregisterLogger()
		{
			RegisterLogger(null);
		}
		internal static void Log(Exception ex, string additionalInformation = "")
		{
			try
			{
				if (logger != null)
					logger.Log(ex, additionalInformation);
			}
			catch (Exception) { }
		}
		internal static void Log(string str)
		{
			try
			{
				if (logger != null)
				{
					logger.Log(str);
				}
			}
			catch (Exception) { }
		}

		internal static void LogVerbose(Exception ex, string additionalInformation = "")
		{
			if (logVerbose)
				Log(ex, additionalInformation);
		}

		internal static void LogVerbose(string str)
		{
			if (logVerbose)
				Log(str);
		}
	}
	/// <summary>
	/// An interface which handles logging of exceptions and strings.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Log an exception, possibly with additional information provided to assist with debugging.
		/// </summary>
		/// <param name="ex">An exception that was caught.</param>
		/// <param name="additionalInformation">Additional information about the exception.</param>
		void Log(Exception ex, string additionalInformation = "");
		/// <summary>
		/// Log a string.
		/// </summary>
		/// <param name="str">A string to log.</param>
		void Log(string str);
	}
	#endregion
}
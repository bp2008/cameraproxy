using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Drawing;
using System.IO;
using System.Threading;

namespace MJpegCameraProxy
{
	public class SimpleProxy
	{
		/// <summary>
		/// Gets data from a URL and returns it as a byte array.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static byte[] GetData(string url, string user = "", string password = "", bool keepAlive = false, bool allowErrorLogging = true)
		{
			try
			{
				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
				webRequest.Proxy = null;
				webRequest.KeepAlive = keepAlive;

				if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password))
				{
					string authInfo = user + ":" + password;
					authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
					webRequest.Headers["Authorization"] = "Basic " + authInfo;
				}
				webRequest.Method = "GET";
				webRequest.Timeout = 5000;
				return GetResponse(webRequest);
			}
			catch (ThreadAbortException ex) { throw ex; }
			catch (Exception ex)
			{
				if (allowErrorLogging)
					Logger.Debug(ex, "SimpleProxy URL: " + url);
			}
			return new byte[0];
		}
		private static byte[] GetResponse(HttpWebRequest webRequest)
		{
			byte[] data;
			using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
			{
				using (MemoryStream ms = new MemoryStream())
				{
					using (Stream responseStream = webResponse.GetResponseStream())
					{
						// Dump the response stream into the MemoryStream ms
						int bytesRead = 1;
						while (bytesRead > 0)
						{
							byte[] buffer = new byte[8000];
							bytesRead = responseStream.Read(buffer, 0, buffer.Length);
							if (bytesRead > 0)
								ms.Write(buffer, 0, bytesRead);
						}
						data = new byte[ms.Length];

						// Dump the data into the byte array
						ms.Seek(0, SeekOrigin.Begin);
						ms.Read(data, 0, data.Length);
						responseStream.Close();
					}
				}
				webResponse.Close();
			}
			return data;
		}
	}
}

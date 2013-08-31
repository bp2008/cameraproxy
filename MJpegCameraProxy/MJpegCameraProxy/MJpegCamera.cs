using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace MJpegCameraProxy
{
	public class MJpegCamera : IPCameraBase
	{
		public string url, user, pass, ip;
		public MJpegCamera(string url, string user, string pass)
		{
			this.url = url;
			this.user = user;
			this.pass = pass;
			Match m = Regex.Match(url, "://(.*?)/");
			if (m.Success)
				ip = m.Groups[1].Value;
			else
				ip = "";
		}

		protected override void DoBackgroundWork()
		{
			/*
			 * An MJPEG stream is basically a stream that starts with a non-standard boundary string and then contains a never ending series of http headers followed by jpeg data followed by everything again (starting with the boundary string).  There is usually a bit of \r\n padding thrown in after the jpeg data and before the boundary string starts again.
			 * */
			while (!Exit)
			{
				try
				{
					WebRequest request = HttpWebRequest.Create(url);
					request.Proxy = null;
					if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(pass))
						request.Credentials = new NetworkCredential(user, pass);
					WebResponse response = request.GetResponse();
					// BOUNDARY is implemented wrong by some cameras
					//string boundary = response.Headers["boundary"];
					//if (boundary == null)
					//    throw new MJpegStreamProblemException("Boundary header not found!");
					using (Stream s = response.GetResponseStream())
					{
						// Now start looping, saving images from the stream.
						int read;

						while (!Exit)
						{
							StringBuilder sb = new StringBuilder();
							// BOUNDARY is implemented wrong by some cameras
							// Read through the next boundary string
							//ReadUntilCompleteStringFound(boundary, s, ref sb);
							//sb = new StringBuilder();

							// Read up to the content length header
							ReadUntilCompleteStringFound("Content-Length: ", s, ref sb);

							sb = new StringBuilder();
							ReadUntilCharFound('\r', s, ref sb);

							string contentLengthStr = sb.ToString().Trim();
							int jpegLength;
							if (!int.TryParse(contentLengthStr, out jpegLength))
								throw new MJpegStreamProblemException("Content-Length was " + contentLengthStr);

							if (Exit)
								return;

							// Keep reading chars one by one until we reach the end of a "\r\n\r\n" (indicating the end of the http headers).
							sb = new StringBuilder();
							sb.Append('\r'); // We already consumed a \r character after reading the length of the image.
							ReadUntilCompleteStringFound("\r\n\r\n", s, ref sb);

							// Now the jpeg data begins, and it has length jpegLength
							byte[] jpegBuffer = new byte[jpegLength];
							read = 0;
							while (read < jpegLength)
								read += s.Read(jpegBuffer, read, jpegBuffer.Length - read);

							lastFrame = jpegBuffer;
						}
					}
				}
				catch (MJpegStreamProblemException ex)
				{
					Logger.Debug(ex);
				}
				catch (ThreadAbortException)
				{
					return;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
				Thread.Sleep(5000);
			}
		}

		private void ReadUntilCharFound(char c, Stream s, ref StringBuilder sb)
		{
			int tempInt;
			char lastChar = ' ';
			do
			{
				tempInt = s.ReadByte();
				if (tempInt == -1)
					throw new MJpegStreamProblemException("EOF reading header data");
				lastChar = (char)tempInt;
				sb.Append(lastChar);
			}
			while (lastChar != c);
		}

		private void ReadUntilCompleteStringFound(string until, Stream s, ref StringBuilder sb, StringComparison stringComparer = StringComparison.OrdinalIgnoreCase)
		{
			int tempInt;
			do
			{
				tempInt = s.ReadByte();
				if (tempInt == -1)
					throw new MJpegStreamProblemException("EOF reading header data");
				sb.Append((char)tempInt);
			}
			while (!sb.ToString().EndsWith(until, stringComparer));
		}
	}
}

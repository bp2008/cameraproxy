using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using SimpleHttp;

namespace MJpegCameraProxy
{
	public class MJpegServer : HttpServer
	{
		public static CameraManager cm = new CameraManager();
		SessionManager sm = new SessionManager();
		public MJpegServer(int port)
			: base(port)
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				int idxFirstSlash = p.http_url.IndexOf('/');
				if (idxFirstSlash == -1)
				{
					p.writeFailure();
					return;
				}
				string requestedPage = p.http_url_before_querystring.Substring(idxFirstSlash + 1);

				if (requestedPage.StartsWith("Images/"))
				{
					byte[] bytes = Images.GetImage(requestedPage.Substring("Images/".Length));
					p.writeSuccess("image/png", bytes.Length);
					p.outputStream.Flush();
					p.rawOutputStream.Write(bytes, 0, bytes.Length);
					return;
				}
				else if (requestedPage == "Scripts/jquery.js")
				{
					p.writeSuccess("text/javascript");
					p.outputStream.Write(Javascript.JQuery());
					return;
				}
				else if (requestedPage == "Scripts/sha1.js")
				{
					p.writeSuccess("text/javascript");
					p.outputStream.Write(Javascript.Sha_1());
					return;
				}

				Session s = sm.GetSession(p.requestCookies.GetValue("session"), p.requestCookies.GetValue("auth"));
				if (s != null)
					p.responseCookies.Add("session", s.sid, TimeSpan.FromMinutes(20));

				if (requestedPage.EndsWith(".jpg"))
				{
					string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
					if (cm.IsPrivate(cameraId) && s == null)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(Login.GetString(p.http_url));
						return;
					}
					byte[] latestImage = cm.GetLatestImage(cameraId);
					latestImage = ImageConverter.HandleRequestedConversionIfAny(latestImage, p);
					p.writeSuccess("image/jpeg", latestImage.Length);
					p.outputStream.Flush();
					p.rawOutputStream.Write(latestImage, 0, latestImage.Length);
				}
				else if (requestedPage.EndsWith(".mjpg"))
				{
					string cameraId = requestedPage.Substring(0, requestedPage.Length - 5);
					if (cm.IsPrivate(cameraId) && s == null)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(Login.GetString(p.http_url));
						return;
					}
					if (cm.GetLatestImage(cameraId).Length == 0)
						return;
					p.writeSuccess("multipart/x-mixed-replace;boundary=ipcamera");
					byte[] newImage;
					byte[] lastImage = null;
					while (!this.stopRequested)
					{
						try
						{
							newImage = cm.GetLatestImage(cameraId);
							while (newImage == lastImage)
							{
								Thread.Sleep(1);
								newImage = cm.GetLatestImage(cameraId);
							}
							lastImage = newImage;

							byte[] sendImage = ImageConverter.HandleRequestedConversionIfAny(newImage, p);

							p.outputStream.WriteLine("--ipcamera");
							p.outputStream.WriteLine("Content-Type: image/jpeg");
							p.outputStream.WriteLine("Content-Length: " + sendImage.Length);
							p.outputStream.WriteLine();
							p.outputStream.Flush();
							p.rawOutputStream.Write(sendImage, 0, sendImage.Length);
							p.rawOutputStream.Flush();
							p.outputStream.WriteLine();
						}
						catch (Exception)
						{
							break;
						}
					}
				}
				else if (requestedPage.EndsWith(".cam"))
				{
					string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
					if (cm.IsPrivate(cameraId) && s == null)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(Login.GetString(p.http_url));
						return;
					}

					string userAgent = p.httpHeaders.ContainsKey("User-Agent") ? p.httpHeaders["User-Agent"].ToString() : "";
					bool isMobile = userAgent.Contains("iPad") || userAgent.Contains("iPhone") || userAgent.Contains("Android") || userAgent.Contains("BlackBerry");

					bool isLocalConnection = p == null ? false : p.IsLocalConnection;
					int defaultRefresh = isLocalConnection && !isMobile ? -1 : 250;
					string html = CamPage.GetHtml(cameraId, !isMobile, p.GetIntParam("refresh", defaultRefresh), p.GetBoolParam("override") ? -1 : 600000, p);
					if (string.IsNullOrEmpty(html) || html == "NO")
					{
						p.writeFailure();
						return;
					}
					p.writeSuccess("text/html");
					p.outputStream.Write(html);
				}
				else if (requestedPage == "PTZ")
				{
					string cameraId = p.GetParam("id");
					if (cm.IsPrivate(cameraId) && s == null)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(Login.GetString(p.http_url));
						return;
					}
					PTZ.RunCommand(cameraId, p.GetParam("cmd"));
					p.writeSuccess("text/plain");
				}
				else if (requestedPage == "PTZPRESETIMG")
				{
					string cameraId = p.GetParam("id");
					if (cm.GetCamera(cameraId) != null)
					{
						int index = p.GetIntParam("index", -1);
						if (index > -1)
						{
							string fileName = Globals.ThumbsDirectoryBase + cameraId + index + ".jpg";
							if (!cm.IsPrivate(cameraId) || s != null)
							{
								if (File.Exists(fileName))
								{
									byte[] bytes = File.ReadAllBytes(fileName);
									p.writeSuccess("image/jpg", bytes.Length);
									p.outputStream.Flush();
									p.rawOutputStream.Write(bytes, 0, bytes.Length);
									return;
								}
							}
						}
					}
					{ // Failed to get image thumbnail
						byte[] bytes = Images.GetImage("qmark.png");
						p.writeSuccess("image/png", bytes.Length);
						p.outputStream.Flush();
						p.rawOutputStream.Write(bytes, 0, bytes.Length);
						return;
					}
				}
				else if (requestedPage.EndsWith(".html") || requestedPage.EndsWith(".htm"))
				{
					HtmlResponse html = Html.GetHtml(requestedPage, s, p);
					if (html.errorType == Html.ErrorType.NoError)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(html.html);
					}
					else if (html.errorType == Html.ErrorType.BadFile)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write("Bad File");
					}
					else if (html.errorType == Html.ErrorType.NoAuth)
					{
						p.writeSuccess("text/html");
						p.outputStream.Write(Login.GetString(p.http_url));
					}
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				int idxFirstSlash = p.http_url.IndexOf('/');
				if (idxFirstSlash == -1)
				{
					p.writeFailure();
					return;
				}
				string requestedPage = p.http_url.Substring(idxFirstSlash + 1);
				if (string.IsNullOrEmpty(requestedPage))
				{

				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		public override void stopServer()
		{
			cm.Stop();
		}
	}
}
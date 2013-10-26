using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using SimpleHttp;
using System.Text.RegularExpressions;
using System.Web;

namespace MJpegCameraProxy
{
	public class MJpegServer : HttpServer
	{
		public static CameraManager cm = new CameraManager();
		public static SessionManager sm = new SessionManager();
		public MJpegServer(int port, int port_https)
			: base(port, port_https)
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPage = p.request_url.AbsolutePath.TrimStart('/');

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
				else if (requestedPage == "Scripts/TableSorter.js")
				{
					p.writeSuccess("text/javascript");
					p.outputStream.Write(Javascript.TableSorter());
					return;
				}
				else if (requestedPage == "Scripts/jquery_ui_1_10_2_custom_min.js")
				{
					p.writeSuccess("text/javascript");
					p.outputStream.Write(Pages.Resource.getStringResource("jquery_ui_1_10_2_custom_min.js"));
					return;
				}
				else if (requestedPage == "Styles/jquery_ui_1_10_2_custom_min.css")
				{
					p.writeSuccess("text/css");
					p.outputStream.Write(Pages.Resource.getStringResource("jquery_ui_1_10_2_custom_min.css"));
					return;
				}
				else if (requestedPage == "Styles/TableSorter_Blue.css")
				{
					p.writeSuccess("text/css");
					p.outputStream.Write(Css.TableSorter_Blue());
					return;
				}
				else if (requestedPage == "Styles/TableSorter_Green.css")
				{
					p.writeSuccess("text/css");
					p.outputStream.Write(Css.TableSorter_Green());
					return;
				}
				else if (requestedPage == "Styles/Site.css")
				{
					p.writeSuccess("text/css");
					p.outputStream.Write(Css.Site());
					return;
				}
				else if (requestedPage.StartsWith("Styles/images/"))
				{
					string file = requestedPage.Substring("Styles/images/".Length);
					if (file.EndsWith(".png"))
						p.writeSuccess("image/png");
					else if (file.EndsWith(".gif"))
						p.writeSuccess("image/gif");
					else if (file.EndsWith(".jpg") || file.EndsWith(".jpg"))
						p.writeSuccess("image/jpeg");
					else
					{
						p.writeFailure();
						return;
					}
					p.outputStream.Flush();

					int idxLastDot = file.LastIndexOf('.');
					if (idxLastDot > -1)
						file = file.Substring(0, idxLastDot);

					byte[] imageData = Pages.Resource.getBinaryResource(file);
					p.rawOutputStream.Write(imageData, 0, imageData.Length);
					return;
				}

				if (requestedPage == "admin")
				{
					p.writeRedirect("admin/main");
					return;
				}

				if (requestedPage == "login")
				{
					LogOutUser(p, null);
					return;
				}

				Session s = sm.GetSession(p.requestCookies.GetValue("session"), p.requestCookies.GetValue("auth"), p.GetParam("rawauth"));
				//if (s != null)
				p.responseCookies.Add("session", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));

				if (requestedPage == "logout")
				{
					LogOutUser(p, s);
					return;
				}


				if (requestedPage.StartsWith("admin/"))
				{
					string adminPage = requestedPage == "admin" ? "" : requestedPage.Substring("admin/".Length);
					if (string.IsNullOrWhiteSpace(adminPage))
						adminPage = "main";
					int idxQueryStringStart = adminPage.IndexOf('?');
					if (idxQueryStringStart == -1)
						idxQueryStringStart = adminPage.Length;
					adminPage = adminPage.Substring(0, idxQueryStringStart);
					Pages.Admin.AdminPage.HandleRequest(adminPage, p, s);
					return;
				}
				else if (requestedPage.EndsWith(".jpg") || requestedPage.EndsWith(".jpeg") || requestedPage.EndsWith(".png") || requestedPage.EndsWith(".webp"))
				{
					int extensionLength = requestedPage[requestedPage.Length - 4] == '.' ? 4 : 5;
					string format = requestedPage.Substring(requestedPage.Length - (extensionLength - 1));
					string cameraId = requestedPage.Substring(0, requestedPage.Length - extensionLength);

					int minPermission = cm.GetCameraMinPermission(cameraId);
					if (minPermission == 101)
					{
						p.writeFailure();
						return;
					}
					if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
					{
						LogOutUser(p, s);
						return;
					}
					IPCameraBase cam = cm.GetCamera(cameraId);
					byte[] latestImage = cm.GetLatestImage(cameraId);
					int patience = p.GetIntParam("patience");
					if (patience > 0)
					{
						if (patience > 5000)
							patience = 5000;

						int timeLeft = patience;
						HiResTimer timer = new HiResTimer();
						timer.Start();
						while (s.DuplicateImageSendCheck(cameraId, latestImage) && cam != null && timeLeft > 0)
						{
							// The latest image was already sent to the user in a previous image request.
							// Wait for up to 5 seconds as desired by the user to get a "new" image.
							cam.newFrameWaitHandle.WaitOne(Math.Max(50, timeLeft));  // This EventWaitHandle nonsense isn't perfect, so this should prevent excessively long delays in the event of a timing error.
							latestImage = cm.GetLatestImage(cameraId);
							timeLeft = patience - (int)timer.ElapsedMilliseconds;
						}
					}
					ImageFormat imgFormat = ImageFormat.Jpeg;
					latestImage = ImageConverter.HandleRequestedConversionIfAny(latestImage, p, ref imgFormat, format);
					p.writeSuccess(ImageConverter.GetMime(imgFormat), latestImage.Length);
					p.outputStream.Flush();
					p.rawOutputStream.Write(latestImage, 0, latestImage.Length);
				}
				else if (requestedPage == "keepalive")
				{
					string cameraId = p.GetParam("id");
					int minPermission = cm.GetCameraMinPermission(cameraId);
					if (minPermission == 101)
					{
						p.writeFailure();
						return;
					}
					if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
					{
						p.writeFailure("403 Forbidden");
						return;
					}
					cm.GetRTSPUrl(cameraId, p);
					p.writeSuccess("text/plain");
					p.outputStream.Write("1");
				}
				else if (requestedPage.EndsWith(".mjpg"))
				{
					string cameraId = requestedPage.Substring(0, requestedPage.Length - 5);
					int minPermission = cm.GetCameraMinPermission(cameraId);
					if (minPermission == 101)
					{
						p.writeFailure();
						return;
					}
					if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
					{
						LogOutUser(p, s);
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
							
							ImageFormat imgFormat = ImageFormat.Jpeg;
							byte[] sendImage = ImageConverter.HandleRequestedConversionIfAny(newImage, p, ref imgFormat);

							p.outputStream.WriteLine("--ipcamera");
							p.outputStream.WriteLine("Content-Type: " + ImageConverter.GetMime(imgFormat));
							p.outputStream.WriteLine("Content-Length: " + sendImage.Length);
							p.outputStream.WriteLine();
							p.outputStream.Flush();
							p.rawOutputStream.Write(sendImage, 0, sendImage.Length);
							p.rawOutputStream.Flush();
							p.outputStream.WriteLine();
						}
						catch (Exception ex)
						{
							if (!p.isOrdinaryDisconnectException(ex))
								Logger.Debug(ex);
							break;
						}
					}
				}
				else if (requestedPage.EndsWith(".cam"))
				{
					string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
					int minPermission = cm.GetCameraMinPermission(cameraId);
					if (minPermission == 101)
					{
						p.writeFailure();
						return;
					}
					if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
					{
						LogOutUser(p, s);
						return;
					}

					string userAgent = p.GetHeaderValue("User-Agent", "");
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
					int minPermission = cm.GetCameraMinPermission(cameraId);
					if (minPermission == 101)
					{
						p.writeFailure();
						return;
					}
					if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
					{
						LogOutUser(p, s);
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
							int minPermission = cm.GetCameraMinPermission(cameraId);
							if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission) || minPermission == 101)
							{
							}
							else
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
						LogOutUser(p, s);
					}
					return;
				}
			}
			catch (Exception ex)
			{
				if (!p.isOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}

		private void LogOutUser(HttpProcessor p, Session s)
		{
			if (s != null)
				sm.RemoveSession(s.sid);
			p.responseCookies.Add("session", "", TimeSpan.Zero);
			p.writeSuccess("text/html");
			p.outputStream.Write(Login.GetString());
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				Session s = sm.GetSession(p.requestCookies.GetValue("session"), p.requestCookies.GetValue("auth"));
				if (s.permission == 100)
					p.responseCookies.Add("session", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));
				else
				{
					p.writeFailure("403 Forbidden");
					return;
				}

				string requestedPage = p.request_url.AbsolutePath.TrimStart('/');
				if (requestedPage == "admin/saveitem")
				{
					string result = MJpegWrapper.cfg.SaveItem(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else if (requestedPage == "admin/deleteitems")
				{
					string result = MJpegWrapper.cfg.DeleteItems(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else if (requestedPage == "admin/reordercam")
				{
					string result = MJpegWrapper.cfg.ReorderCam(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
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
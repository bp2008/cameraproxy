using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using BPUtil;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy
{
	public class MJpegServer : HttpServer
	{
		public static CameraManager cm = new CameraManager();
		public static SessionManager sm = new SessionManager();
		public MJpegServer(int port, int port_https, X509Certificate2 cert)
			: base(port, port_https, cert)
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPage = Uri.UnescapeDataString(p.request_url.AbsolutePath.TrimStart('/'));

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

				Session s = sm.GetSession(p.requestCookies.GetValue("cps"), p.requestCookies.GetValue("auth"), p.GetParam("rawauth"));
				if (s.sid != null && s.sid.Length == 16)
					p.responseCookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));

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
				else if (requestedPage.StartsWith("image/"))
				{
					requestedPage = requestedPage.Substring("image/".Length);
					#region image/
					if (requestedPage.EndsWith(".jpg") || requestedPage.EndsWith(".jpeg") || requestedPage.EndsWith(".png") || requestedPage.EndsWith(".webp"))
					{
						int extensionLength = requestedPage[requestedPage.Length - 4] == '.' ? 4 : 5;
						string format = requestedPage.Substring(requestedPage.Length - (extensionLength - 1));
						string cameraId = requestedPage.Substring(0, requestedPage.Length - extensionLength);
						cameraId = cameraId.ToLower();

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
						int wait = p.GetIntParam("wait", 5000);
						IPCameraBase cam = cm.GetCamera(cameraId);
						byte[] latestImage = cm.GetLatestImage(cameraId, wait);
						int patience = p.GetIntParam("patience");
						if (patience > 0)
						{
							if (patience > 5000)
								patience = 5000;

							int timeLeft = patience;
							System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
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
						if (latestImage.Length == 0)
						{
							p.writeFailure("502 Bad Gateway");
							return;
						}
						ImageFormat imgFormat = ImageFormat.Jpeg;
						latestImage = ImageConverter.HandleRequestedConversionIfAny(latestImage, p, ref imgFormat, format);
						p.tcpClient.SendBufferSize = latestImage.Length + 256;
						p.writeSuccess(Util.GetMime(imgFormat), latestImage.Length);
						p.outputStream.Flush();
						p.rawOutputStream.Write(latestImage, 0, latestImage.Length);
					}
					else if (requestedPage.EndsWith(".mjpg"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 5);
						cameraId = cameraId.ToLower();
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
						// Increasing the send buffer size here does not help streaming fluidity.
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
									if (this.stopRequested)
										return;
								}
								lastImage = newImage;

								ImageFormat imgFormat = ImageFormat.Jpeg;
								byte[] sendImage = ImageConverter.HandleRequestedConversionIfAny(newImage, p, ref imgFormat);

								p.outputStream.WriteLine("--ipcamera");
								p.outputStream.WriteLine("Content-Type: " + Util.GetMime(imgFormat));
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
					else if (requestedPage.EndsWith(".ogg"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
						cameraId = cameraId.ToLower();
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
						IPCameraBase _cam = cm.GetCamera(cameraId);
						if (_cam is Html5VideoCamera)
						{
							Html5VideoCamera cam = (Html5VideoCamera)_cam;
							ConcurrentQueue<byte[]> myDataListener = new ConcurrentQueue<byte[]>();
							try
							{
								cam.RegisterStreamListener(myDataListener);
								p.writeSuccess("application/octet-stream");
								p.outputStream.Flush();
								byte[] outputBuffer;
								int chunkCount = 0;
								while (!this.stopRequested)
								{
									try
									{
										chunkCount = myDataListener.Count;
										if (chunkCount > 100)
											return; // This connection is falling too far behind.  End it.
										else if (chunkCount > 0)
										{
											Console.Write(chunkCount + " ");
											if (myDataListener.TryDequeue(out outputBuffer))
											{
												p.rawOutputStream.Write(outputBuffer, 0, outputBuffer.Length);
												p.rawOutputStream.Flush();
											}
										}
										else
											Thread.Sleep(1);
									}
									catch (Exception ex)
									{
										if (!p.isOrdinaryDisconnectException(ex))
											Logger.Debug(ex);
										break;
									}
								}
							}
							finally
							{
								cam.UnregisterStreamListener(myDataListener);
							}
						}
						else
						{
							p.writeFailure("501 Not Implemented");
						}
					}
					else if (requestedPage.EndsWith(".cam"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
						cameraId = cameraId.ToLower();
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
						if (cam != null && cam.cameraSpec.ptzType == MJpegCameraProxy.Configuration.PtzType.Dahua || cam.cameraSpec.ptzType == MJpegCameraProxy.Configuration.PtzType.Hikvision)
						{
							p.writeRedirect("../Camera.html?cam=" + cameraId);
							return;
						}

						string userAgent = p.GetHeaderValue("User-Agent", "");
						bool isMobile = userAgent.Contains("iPad") || userAgent.Contains("iPhone") || userAgent.Contains("Android") || userAgent.Contains("BlackBerry");

						bool isLanConnection = p == null ? false : p.IsLanConnection;
						int defaultRefresh = isLanConnection && !isMobile ? -1 : 250;
						string html = CamPage.GetHtml(cameraId, !isMobile, p.GetIntParam("refresh", defaultRefresh), p.GetBoolParam("override") ? -1 : 600000, p);
						if (string.IsNullOrEmpty(html) || html == "NO")
						{
							p.writeFailure();
							return;
						}
						p.writeSuccess("text/html");
						p.outputStream.Write(html);
					}
					else if (requestedPage == "PTZPRESETIMG")
					{
						string cameraId = p.GetParam("id");
						cameraId = cameraId.ToLower();
						IPCameraBase cam = cm.GetCamera(cameraId);
						if (cam != null)
						{
							int index = p.GetIntParam("index", -1);
							if (index > -1)
							{
								if (cam.cameraSpec.ptz_proxy)
								{
									string auth = (!string.IsNullOrEmpty(cam.cameraSpec.ptz_username) && !string.IsNullOrEmpty(cam.cameraSpec.ptz_password)) ? "rawauth=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_username) + ":" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_password) + "&" : "";
									byte[] data = SimpleProxy.GetData("http://" + cam.cameraSpec.ptz_hostName + "/PTZPRESETIMG?" + auth + "id=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_proxy_cameraId) + "&index=" + index);
									if (data.Length > 0)
									{
										p.writeSuccess("image/jpg", data.Length);
										p.outputStream.Flush();
										p.rawOutputStream.Write(data, 0, data.Length);
										return;
									}
								}
								else
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
						}
						{ // Failed to get image thumbnail
							byte[] bytes = File.ReadAllBytes(Globals.WWWPublicDirectoryBase + "Images/qmark.png");
							p.writeSuccess("image/png", bytes.Length);
							p.outputStream.Flush();
							p.rawOutputStream.Write(bytes, 0, bytes.Length);
							return;
						}
					}
					else if (requestedPage.EndsWith(".wanscamstream"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - ".wanscamstream".Length);
						IPCameraBase cam = cm.GetCamera(cameraId);
						if (cam == null)
							return;
						if (!cam.cameraSpec.wanscamCompatibilityMode)
							return;
						if (p.RemoteIPAddress != "127.0.0.1")
							return;
						Uri url = new Uri(cam.cameraSpec.imageryUrl);
						string host = url.Host;
						int port = url.Port;
						string path = url.PathAndQuery;
						//string path = "/livestream.cgi?user=admin&pwd=nooilwell&streamid=0&audio=0&filename=";
						//string path = "/videostream.cgi?user=admin&pwd=nooilwell&resolution=8";
						int total = 0;
						try
						{
							//Console.WriteLine("opening");
							Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
							socket.Connect(host, port);
							byte[] buffer = new byte[4096];
							socket.Send(UTF8Encoding.UTF8.GetBytes("GET " + path + " HTTP/1.1\r\nHost: " + host + ":" + port + "\r\nConnection: close\r\n\r\n"));
							//Console.WriteLine("open");
							int read = socket.Receive(buffer);
							p.writeSuccess("video/raw");
							p.outputStream.Flush();
							while (read > 0 && socket.Connected && p.tcpClient.Connected)
							{
								p.rawOutputStream.Write(buffer, 0, read);
								total += read;
								//Console.WriteLine(read);
								read = socket.Receive(buffer);
							}
							//Console.WriteLine("close");
						}
						catch (Exception ex)
						{
							if (!p.isOrdinaryDisconnectException(ex))
								Logger.Debug(ex);
						}
					}
					#endregion
				}
				else if (requestedPage.StartsWith("control/"))
				{
					requestedPage = requestedPage.Substring("control/".Length);
					#region control/
					if (requestedPage == "keepalive")
					{
						string cameraId = p.GetParam("id");
						cameraId = cameraId.ToLower();
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

					else if (requestedPage == "PTZ")
					{
						string cameraId = p.GetParam("id");
						cameraId = cameraId.ToLower();
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
					#endregion
				}
				else
				{
					#region www
					int permissionRequired;
					if (!Util.TryGetValue(requestedPage.ToLower(), MJpegWrapper.cfg.GetWwwFilesList(), out permissionRequired))
						permissionRequired = -1;


					string wwwDirectory = permissionRequired == -1 ? Globals.WWWPublicDirectoryBase : Globals.WWWDirectoryBase;

					if (permissionRequired < 0) permissionRequired = 0;
					else if (permissionRequired > 100) permissionRequired = 100;

					if (permissionRequired > s.permission)
					{
						LogOutUser(p, s);
						return;
					}

					DirectoryInfo WWWDirectory = new DirectoryInfo(wwwDirectory);
					string wwwDirectoryBase = WWWDirectory.FullName.Replace('\\', '/').TrimEnd('/') + '/';
					FileInfo fi = new FileInfo(wwwDirectoryBase + requestedPage);
					string targetFilePath = fi.FullName.Replace('\\', '/');
					if (!targetFilePath.StartsWith(wwwDirectoryBase) || targetFilePath.Contains("../"))
					{
						p.writeFailure("400 Bad Request");
						return;
					}
					if (!fi.Exists)
					{
						p.writeFailure();
						return;
					}

					// && (fi.Extension == ".html" || fi.Extension == ".htm")
					if (fi.Name.ToLower() == "camera.html" && fi.Length < 256000)
					{
						p.writeSuccess(Mime.GetMimeType(fi.Extension));
						string html = File.ReadAllText(fi.FullName);
						CamPage2 cp = new CamPage2(html, p);
						html = cp.Html;
						html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
						html = html.Replace("%ALLCAMS_IDS_NAMES_JS_ARRAY%", MJpegServer.cm.GenerateAllCameraIdNameList(s == null ? 0 : s.permission));
						try
						{
							html = html.Replace("%REMOTEIP%", p.RemoteIPAddress);
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
						p.outputStream.Write(html);
						p.outputStream.Flush();
					}
					else if ((fi.Extension == ".html" || fi.Extension == ".htm") && fi.Length < 256000)
					{
						p.writeSuccess(Mime.GetMimeType(fi.Extension));
						string html = File.ReadAllText(fi.FullName);
						html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
						html = html.Replace("%ALLCAMS_IDS_NAMES_JS_ARRAY%", MJpegServer.cm.GenerateAllCameraIdNameList(s == null ? 0 : s.permission));
						try
						{
							html = html.Replace("%REMOTEIP%", p.RemoteIPAddress);
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
						p.outputStream.Write(html);
						p.outputStream.Flush();
					}
					else
					{
						List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
						additionalHeaders.Add(new KeyValuePair<string, string>("Cache-Control", "max-age=3600, public"));
						p.writeSuccess(Mime.GetMimeType(fi.Extension), additionalHeaders: additionalHeaders);
						p.outputStream.Flush();
						using (FileStream fs = fi.OpenRead())
						{
							fs.CopyTo(p.rawOutputStream);
						}
						p.rawOutputStream.Flush();
					}
					#endregion
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
			p.responseCookies.Add("cps", "", TimeSpan.Zero);
			p.writeSuccess("text/html");
			p.outputStream.Write(Login.GetString());
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				Session s = sm.GetSession(p.requestCookies.GetValue("cps"), p.requestCookies.GetValue("auth"));
				if (s.permission == 100)
					p.responseCookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));
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
				else if (requestedPage == "admin/savelist")
				{
					string result = Pages.Admin.AdminPage.HandleSaveList(p, s);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		protected override void stopServer()
		{
			cm.Stop();
		}
	}
}
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
		public MJpegServer(X509Certificate2 cert)
			: base(SimpleCertificateSelector.FromCertificate(cert))
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPage = Uri.UnescapeDataString(p.Request.Url.AbsolutePath.TrimStart('/'));

				if (requestedPage == "admin")
				{
					p.Response.Redirect("admin/main");
					return;
				}

				if (requestedPage == "login")
				{
					LogOutUser(p, null);
					return;
				}

				Session s = sm.GetSession(p.Request.Cookies.GetValue("cps"), p.Request.Cookies.GetValue("auth"), p.Request.GetParam("rawauth"));
				if (s.sid != null && s.sid.Length == 16)
					p.Response.Cookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));

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
							p.Response.Simple("404 Not Found");
							return;
						}
						if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
						{
							LogOutUser(p, s);
							return;
						}
						int wait = p.Request.GetIntParam("wait", 5000);
						IPCameraBase cam = cm.GetCamera(cameraId);
						byte[] latestImage = cm.GetLatestImage(cameraId, wait);
						int patience = p.Request.GetIntParam("patience");
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
							p.Response.Simple("502 Bad Gateway");
							return;
						}
						ImageFormat imgFormat = ImageFormat.Jpeg;
						latestImage = ImageConverter.HandleRequestedConversionIfAny(latestImage, p, ref imgFormat, format);
						p.GetTcpClient().SendBufferSize = latestImage.Length + 256;
						p.Response.FullResponseBytes(latestImage, Util.GetMime(imgFormat));
						return;
					}
					else if (requestedPage.EndsWith(".mjpg"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 5);
						cameraId = cameraId.ToLower();
						int minPermission = cm.GetCameraMinPermission(cameraId);
						if (minPermission == 101)
						{
							p.Response.Simple("404 Not Found");
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
						p.Response.CloseWithoutResponse();
						Stream responseStream = p.Response.GetResponseStreamSync();
						WriteUtf8String(responseStream, "HTTP/1.1 200 OK" + "\r\n"
							+ "Connection: close" + "\r\n"
							+ "Content-Type: multipart/x-mixed-replace;boundary=ipcamera" + "\r\n"
							+ "\r\n");
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

								WriteUtf8Line(responseStream, "--ipcamera");
								WriteUtf8Line(responseStream, "Content-Type: " + Util.GetMime(imgFormat));
								WriteUtf8Line(responseStream, "Content-Length: " + sendImage.Length);
								WriteUtf8Line(responseStream, "");
								WriteBytes(responseStream, sendImage);
								WriteUtf8Line(responseStream, "");
							}
							catch (Exception ex)
							{
								if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
									Logger.Debug(ex);
								break;
							}
						}
						return;
					}
					else if (requestedPage.EndsWith(".ogg"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
						cameraId = cameraId.ToLower();
						int minPermission = cm.GetCameraMinPermission(cameraId);
						if (minPermission == 101)
						{
							p.Response.Simple("404 Not Found");
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
								p.Response.CloseWithoutResponse();
								Stream responseStream = p.Response.GetResponseStreamSync();
								WriteUtf8String(responseStream, "HTTP/1.1 200 OK" + "\r\n"
									+ "Connection: close" + "\r\n"
									+ "Content-Type: application/octet-stream" + "\r\n"
									+ "\r\n");
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
												WriteBytes(responseStream, outputBuffer);
											}
										}
										else
											Thread.Sleep(1);
									}
									catch (Exception ex)
									{
										if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
											Logger.Debug(ex);
										break;
									}
								}
								return;
							}
							finally
							{
								cam.UnregisterStreamListener(myDataListener);
							}
						}
						else
						{
							p.Response.Simple("501 Not Implemented");
							return;
						}
					}
					else if (requestedPage.EndsWith(".cam"))
					{
						string cameraId = requestedPage.Substring(0, requestedPage.Length - 4);
						cameraId = cameraId.ToLower();
						int minPermission = cm.GetCameraMinPermission(cameraId);
						if (minPermission == 101)
						{
							p.Response.Simple("404 Not Found");
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
							p.Response.Redirect("../Camera.html?cam=" + cameraId);
							return;
						}

						string userAgent = p.Request.Headers.Get("User-Agent") ?? "";
						bool isMobile = userAgent.Contains("iPad") || userAgent.Contains("iPhone") || userAgent.Contains("Android") || userAgent.Contains("BlackBerry");

						bool isLanConnection = p == null ? false : p.IsLanConnection;
						int defaultRefresh = isLanConnection && !isMobile ? -1 : 250;
						string html = CamPage.GetHtml(cameraId, !isMobile, p.Request.GetIntParam("refresh", defaultRefresh), p.Request.GetBoolParam("override") ? -1 : 600000, p);
						if (string.IsNullOrEmpty(html) || html == "NO")
						{
							p.Response.Simple("404 Not Found");
							return;
						}
						p.Response.FullResponseUTF8(html, "text/html; charset=utf-8");
						p.Response.CompressResponseIfCompatible();
						return;
					}
					else if (requestedPage == "PTZPRESETIMG")
					{
						string cameraId = p.Request.GetParam("id");
						cameraId = cameraId.ToLower();
						IPCameraBase cam = cm.GetCamera(cameraId);
						if (cam != null)
						{
							int index = p.Request.GetIntParam("index", -1);
							if (index > -1)
							{
								if (cam.cameraSpec.ptz_proxy)
								{
									string auth = (!string.IsNullOrEmpty(cam.cameraSpec.ptz_username) && !string.IsNullOrEmpty(cam.cameraSpec.ptz_password)) ? "rawauth=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_username) + ":" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_password) + "&" : "";
									byte[] data = SimpleProxy.GetData("http://" + cam.cameraSpec.ptz_hostName + "/PTZPRESETIMG?" + auth + "id=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_proxy_cameraId) + "&index=" + index);
									if (data.Length > 0)
									{
										p.Response.FullResponseBytes(data, "image/jpg");
										return;
									}
								}
								else
								{
									string fileName = CameraProxyGlobals.ThumbsDirectoryBase + cameraId + index + ".jpg";
									int minPermission = cm.GetCameraMinPermission(cameraId);
									if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission) || minPermission == 101)
									{
									}
									else
									{
										if (File.Exists(fileName))
										{
											p.Response.StaticFile(fileName);
											return;
										}
									}
								}
							}
						}
						{ // Failed to get image thumbnail
							p.Response.StaticFile(CameraProxyGlobals.WWWPublicDirectoryBase + "Images/qmark.png");
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
						if (p.RemoteIPAddressStr != "127.0.0.1")
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
							p.Response.CloseWithoutResponse();
							Stream responseStream = p.Response.GetResponseStreamSync();
							WriteUtf8String(responseStream, "HTTP/1.1 200 OK" + "\r\n"
								+ "Connection: close" + "\r\n"
								+ "Content-Type: video/raw" + "\r\n"
								+ "\r\n");
							while (read > 0 && socket.Connected && p.CheckIfStillConnected())
							{
								responseStream.Write(buffer, 0, read);
								total += read;
								//Console.WriteLine(read);
								read = socket.Receive(buffer);
							}
							return;
							//Console.WriteLine("close");
						}
						catch (Exception ex)
						{
							if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
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
						string cameraId = p.Request.GetParam("id");
						cameraId = cameraId.ToLower();
						int minPermission = cm.GetCameraMinPermission(cameraId);
						if (minPermission == 101)
						{
							p.Response.Simple("404 Not Found");
							return;
						}
						if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
						{
							p.Response.Simple("403 Forbidden");
							return;
						}
						cm.GetRTSPUrl(cameraId, p);
						p.Response.FullResponseUTF8("1", "text/plain");
						return;
					}

					else if (requestedPage == "PTZ")
					{
						string cameraId = p.Request.GetParam("id");
						cameraId = cameraId.ToLower();
						int minPermission = cm.GetCameraMinPermission(cameraId);
						if (minPermission == 101)
						{
							p.Response.Simple("404 Not Found");
							return;
						}
						if ((s == null && minPermission > 0) || (s != null && s.permission < minPermission))
						{
							LogOutUser(p, s);
							return;
						}
						PTZ.RunCommand(cameraId, p.Request.GetParam("cmd"));
						p.Response.FullResponseUTF8("", "text/plain");
						return;
					}
					#endregion
				}
				else if (requestedPage.StartsWith(".well-known/acme-challenge/"))
				{
					string token = requestedPage.Substring(".well-known/acme-challenge/".Length);
					string response;
					if (letsEncryptChallenges.TryGetValue(token, out response))
					{
						p.Response.FullResponseUTF8(response, "text/plain");
						return;
					}
					else
					{
						p.Response.Simple("404 Not Found");
						return;
					}
				}
				else
				{
					#region www
					int permissionRequired;
					if (!Util.TryGetValue(requestedPage.ToLower(), MJpegWrapper.cfg.GetWwwFilesList(), out permissionRequired))
						permissionRequired = -1;


					string wwwDirectory = permissionRequired == -1 ? CameraProxyGlobals.WWWPublicDirectoryBase : CameraProxyGlobals.WWWDirectoryBase;

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
						p.Response.Simple("400 Bad Request");
						return;
					}
					if (!fi.Exists)
					{
						p.Response.Simple("404 Not Found");
						return;
					}

					// && (fi.Extension == ".html" || fi.Extension == ".htm")
					if (fi.Name.ToLower() == "camera.html" && fi.Length < 256000)
					{
						string html = File.ReadAllText(fi.FullName);
						CamPage2 cp = new CamPage2(html, p);
						html = cp.Html;
						html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
						html = html.Replace("%ALLCAMS_IDS_NAMES_JS_ARRAY%", MJpegServer.cm.GenerateAllCameraIdNameList(s == null ? 0 : s.permission));
						try
						{
							html = html.Replace("%REMOTEIP%", p.RemoteIPAddressStr);
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
						p.Response.FullResponseUTF8(html, "text/html; charset=utf-8");
						p.Response.CompressResponseIfCompatible();
						return;
					}
					else if ((fi.Extension == ".html" || fi.Extension == ".htm") && fi.Length < 256000)
					{
						string html = File.ReadAllText(fi.FullName);
						html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
						html = html.Replace("%ALLCAMS_IDS_NAMES_JS_ARRAY%", MJpegServer.cm.GenerateAllCameraIdNameList(s == null ? 0 : s.permission));
						try
						{
							html = html.Replace("%REMOTEIP%", p.RemoteIPAddressStr);
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
						p.Response.FullResponseUTF8(html, "text/html; charset=utf-8");
						p.Response.CompressResponseIfCompatible();
						return;
					}
					else
					{
						p.Response.StaticFile(fi);
						return;
					}
					#endregion
				}
			}
			catch (Exception ex)
			{
				if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}

		private void WriteBytes(Stream stream, byte[] bytes)
		{
			stream.Write(bytes, 0, bytes.Length);
		}
		private void WriteUtf8Line(Stream stream, string str)
		{
			WriteUtf8String(stream, str + "\r\n");
		}
		private void WriteUtf8String(Stream stream, string str)
		{
			WriteBytes(stream, ByteUtil.Utf8NoBOM.GetBytes(str));
		}
		ConcurrentDictionary<string, string> letsEncryptChallenges = new ConcurrentDictionary<string, string>();
		internal void PrepareCertificationChallenge(LetsEncrypt.CertificateChallenge challenge)
		{
			Logger.Info("Challenge setup: \"" + challenge.challengeToken + "\" : \"" + challenge.expectedResponse + "\"");
			letsEncryptChallenges[challenge.challengeToken] = challenge.expectedResponse;
		}
		internal void ClearChallenges()
		{
			letsEncryptChallenges.Clear();
		}

		private void LogOutUser(HttpProcessor p, Session s)
		{
			if (s != null)
				sm.RemoveSession(s.sid);
			p.Response.Cookies.Add("cps", "", TimeSpan.Zero);
			p.Response.FullResponseUTF8(Login.GetString(), "text/html; charset=utf-8");
			p.Response.CompressResponseIfCompatible();
			return;
		}

		public override void handlePOSTRequest(HttpProcessor p)
		{
			try
			{
				Session s = sm.GetSession(p.Request.Cookies.GetValue("cps"), p.Request.Cookies.GetValue("auth"));
				if (s.permission == 100)
					p.Response.Cookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));
				else
				{
					p.Response.Simple("403 Forbidden");
					return;
				}

				string requestedPage = p.Request.Url.AbsolutePath.TrimStart('/');
				if (requestedPage == "admin/saveitem")
				{
					string result = MJpegWrapper.cfg.SaveItem(p);
					p.Response.FullResponseUTF8(HttpUtility.HtmlEncode(result), "text/plain; charset=utf-8");
				}
				else if (requestedPage == "admin/deleteitems")
				{
					string result = MJpegWrapper.cfg.DeleteItems(p);
					p.Response.FullResponseUTF8(HttpUtility.HtmlEncode(result), "text/plain; charset=utf-8");
				}
				else if (requestedPage == "admin/reordercam")
				{
					string result = MJpegWrapper.cfg.ReorderCam(p);
					p.Response.FullResponseUTF8(HttpUtility.HtmlEncode(result), "text/plain; charset=utf-8");
				}
				else if (requestedPage == "admin/savelist")
				{
					string result = Pages.Admin.AdminPage.HandleSaveList(p, s);
					p.Response.FullResponseUTF8(HttpUtility.HtmlEncode(result), "text/plain; charset=utf-8");
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
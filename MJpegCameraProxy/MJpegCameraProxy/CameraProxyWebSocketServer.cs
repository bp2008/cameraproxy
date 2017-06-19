using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Fleck;
using BPUtil.SimpleHttp;
using MJpegCameraProxy.PanTiltZoom;
using BPUtil;

namespace MJpegCameraProxy
{
	public class CameraProxyWebSocketServer
	{
		/// <summary>
		/// If > -1, the server is listening for ws connections on this port.
		/// </summary>
		protected readonly int port;
		/// <summary>
		/// If > -1, the server is listening for wss connections on this port.
		/// </summary>
		protected readonly int secure_port;
		protected volatile bool stopRequested = false;
		private X509Certificate2 ssl_certificate;

		WebSocketServer ws_insecure;
		WebSocketServer wss_secure;

		Action<IWebSocketConnection> socketHandler;
		SortedList<IWebSocketConnection, int> allSockets;
		SortedList<string, HashSet<IWebSocketConnection>> registration_cameraToSocket = new SortedList<string, HashSet<IWebSocketConnection>>();
		SortedList<IWebSocketConnection, string> registration_socketToCamera;

		ComparisonComparer<IWebSocketConnection> wsComparer = new ComparisonComparer<IWebSocketConnection>((ws1, ws2) =>
		{
			return ws1.ConnectionInfo.Id.CompareTo(ws2.ConnectionInfo.Id);
		});

		public event EventHandler<string> SocketBound = delegate { };

		public CameraProxyWebSocketServer(int port, int secure_port = -1, X509Certificate2 cert = null)
		{
			allSockets = new SortedList<IWebSocketConnection, int>(wsComparer);
			registration_socketToCamera = new SortedList<IWebSocketConnection, string>(wsComparer);
			FleckLog.logStuff = false;

			this.port = port;
			this.secure_port = secure_port;
			this.ssl_certificate = cert;

			if (this.port > 65535 || this.port < -1) this.port = -1;
			if (this.secure_port > 65535 || this.secure_port < -1) this.secure_port = -1;

			if (port > -1)
			{
				ws_insecure = new WebSocketServer("ws://localhost:" + port);
				ws_insecure.SocketBound += Ws_SocketBound;
			}

			if (secure_port > -1)
			{
				wss_secure = new WebSocketServer("wss://localhost:" + secure_port);
				wss_secure.SocketBound += Ws_SocketBound;
				wss_secure.Certificate = cert != null ? cert : HttpServer.GetSelfSignedCertificate();
			}

			socketHandler = new Action<IWebSocketConnection>(handler);
		}

		private void Ws_SocketBound(object sender, string e)
		{
			SocketBound(sender, e);
		}

		/// <summary>
		/// Starts listening for connections.
		/// </summary>
		public void Start()
		{
			if (ws_insecure != null)
				ws_insecure.Start(handler);
			if (wss_secure != null)
				wss_secure.Start(handler);
		}
		public void handler(IWebSocketConnection socket)
		{
			socket.OnOpen = () =>
			{
				try
				{
					Console.WriteLine("WebSocket Open!");
					lock (allSockets)
					{
						allSockets[socket] = 0;
					}
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
			};
			socket.OnClose = () =>
			{
				try
				{
					Console.WriteLine("WebSocket Close!");
					lock (allSockets)
					{
						allSockets.Remove(socket);
						foreach (HashSet<IWebSocketConnection> socketSet in registration_cameraToSocket.Values)
							if (socketSet != null)
								socketSet.Remove(socket);
						registration_socketToCamera.Remove(socket);
					}
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
			};
			socket.OnMessage = message =>
			{
				try
				{
					Console.WriteLine(message);

					if (message.StartsWith("session "))
					{
						Session s = MJpegServer.sm.GetSession(message.Substring("session ".Length));
						//if (s == null)
						//    socket.Send("sessioninvalid");
						//else
						//{
						socket.Send("sessionaccepted");
						lock (allSockets)
						{
							allSockets[socket] = (s == null ? 0 : s.permission);
						}
						//}
					}
					else if (message.StartsWith("login "))
					{
						Session s = MJpegServer.sm.GetSession(null, message.Substring("login ".Length));
						lock (allSockets)
						{
							allSockets[socket] = s.permission;
						}
					}
					else if (message.StartsWith("loginraw "))
					{
						Session s = MJpegServer.sm.GetSession(null, null, message.Substring("loginraw ".Length));
						lock (allSockets)
						{
							allSockets[socket] = s.permission;
						}
					}
					else if (message.StartsWith("register "))
					{
						string cameraId;
						if (registration_socketToCamera.TryGetValue(socket, out cameraId))
						{
							socket.Send("registrationfailed alreadyregistered");
							socket.Close();
						}
						cameraId = message.Substring("register ".Length);
						IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
						if (cam == null)
						{
							socket.Send("registrationfailed cameraunavailable");
						}
						else
						{
							int minPermission = cam.cameraSpec.minPermissionLevel;
							if (minPermission == 101)
								socket.Send("registrationfailed cameraunavailable");
							else
							{
								bool permissionOkay = false;
								lock (allSockets)
								{
									int currentPermission = allSockets[socket];
									if (currentPermission >= minPermission)
									{
										permissionOkay = true;
										HashSet<IWebSocketConnection> registeredList;
										if (!registration_cameraToSocket.TryGetValue(cameraId, out registeredList))
											registration_cameraToSocket[cameraId] = registeredList = new HashSet<IWebSocketConnection>();
										registeredList.Add(socket);
										registration_socketToCamera[socket] = cameraId;
									}
								}
								if (permissionOkay)
								{
									socket.Send("registrationaccepted");
									AdvPtz obj = AdvPtz.GetPtzObj(cameraId);

									if (obj != null)
										socket.Send("sup " + cameraId + " " + obj.ptzController.GetStatusUpdate());
								}
								else
									socket.Send("registrationfailed insufficientpermission");
							}
						}
					}
					else if (message.StartsWith("abs "))
					{
						double x;
						double y;
						double z;
						string[] parts = message.Split(' ');
						if (parts.Length == 4 && double.TryParse(parts[1], out x) && double.TryParse(parts[2], out y) && double.TryParse(parts[3], out z))
						{
							string cameraId;
							if (registration_socketToCamera.TryGetValue(socket, out cameraId))
							{
								IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
								if (cam.isRunning)
								{
									AdvPtz obj = AdvPtz.GetPtzObj(cameraId);
									if (obj != null)
									{
										obj.ptzController.PrepareWorkerThreadIfNecessary();
										obj.ptzController.SetAbsolutePTZPosition(new FloatVector3(Util.Clamp((float)x, 0f, 1f), Util.Clamp((float)y, 0f, 1f), Util.Clamp((float)z, 0f, 1f)));
									}
									else
										socket.Send("error Camera is not correct type.");
								}
								else
									socket.Send("error Camera is not running. Refresh page.");
							}
							else
								socket.Send("error This WebSocket connection does not have a camera registered.");
						}
					}
					else if (message.StartsWith("3dpos "))
					{
						double x;
						double y;
						double w;
						double h;
						int zoom;
						string[] parts = message.Split(' ');
						if (parts.Length == 6 && double.TryParse(parts[1], out x) && double.TryParse(parts[2], out y) && double.TryParse(parts[3], out w) && double.TryParse(parts[4], out h) && int.TryParse(parts[5], out zoom))
						{
							string cameraId;
							if (registration_socketToCamera.TryGetValue(socket, out cameraId))
							{
								IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
								if (cam.isRunning)
								{
									AdvPtz obj = AdvPtz.GetPtzObj(cameraId);
									if (obj != null)
									{
										obj.ptzController.PrepareWorkerThreadIfNecessary();
										obj.ptzController.Set3DPTZPosition(new Pos3d(Util.Clamp((float)x, 0, 1), Util.Clamp((float)y, 0, 1), Util.Clamp((float)w, 0, 1), Util.Clamp((float)h, 0, 1), zoom == 1 ? true : false));
									}
									else
										socket.Send("error Camera is not correct type.");
								}
								else
									socket.Send("error Camera is not running. Refresh page.");
							}
							else
								socket.Send("error This WebSocket connection does not have a camera registered.");
						}
					}
					else if (message.StartsWith("zoom "))
					{
						double z;
						string[] parts = message.Split(' ');
						if (parts.Length == 2 && double.TryParse(parts[1], out z))
						{
							string cameraId;
							if (registration_socketToCamera.TryGetValue(socket, out cameraId))
							{
								IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
								if (cam.isRunning)
								{
									AdvPtz obj = AdvPtz.GetPtzObj(cameraId);
									if (obj != null)
									{
										obj.ptzController.PrepareWorkerThreadIfNecessary();
										obj.ptzController.SetZoomPosition(z);
									}
									else
										socket.Send("error Camera is not correct type.");
								}
								else
									socket.Send("error Camera is not running. Refresh page.");
							}
							else
								socket.Send("error This WebSocket connection does not have a camera registered.");
						}
					}
					else if (message.StartsWith("genpano "))
					{
						bool permissionOkay = false;
						lock (allSockets)
						{
							if (allSockets[socket] >= 100)
								permissionOkay = true;
						}
						if (permissionOkay)
						{
							string cameraId;
							if (registration_socketToCamera.TryGetValue(socket, out cameraId))
							{
								IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
								if (cam.isRunning)
								{
									AdvPtz obj = AdvPtz.GetPtzObj(cameraId);
									if (obj != null)
									{
										if (message.EndsWith(" full"))
										{
											obj.ptzController.PrepareWorkerThreadIfNecessary();
											obj.ptzController.GeneratePanorama(true);
										}
										else if (message.EndsWith(" pseudo"))
										{
											obj.ptzController.PrepareWorkerThreadIfNecessary();
											obj.ptzController.GeneratePanorama(false);
										}
									}
									else
										socket.Send("error Camera is not correct type.");
								}
								else
									socket.Send("error Camera is not running. Refresh page.");
							}
							else
								socket.Send("error This WebSocket connection does not have a camera registered.");
						}
						else
							socket.Send("error Insufficient Permission");
					}
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
			};
		}
		public void BroadcastCameraStatusUpdate(string cameraId, string message)
		{
			HashSet<IWebSocketConnection> registeredList;
			lock (allSockets)
			{
				if (registration_cameraToSocket.TryGetValue(cameraId, out registeredList))
					foreach (IWebSocketConnection socket in registeredList)
					{
						socket.Send("sup " + cameraId + " " + message);
					}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy
{
	public class DahuaPTZ
	{
		public enum Direction
		{
			Up, Down, Left, Right, LeftUp, RightUp, LeftDown, RightDown
		}
		public class PTZPosition
		{
			public volatile float X = 0;
			public volatile float Y = 0;
			public volatile float Z = 0;
			public PTZPosition()
			{
			}
			public PTZPosition(float X, float Y, float Z)
			{
				this.X = X;
				this.Y = Y;
				this.Z = Z;
			}
		}
		public class IntPTZPosition
		{
			public volatile int X = 0;
			public volatile int Y = 0;
			public volatile int Z = 0;
			public IntPTZPosition()
			{
			}
			public IntPTZPosition(int X, int Y, int Z)
			{
				this.X = X;
				this.Y = Y;
				this.Z = Z;
			}
		}
		public class Pos3d
		{
			public volatile float X = 0;
			public volatile float Y = 0;
			public volatile float W = 0;
			public volatile float H = 0;
			public volatile bool zoomIn = false;
			public Pos3d()
			{
			}
			public Pos3d(float X, float Y, float W, float H, bool zoomIn)
			{
				this.X = X;
				this.Y = Y;
				this.W = W;
				this.H = H;
				this.zoomIn = zoomIn;
			}
		}

		string baseCGIURL;
		string host, user, pass;
		System.Threading.Timer keepAliveTimer = null;
		int session = -1;
		double thumbnailBoxWidth, thumbnailBoxHeight;
		int absoluteXOffset, panoramaVerticalDegrees;
		bool simplePanorama;
		LoginManager loginManager;
		long inner_id = 0;
		private CameraSpec cs;
		object workerThreadLock = new object();
		Thread ptzWorkerThread;
		bool[] ptzThreadAbortFlag = new bool[] { false };

		DateTime willBeIdleAt;

		PTZPosition currentPTZPosition = new PTZPosition();
		IntPTZPosition currentIntPTZPosition = new IntPTZPosition(0, 0, 0);
		public PTZPosition desiredPTZPosition = null;

		public Pos3d desired3dPosition = null;

		volatile bool currentlyAbsolutePositioned = false;

		protected long CmdID
		{
			get
			{
				return Interlocked.Increment(ref inner_id);
			}
		}

		static DahuaPTZ()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			if (System.Net.ServicePointManager.DefaultConnectionLimit < 16)
				System.Net.ServicePointManager.DefaultConnectionLimit = 16;
		}

		public DahuaPTZ(CameraSpec cs)
		{
			this.cs = cs;
			this.host = cs.ptz_hostName;
			this.user = cs.ptz_username;
			this.pass = cs.ptz_password;
			this.absoluteXOffset = cs.ptz_absoluteXOffset;
			this.thumbnailBoxWidth = cs.ptz_panorama_selection_rectangle_width_percent;
			this.thumbnailBoxHeight = cs.ptz_panorama_selection_rectangle_height_percent;
			this.simplePanorama = cs.ptz_panorama_simple;
			this.panoramaVerticalDegrees = cs.ptz_panorama_degrees_vertical;

			SetNewIdleTime();

			baseCGIURL = "http://" + host + "/cgi-bin/ptz.cgi?";
			loginManager = new LoginManager();
			PrepareWorkerThreadIfNecessary();
		}

		private void SetNewIdleTime()
		{
			if (cs.ptz_enableidleresetposition)
				this.willBeIdleAt = DateTime.Now.Add(TimeSpan.FromSeconds(cs.ptz_idleresettimeout));
			else
				this.willBeIdleAt = DateTime.MaxValue;
		}

		#region Login / Session Management
		private void DoLogin()
		{
			if (loginManager.AllowLogin())
				try
				{
					if (keepAliveTimer != null)
						keepAliveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

					session = -1;
					// Log in now
					// First, we need to get some info from the server
					string url = "http://" + host + "/RPC2_Login";
					string request = "{\"method\":\"global.login\",\"params\":{\"userName\":\"" + user + "\",\"password\":\"\",\"clientType\":\"Web3.0\"},\"id\":10000}";
					string response = HttpPost(url, request);

					var r1 = JSON.ParseJson(response);
					if (r1.error.code == 401)
					{
						// Unauthorized error; we expect this
						session = r1.session;
						string encryptionType = getJsonStringValue(response, "encryption");
						string pwtoken;
						if (encryptionType == "Basic")
							pwtoken = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(user + ":" + pass));
						else if (encryptionType == "Default")
						{
							string realm = getJsonStringValue(response, "realm");
							string random = getJsonStringValue(response, "random");
							pwtoken = Hash.GetMD5Hex(user + ":" + realm + ":" + pass).ToUpper();
							pwtoken = Hash.GetMD5Hex(user + ":" + random + ":" + pwtoken).ToUpper();
							pwtoken += pass; // Hey, I didn't invent this "encryption" type. LOL
						}
						else
							pwtoken = pass;

						// Finally, we can try to log in.
						request = "{\"method\":\"global.login\",\"session\":" + session + ",\"params\":{\"userName\":\"" + user + "\",\"password\":\"" + pwtoken + "\",\"clientType\":\"Web3.0\"},\"id\":10000}";
						response = HttpPost(url, request);
					}
					else
						Logger.Debug("Login error code not 401. Request Info Follows" + Environment.NewLine + "URL: " + url + Environment.NewLine + "Request: " + request + Environment.NewLine + "Response: " + response);

					keepAliveTimer = new System.Threading.Timer(new TimerCallback(timerTick), this, 1200, 1200);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
		}
		private string getJsonStringValue(string json, string key)
		{
			Match m = Regex.Match(json, "\"" + key + "\" *: *\"(.*?)\"");
			if (!m.Success)
				m = Regex.Match(json, "\"" + key + "\" *: *\\d+");
			return m.Success ? m.Groups[1].Value : null;
		}
		private void timerTick(object state)
		{
			KeepAlive();
		}
		private void KeepAlive()
		{
			string response = HttpPost("http://" + host + "/RPC2", "{\"method\":\"global.keepAlive\",\"params\":{\"timeout\": 300},\"session\":" + session + ",\"id\":" + CmdID + "}");
			Console.WriteLine(response);
		}
		#endregion
		public void PrepareWorkerThreadIfNecessary()
		{
			if (ptzWorkerThread == null)
			{
				lock (workerThreadLock)
				{
					if (ptzWorkerThread == null)
					{
						ptzThreadAbortFlag = new bool[] { false };
						ptzWorkerThread = new Thread(ptzWorkerLoop);
						ptzWorkerThread.Name = "PTZ_" + cs.id;
						ptzWorkerThread.Start(ptzThreadAbortFlag);
					}
				}
			}
		}
		public void Shutdown(bool isAboutToStart)
		{
			if (ptzWorkerThread != null && !isAboutToStart)
				ptzThreadAbortFlag[0] = true;
			//SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=Up&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			//SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=ZoomWide&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			//SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=FocusFar&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			//SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=IrisSmall&arg1=0&arg2=0&arg3=0", user, pass, false, false);
			if (keepAliveTimer != null)
				keepAliveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
			keepAliveTimer = null;
			session = -1;
			loginManager = new LoginManager();
			if (isAboutToStart)
				DoLogin();
			else
			{
				if (ptzWorkerThread != null && ptzWorkerThread.IsAlive)
					ptzWorkerThread.Abort();
			}
		}
		public void GeneratePseudoPanorama(int numImagesWide = 6, int numImagesHigh = 4, bool fullSizeImages = false)
		{
			IPCameraBase cam = MJpegServer.cm.GetCamera(cs.id);
			bool isFirstTime = true;
			//int numImagesHigh = full ? 6 : 4;
			//int numImagesWide = full ? 14 : 7;
			double degreesSeparationVertical = 810.0 / (numImagesHigh - 1);
			double degreesSeparationHorizontal = 3600.0 / -numImagesWide;
			for (int j = 0; j < numImagesHigh; j++)
			{
				for (int i = 0; i < numImagesWide; i++)
				{
					PositionABS((int)(i * degreesSeparationHorizontal), (int)(j * degreesSeparationVertical) + 90, 0);
					//Thread.Sleep(isFirstTime ? 7000 : 4500);
					Thread.Sleep(isFirstTime ? 6000 : 4500);
					isFirstTime = false;
					try
					{
						byte[] input = cam.LastFrame;
						if (input.Length > 0)
						{
							if (!fullSizeImages)
								input = ImageConverter.ConvertImage(input, maxWidth: 240, maxHeight: 160);
							FileInfo file = new FileInfo(Globals.ThumbsDirectoryBase + cam.cameraSpec.id.ToLower() + (i + (j * numImagesWide)) + ".jpg");
							Util.EnsureDirectoryExists(file.Directory.FullName);
							File.WriteAllBytes(file.FullName, input);
						}
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
			}
		}
		public void ptzWorkerLoop(object arg)
		{
			bool[] abortFlag = (bool[])arg;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(Thread.CurrentThread.Name + " Started");
			Console.ResetColor();
			try
			{
				DoLogin();
				while (!abortFlag[0])
				{
					try
					{
						PTZPosition desiredAbs = (PTZPosition)Interlocked.Exchange(ref desiredPTZPosition, null);
						if (desiredAbs != null)
						{
							GoToAbsolutePosition(desiredAbs, true);
							//if ("grapefruit" == false.ToString().ToLower())
							//{
							//    GeneratePseudoPanorama(14, 6, true);
							//}
						}

						Pos3d desired3d = (Pos3d)Interlocked.Exchange(ref desired3dPosition, null);
						if (desired3d != null)
						{
							SetNewIdleTime();

							float x = desired3d.X;
							float y = desired3d.Y;
							float w = desired3d.W;
							float h = desired3d.H;
							bool zIn = desired3d.zoomIn;
							float z = Math.Max(w, h);
							if (!zIn)
								z *= -1;
							currentlyAbsolutePositioned = false;
							this.Position3D(x, y, z);
							Broadcast3dPosition(x, y, w, h, zIn);
							Thread.Sleep(500);
						}
						Thread.Sleep(10);
						if (DateTime.Now > willBeIdleAt)
						{
							willBeIdleAt = DateTime.MaxValue;
							GoToAbsolutePosition(new PTZPosition((float)cs.ptz_idleresetpositionX, (float)cs.ptz_idleresetpositionY, (float)cs.ptz_idleresetpositionZ), false);
						}
					}
					catch (ThreadAbortException ex)
					{
						throw ex;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(Thread.CurrentThread.Name + " Stopping");
				Console.ResetColor();
			}
			catch (ThreadAbortException)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(Thread.CurrentThread.Name + " Aborted");
				Console.ResetColor();
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			ptzWorkerThread = null;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(Thread.CurrentThread.Name + " Exiting");
			Console.ResetColor();
		}

		private void GoToAbsolutePosition(PTZPosition desiredAbs, bool setNewIdleTime)
		{
			float x = Util.Clamp(desiredAbs.X, 0, 1);
			float y = Util.Clamp(desiredAbs.Y, 0, 1);
			float z = Util.Clamp(desiredAbs.Z, 0, 1);
			int X = (int)((1 - x) * 3600) % 3600;
			int Y = (int)(y * (panoramaVerticalDegrees / 90.0f) * 900);
			int Z = (int)Math.Round((double)z * 127) + 1;
			if (currentIntPTZPosition.X != X || currentIntPTZPosition.Y != Y || currentIntPTZPosition.Z != Z)
			{
				if (setNewIdleTime)
					SetNewIdleTime();

				currentPTZPosition.X = desiredAbs.X;
				currentPTZPosition.Y = desiredAbs.Y;
				currentPTZPosition.Z = desiredAbs.Z;
				currentIntPTZPosition.X = X;
				currentIntPTZPosition.Y = Y;
				currentIntPTZPosition.Z = Z;
				currentlyAbsolutePositioned = true;
				this.PositionABS(X, Y, Z);
				BroadcastStatusUpdate();
				Thread.Sleep(1000);
			}
		}
		public void Broadcast3dPosition(float x, float y, float w, float h, bool zIn)
		{
			MJpegWrapper.webSocketServer.BroadcastCameraStatusUpdate(cs.id, "3dpos " + x + " " + y + " " + w + " " + h + " " + (zIn ? "1" : "0"));
		}
		public string GetStatusUpdate()
		{
			return "newpos " + currentPTZPosition.X + " " + currentPTZPosition.Y + " " + currentPTZPosition.Z + " " + (currentlyAbsolutePositioned ? "1" : "0");
		}
		public void BroadcastStatusUpdate()
		{
			MJpegWrapper.webSocketServer.BroadcastCameraStatusUpdate(cs.id, GetStatusUpdate());
		}
		/// <summary>
		/// Runs the specified PTZ command.  Note that this is the only way to perform the commands in a thread-safe manner.
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="cmd"></param>
		public static void RunCommand(IPCameraBase cam, string cmd)
		{
			//if (cam == null || cam.dahuaPtz == null || string.IsNullOrWhiteSpace(cmd))
			//    return;
			//if (cam.ptzLock != null && cam.ptzLock.Wait(0))
			//{
			//    try
			//    {

			//        string[] parts = cmd.Split('/');
			//        if (parts.Length < 1)
			//            return;

			//        if (parts[0] == "move_simple")
			//        {
			//            int moveButtonIndex = int.Parse(parts[1]);
			//            if (moveButtonIndex < 1 || moveButtonIndex > 9)
			//                return;

			//            int x = 0;
			//            int y = 0;

			//            if (moveButtonIndex < 4)
			//                y = -6000;
			//            else if (moveButtonIndex > 6)
			//                y = 6000;

			//            if (moveButtonIndex % 3 == 1)
			//                x = -6000;
			//            else if (moveButtonIndex % 3 == 0)
			//                x = 6000;

			//            cam.dahuaPtz.MoveSimple(x, y, 0);
			//        }
			//        else if (parts[0] == "zoom")
			//        {
			//            int amount = int.Parse(parts[1]);
			//            cam.dahuaPtz.MoveSimple(0, 0, amount);
			//        }
			//        else if (parts[0] == "focus")
			//        {
			//            //int amount = int.Parse(parts[1]);
			//        }
			//        else if (parts[0] == "iris")
			//        {
			//            int amount = int.Parse(parts[1]);
			//            cam.dahuaPtz.Iris(amount);
			//        }
			//        else if (parts[0] == "frame")
			//        {
			//            double x = double.Parse(parts[1]);
			//            double y = double.Parse(parts[2]);
			//            x -= 0.5;
			//            y -= 0.5;
			//            int amountX = (int)(20900.0 * x);
			//            int amountY = (int)(16500.0 * y);
			//            cam.dahuaPtz.MoveSimple(amountX, amountY, 0);
			//        }
			//        else if (parts[0] == "m" && parts.Length == 3)
			//        {
			//            // Constant motion from joystick-like control; disabled due to usability and reliability concerns.  The camera's http interface simply can't accept the necessary level of control.
			//            //
			//            //double x = double.Parse(parts[1]);
			//            //double y = double.Parse(parts[2]);
			//            //x -= 0.5;
			//            //y -= 0.5;
			//            //x *= 10;
			//            //y *= 10;
			//            //int speedX = (int)Math.Round(x);
			//            //int speedY = (int)Math.Round(y);
			//            //Stopwatch watch = new Stopwatch();
			//            //watch.Start();
			//            //cam.dahuaPtz.StopMoving();
			//            //watch.Stop();
			//            //Console.WriteLine(watch.ElapsedMilliseconds);
			//            //watch.Reset();
			//            //watch.Start();
			//            //cam.dahuaPtz.StartMoving(speedX, speedY);
			//            //watch.Stop();
			//            //Console.WriteLine(watch.ElapsedMilliseconds);
			//            //Console.WriteLine();
			//        }
			//        else if (parts[0] == "stopall")
			//        {
			//            cam.dahuaPtz.StopAll();
			//        }
			//        else if (parts[0] == "generatepseudopanorama" || parts[0] == "generatepseudopanoramafull")
			//        {
			//            bool full = parts[0].EndsWith("full");
			//            bool isFirstTime = true;
			//            int numImagesHigh = full ? 6 : 4;
			//            int numImagesWide = full ? 14 : 7;
			//            double degreesSeparationVertical = 900.0 / (numImagesHigh - 1);
			//            double degreesSeparationHorizontal = 3600.0 / -numImagesWide;
			//            for (int j = 0; j < numImagesHigh; j++)
			//            {
			//                for (int i = 0; i < numImagesWide; i++)
			//                {
			//                    cam.dahuaPtz.PositionABS((int)(i * degreesSeparationHorizontal), (int)(j * degreesSeparationVertical), 0);
			//                    Thread.Sleep(isFirstTime ? 7000 : 4500);
			//                    isFirstTime = false;
			//                    try
			//                    {
			//                        byte[] input = cam.LastFrame;
			//                        if (input.Length > 0)
			//                        {
			//                            if (!full)
			//                                input = ImageConverter.ConvertImage(input, maxWidth: 240, maxHeight: 160);
			//                            FileInfo file = new FileInfo(Globals.ThumbsDirectoryBase + cam.cameraSpec.id.ToLower() + (i + (j * numImagesWide)) + ".jpg");
			//                            Util.EnsureDirectoryExists(file.Directory.FullName);
			//                            File.WriteAllBytes(file.FullName, input);
			//                        }
			//                    }
			//                    catch (Exception ex)
			//                    {
			//                        Logger.Debug(ex);
			//                    }
			//                }
			//            }
			//        }
			//        else if (parts[0] == "percent" && parts.Length == 4)
			//        {
			//            double x = 1 - double.Parse(parts[1]);
			//            double y = double.Parse(parts[2]);
			//            x *= 3600;
			//            y *= cam.dahuaPtz.panoramaVerticalDegrees * 10;
			//            cam.dahuaPtz.PositionABS((int)x, (int)y, int.Parse(parts[3]) * 8);
			//        }
			//        //if (preset_number > 0)
			//        //{
			//        //    try
			//        //    {
			//        //        byte[] image = MJpegServer.cm.GetLatestImage(cam);
			//        //        if (image.Length > 0)
			//        //        {
			//        //            Util.WriteImageThumbnailToFile(image, Globals.ThumbsDirectoryBase + cam.ToLower() + preset_number + ".jpg");
			//        //        }
			//        //    }
			//        //    catch (Exception ex)
			//        //    {
			//        //        Logger.Debug(ex);
			//        //    }
			//        //}
			//    }
			//    catch (Exception ex)
			//    {
			//        Logger.Debug(ex);
			//    }
			//    finally
			//    {
			//        cam.ptzLock.Release();
			//    }
			//}
		}

		public void MoveSimple(int xAmount, int yAmount, int zAmount)
		{
			DoAction("start", "Position", xAmount, yAmount, zAmount);
		}

		public void PositionABS(int x, int y, int z)
		{
			x = (x + (this.absoluteXOffset * 10)) % 3600;
			if (x < 0) x = 3600 + x;
			if (y < 0) y = 0;
			if (y > 900) y = 900;
			if (z < 1) z = 1;
			if (z > 128) z = 128;
			Console.WriteLine("ab x:" + x + ", y:" + y + ", z:" + z);
			DoAction("start", "PositionABS", x, y, z);
		}
		public void Position3D(float x, float y, float z)
		{
			x = Util.Clamp(x, 0, 1);
			y = Util.Clamp(y, 0, 1);
			z = Util.Clamp(z, -1, 1);
			Console.WriteLine("3d x:" + x + ", y:" + y + ", z:" + z);
			string json = "{\"method\" : \"ptz.moveDirectly\", \"session\" : " + session + ", \"params\" : {\"screen\" : [" + x + "," + y + "," + z + "]}, \"id\" : " + CmdID + "}";
			DoJsonAction(json);
		}

		public void FocusStart(int amount)
		{
			DoStartAction("start", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}
		public void FocusStop(int amount)
		{
			DoAction("stop", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}
		public void StartMoving(int speedX, int speedY)
		{
			int lowerLimit = -5;
			int upperLimit = 5;
			if (speedX < lowerLimit) speedX = lowerLimit;
			if (speedX > upperLimit) speedX = upperLimit;
			if (speedY < lowerLimit) speedY = lowerLimit;
			if (speedY > upperLimit) speedY = upperLimit;
			bool up = speedY < 0;
			bool down = speedY > 0;
			bool left = speedX < 0;
			bool right = speedX > 0;
			speedX = Math.Abs(speedX);
			speedY = Math.Abs(speedY);

			speedX += 3;
			speedY += 3;

			if (left && up) DoStartAction("start", "LeftUp", speedY, speedX, 0);
			else if (right && up) DoStartAction("start", "RightUp", speedY, speedX, 0);
			else if (left && down) DoStartAction("start", "LeftDown", speedY, speedX, 0);
			else if (right && down) DoStartAction("start", "RightDown", speedY, speedX, 0);
			else if (left) DoStartAction("start", "Left", speedX, 0, 0);
			else if (right) DoStartAction("start", "Right", speedX, 0, 0);
			else if (up) DoStartAction("start", "Up", speedY, 0, 0);
			else DoStartAction("start", "Down", speedY, 0, 0);
		}
		public void StopMoving()
		{
			DoAction("stop", "Up", 0, 0, 0);
		}

		public void StopAll()
		{
			DoAction("stop", "Up", 0, 0, 0);
			DoAction("stop", "ZoomWide", 0, 0, 0);
		}

		public void Iris(int amount)
		{
			if (amount < 1)
				amount = 1;
			if (amount > 8)
				amount = 8;
			DoAction("start", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
			DoAction("stop", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}

		public void GotoPreset(int preset)
		{
			DoAction("start", "GotoPreset", preset, 0, 0);
		}

		public void SetPreset(int preset)
		{
			DoAction("start", "SetPreset", preset, 0, 0);
		}

		protected void DoStartAction(string action, string code, int arg1, int arg2, int arg3)
		{
			DoAction(action, code, arg1, arg2, arg3);
		}
		protected void DoAction(string action, string code, int arg1, int arg2, int arg3)
		{
			//string url = baseCGIURL + "action=" + action + "&channel=0&code=" + code + "&arg1=" + arg1 + "&arg2=" + arg2 + "&arg3=" + arg3;
			PTZCommand c = new PTZCommand();
			c.id = (int)CmdID;
			c.method = "ptz." + action;
			c.param = new PTZParams(code, arg1, arg2, arg3);
			c.session = session;
			string jsonString = c.GetJson();
			DoJsonAction(jsonString);
		}

		private void DoJsonAction(string jsonString)
		{
			string url = "http://" + host + "/RPC2";
			string response = HttpPost(url, jsonString);
			int errorcode;
			if (!int.TryParse(getJsonStringValue(response, "code"), out errorcode))
			{
				errorcode = -1;
				try
				{
					var r1 = JSON.ParseJson(response);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "Request Info Follows" + Environment.NewLine + "URL: " + url + Environment.NewLine + "Request: " + jsonString + Environment.NewLine + "Response: " + response);
					errorcode = -2;
				}
			}
			if (errorcode == 404 || errorcode == -2)
				DoLogin();
		}
		int requestNumber = 0;
		protected string HttpPost(string URI, string data)
		{
			int myRequestNumber = Interlocked.Increment(ref requestNumber);
			//File.AppendAllText(Globals.ApplicationDirectoryBase + "DahuaPTZ.txt", DateTime.Now.ToString() + " " + requestNumber + " Request " + URI + Environment.NewLine + data + Environment.NewLine);
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				req.Proxy = null;
				req.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
				req.Headers["Accept-Encoding"] = "gzip, deflate";
				req.Headers["Accept-Language"] = "en-US,en;q=0.5";
				req.KeepAlive = true;
				//req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
				req.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
				req.Method = "POST";
				//req.UserAgent = "Mozilla/5.0 (DahuaPTZ Service)";
				req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:22.0) Gecko/20100101 Firefox/22.0";
				req.Headers["X-Request"] = "JSON";
				req.Headers["X-Requested-With"] = "XMLHttpRequest";
				req.Referer = "http://" + req.Host + "/";
				req.CookieContainer = new CookieContainer();
				string host = req.Host;
				int idxLastColon = host.LastIndexOf(':');
				if (idxLastColon > -1)
					host = host.Remove(idxLastColon);
				req.CookieContainer.Add(new System.Net.Cookie("DHLangCookie30", "%2Fcustom_lang%2FEnglish.txt", "/", host));
				if (session != -1)
					req.CookieContainer.Add(new System.Net.Cookie("DhWebClientSessionID", session.ToString(), "/", host));

				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
				req.ContentLength = bytes.Length;

				using (Stream os = req.GetRequestStream())
				{
					os.Write(bytes, 0, bytes.Length);
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					string responseText = sr.ReadToEnd();
					//File.AppendAllText(Globals.ApplicationDirectoryBase + "DahuaPTZ.txt", DateTime.Now.ToString() + " " + requestNumber + " Response " + URI + Environment.NewLine + responseText + Environment.NewLine);
					return responseText;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);//, "Will log in again.");
				//DoLogin();
				return ex.ToString();
			}
		}
	}

	#region JSON Control (much faster than cgi API)
	public class PTZCommand
	{
		public int id;
		public string method;
		public Params param;
		public long session;
		public string GetJson()
		{
			return "{\"method\" : \"" + method + "\", \"session\" : " + session + ",\"params\" : " + param.GetJson() + ",\"id\" : " + id + "}";
		}
	}
	public abstract class Params
	{
		public abstract string GetJson();
	}
	public class PTZParams : Params
	{
		public int arg1, arg2, arg3, channel;
		public string code;
		public PTZParams(string code, int arg1, int arg2, int arg3)
		{
			this.code = code;
			this.channel = 0;
			//if (speed < 1)
			//    speed = 1;
			//if (speed > 8)
			//    speed = 8;
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.arg3 = arg3;
		}
		public override string GetJson()
		{
			return "{\"channel\" : " + channel + ", \"code\" : \"" + code + "\",\"arg1\" : " + arg1 + ",\"arg2\" : " + arg2 + ",\"arg3\" : " + arg3 + "}";
		}
	}
	public class LoginManager
	{
		Queue<DateTime> lastLogins = new Queue<DateTime>();
		public LoginManager()
		{
		}
		public bool AllowLogin()
		{
			DateTime now = DateTime.Now;
			if (lastLogins.Count < 2)
			{
				lastLogins.Enqueue(now);
				return true;
			}
			if (lastLogins.Peek() < now.Subtract(TimeSpan.FromMinutes(1)))
			{
				lastLogins.Dequeue(); // Oldest login in queue was older than a minute ago
				lastLogins.Enqueue(now);
				return true;
			}
			else
				return false; // Oldest login in queue was newer than a minute ago.
		}
	}
	#endregion
}

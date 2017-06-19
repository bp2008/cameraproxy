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
using MJpegCameraProxy.PanTiltZoom;
using BPUtil;

namespace MJpegCameraProxy.Dahua
{
	public class DahuaPTZ : AdvPtzController
	{
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

		FloatVector3 currentPTZPosition = new FloatVector3();

		private FloatVector3 desiredPTZPosition = null;
		private Pos3d desired3dPosition = null;
		private double[] desiredZoomPosition = null;
		private bool generatePanoramaSourceFrames = false;
		private bool generatePseudoPanorama = false;

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
		public void GeneratePseudoPanorama(bool overlap, bool fullSizeImages = false)
		{
			IPCameraBase cam = MJpegServer.cm.GetCamera(cs.id);
			bool isFirstTime = true;
			double hfov = cs.ptz_fov_horizontal == 0 ? 60 : cs.ptz_fov_horizontal;
			double vfov = cs.ptz_fov_vertical == 0 ? 34 : cs.ptz_fov_vertical;
			int numImagesWide = overlap ? (int)Math.Round((360 / hfov) * 2) : 6;
			int numImagesHigh = overlap ? (int)Math.Round(((cs.ptz_tiltlimit_low - cs.ptz_tiltlimit_high) / (vfov * 10)) * 2) : 3;

			for (int j = 0; j < numImagesHigh; j++)
			{
				for (int i = 0; i < numImagesWide; i++)
				{
					FloatVector3 percentPos = new FloatVector3((float)i / (float)numImagesWide, (float)j / (float)numImagesHigh, 0);
					PositionABS_PercentPosition(percentPos);
					Thread.Sleep(isFirstTime ? 7000 : 4500);
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
						if (generatePanoramaSourceFrames)
						{
							generatePanoramaSourceFrames = false;
							GeneratePseudoPanorama(true, true);
							SetNewIdleTime();
						}
						if (generatePseudoPanorama)
						{
							generatePseudoPanorama = false;
							GeneratePseudoPanorama(false, false);
							SetNewIdleTime();
						}
						FloatVector3 desiredAbsPos = (FloatVector3)Interlocked.Exchange(ref desiredPTZPosition, null);
						if (desiredAbsPos != null)
						{
							SetNewIdleTime();

							desiredAbsPos.Y = Util.Clamp(desiredAbsPos.Y, 0, 1);
							desiredAbsPos.Y = desiredAbsPos.Y * (float)(panoramaVerticalDegrees / 90.0);
							desiredAbsPos.Z = Util.Clamp(desiredAbsPos.Z, 0, 1);

							PositionABS_PercentPosition(desiredAbsPos);
							BroadcastStatusUpdate();
							Thread.Sleep(1000);

							//if ("grapefruit" == false.ToString().ToLower())
							//{
							//    GeneratePseudoPanorama(14, 6, true);
							//}
						}

						double[] desiredZoom = (double[])Interlocked.Exchange(ref desiredZoomPosition, null);
						if (desiredZoom != null)
						{
							SetNewIdleTime();
							FloatVector3 percentPos = currentPTZPosition.Copy();
							float newZ = (float)Util.Clamp(desiredZoom[0], 0, 1);
							float diffZ = Math.Abs(newZ - percentPos.Z);
							percentPos.Z = newZ;
							PositionABS_PercentPosition(percentPos);
							BroadcastStatusUpdate();
							Thread.Sleep((int)(diffZ * 1000));
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
							this.Position3D(x, y, z);
							Broadcast3dPosition(x, y, w, h, zIn);
							BroadcastStatusUpdate();
							Thread.Sleep(500);
						}
						Thread.Sleep(10);
						if (DateTime.Now > willBeIdleAt)
						{
							willBeIdleAt = DateTime.MaxValue;
							desiredAbsPos = new FloatVector3((float)cs.ptz_idleresetpositionX, (float)cs.ptz_idleresetpositionY, (float)cs.ptz_idleresetpositionZ);
							desiredAbsPos.Y = Util.Clamp(desiredAbsPos.Y, 0, 1);
							desiredAbsPos.Z = Util.Clamp(desiredAbsPos.Z, 0, 1);

							PositionABS_PercentPosition(desiredAbsPos);
							BroadcastStatusUpdate();
							Thread.Sleep(1000);
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

		public void Broadcast3dPosition(float x, float y, float w, float h, bool zIn)
		{
			MJpegWrapper.webSocketServer.BroadcastCameraStatusUpdate(cs.id, "3dpos " + x + " " + y + " " + w + " " + h + " " + (zIn ? "1" : "0"));
		}
		public string GetStatusUpdate()
		{
			FloatVector3 cp = currentPTZPosition.Copy();
			cp.Y = cp.Y / (float)(panoramaVerticalDegrees / 90.0);
			return "newpos " + cp.X + " " + cp.Y + " " + cp.Z;
		}
		public void BroadcastStatusUpdate()
		{
			MJpegWrapper.webSocketServer.BroadcastCameraStatusUpdate(cs.id, GetStatusUpdate());
		}

		public void MoveSimple(int xAmount, int yAmount, int zAmount)
		{
			DoAction("start", "Position", xAmount, yAmount, zAmount);
		}

		public void SetAbsolutePTZPosition(FloatVector3 pos)
		{
			desiredPTZPosition = pos;
		}
		public void Set3DPTZPosition(Pos3d pos)
		{
			System.Threading.Interlocked.Exchange(ref desired3dPosition, pos);
		}
		public void SetZoomPosition(double z)
		{
			desiredZoomPosition = new double[] { z };
		}
		public void GeneratePanorama(bool full)
		{
			if (full)
				generatePanoramaSourceFrames = true;
			else
				generatePseudoPanorama = true;
		}

		public void PositionABS_PercentPosition(FloatVector3 percentagePosition)
		{
			IntVector3 camPosition = PercentagePosToCameraPos(percentagePosition);
			PositionABS_CamPosition(camPosition);
		}
		public void PositionABS_CamPosition(IntVector3 camPosition)
		{
			camPosition.X = Util.Modulus(camPosition.X, 3600);

			camPosition.Y = Util.Clamp(camPosition.Y, 0, 900);

			camPosition.Z = Util.Clamp(camPosition.Z, 1, 128);

			FloatVector3 percentPos = CameraPosToPercentagePos(camPosition);
			currentPTZPosition.X = percentPos.X;
			currentPTZPosition.Y = percentPos.Y;
			currentPTZPosition.Z = percentPos.Z;

			DoAbsPos(camPosition.X, camPosition.Y, camPosition.Z);
		}

		private void DoAbsPos(int x, int y, int z)
		{
			Console.WriteLine("ab x:" + x + ", y:" + y + ", z:" + z);
			DoAction("start", "PositionABS", x, y, z);
		}
		/// <summary>
		/// Positions the camera to center on the specified location, with the specified zoom change.
		/// </summary>
		/// <param name="x">Number between 0 and 1 indicating the X position on the camera view that is the center of the user's drawn rectangle.</param>
		/// <param name="y">Number between 0 and 1 indicating the Y position on the camera view that is the center of the user's drawn rectangle.</param>
		/// <param name="z">Number between -1 and 1 indicating the size of the rectangle drawn relative to the size of the camera.  Negative values indicate the zoom should be out, positive values indicate the zoom should be in.</param>
		public void Position3D(float x, float y, float z)
		{
			x -= 0.5f;
			y -= 0.5f;

			FloatVector3 percentagePosition = currentPTZPosition.Copy();
			IntVector3 camPosition = PercentagePosToCameraPos(percentagePosition);

			int currentMagnification = Util.PercentageToRangeValueInt(percentagePosition.Z, 1, cs.ptz_magnification);

			double hfov = cs.ptz_fov_horizontal / currentMagnification;
			double vfov = cs.ptz_fov_vertical / currentMagnification;

			double offsetDegreesX = hfov * x;
			double offsetDegreesY = vfov * y;

			IntVector3 newCamPosition = new IntVector3();
			newCamPosition.X = camPosition.X + (int)Math.Round(offsetDegreesX * -10);
			newCamPosition.Y = camPosition.Y + (int)Math.Round(offsetDegreesY * 10);

			// Calculate new zoom position
			if (z == 0)
				newCamPosition.Z = camPosition.Z;
			else
			{
				double offsetMultiplierZ;
				if (z > 0)
					offsetMultiplierZ = 1.0 / z;
				else
					offsetMultiplierZ = -z;
				double newMagnification = currentMagnification * offsetMultiplierZ;
				if (newMagnification < 1)
					newMagnification = 1;
				else if (newMagnification > cs.ptz_magnification)
					newMagnification = cs.ptz_magnification;
				double newPercentMag = Util.DahuaZoomCalc(newMagnification, cs.ptz_magnification);
				newCamPosition.Z = (int)Math.Round(newPercentMag);
			}

			PositionABS_CamPosition(newCamPosition);
		}
		private FloatVector3 CameraPosToPercentagePos(IntVector3 camPos)
		{
			FloatVector3 percentagePos = new FloatVector3();

			percentagePos.X = (float)Util.RangeValueToPercentage(Util.Modulus(camPos.X - (this.absoluteXOffset * 10), 3600), 3600, 0);
			percentagePos.Y = (float)Util.RangeValueToPercentage(camPos.Y, 0, 900);
			double approxMagnification = Util.DahuaMagnificationCalc(camPos.Z, cs.ptz_magnification);
			percentagePos.Z = (float)Util.RangeValueToPercentage(approxMagnification, 1, cs.ptz_magnification);

			return percentagePos;
		}
		private IntVector3 PercentagePosToCameraPos(FloatVector3 percentagePos)
		{
			IntVector3 camPos = new IntVector3();

			camPos.X = Util.Modulus(Util.PercentageToRangeValueInt(percentagePos.X, 3600, 0) + (this.absoluteXOffset * 10), 3600);
			camPos.Y = Util.PercentageToRangeValueInt(percentagePos.Y, 0, 900);
			double magnification = Util.PercentageToRangeValueDouble(percentagePos.Z, 1, cs.ptz_magnification);
			camPos.Z = Util.DahuaZoomCalc(magnification, cs.ptz_magnification);

			return camPos;
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
	#endregion
}

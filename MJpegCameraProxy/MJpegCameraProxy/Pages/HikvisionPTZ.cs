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

namespace MJpegCameraProxy.Hikvision
{
	public class HikvisionPTZ : AdvPtzController
	{
		string baseCGIURL;
		string host, user, pass;
		System.Threading.Timer keepAliveTimer = null;
		int session = -1;
		double thumbnailBoxWidth, thumbnailBoxHeight;
		int absoluteXOffset, panoramaVerticalDegrees;
		bool simplePanorama;
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

		static HikvisionPTZ()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			if (System.Net.ServicePointManager.DefaultConnectionLimit < 16)
				System.Net.ServicePointManager.DefaultConnectionLimit = 16;
		}

		public HikvisionPTZ(CameraSpec cs)
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

			baseCGIURL = "http://" + host + "/PTZCtrl/channels/1/";
			PrepareWorkerThreadIfNecessary();
		}

		private void SetNewIdleTime()
		{
			if (cs.ptz_enableidleresetposition)
				this.willBeIdleAt = DateTime.Now.Add(TimeSpan.FromSeconds(cs.ptz_idleresettimeout));
			else
				this.willBeIdleAt = DateTime.MaxValue;
		}

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

			if (!isAboutToStart)
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
				GetCurrentPosition();
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
							double maxTilt = cs.ptz_tiltlimit_low - cs.ptz_tiltlimit_high;
							if(maxTilt > 0)
								desiredAbsPos.Y = desiredAbsPos.Y * (float)((panoramaVerticalDegrees * 10) / maxTilt);
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
							FloatVector3 percentPos = GetCurrentPosition();
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
			double maxTilt = cs.ptz_tiltlimit_low - cs.ptz_tiltlimit_high;
			if (maxTilt > 0)
				cp.Y = cp.Y / (float)((panoramaVerticalDegrees * 10) / maxTilt);
			return "newpos " + cp.X + " " + cp.Y + " " + cp.Z;
		}
		public void BroadcastStatusUpdate()
		{
			MJpegWrapper.webSocketServer.BroadcastCameraStatusUpdate(cs.id, GetStatusUpdate());
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

			camPosition.Y = Util.Clamp(camPosition.Y, cs.ptz_tiltlimit_high, cs.ptz_tiltlimit_low);

			camPosition.Z = Util.Clamp(camPosition.Z, 10, (int)(cs.ptz_magnification * 10));

			FloatVector3 percentPos = CameraPosToPercentagePos(camPosition);
			currentPTZPosition.X = percentPos.X;
			currentPTZPosition.Y = percentPos.Y;
			currentPTZPosition.Z = percentPos.Z;

			DoAbsPos(camPosition.X, camPosition.Y, camPosition.Z);
		}

		private void DoAbsPos(int x, int y, int z)
		{
			Console.WriteLine("ab x:" + x + ", y:" + y + ", z:" + z);
			DoXmlPTZAction("absolute", @"<PTZData version=""1.0"" xmlns=""http://www.hikvision.com/ver10/XMLSchema"">
<AbsoluteHigh>
<elevation>" + y + @"</elevation>
<azimuth>" + x + @"</azimuth>
<absoluteZoom>" + z + @"</absoluteZoom>
</AbsoluteHigh>
</PTZData>");
		}
		/// <summary>
		/// Position the camera to center upon the specified point, relative to the current camera position.  Optionally may zoom the camera in or out.
		/// </summary>
		/// <param name="x">The percentage [0.0 ~ 1.0] that specifies the horizontal position, where 0 is the left side of the view.</param>
		/// <param name="y">The percentage [0.0 ~ 1.0] that specifies the vertical position, where 0 is the top of the view.</param>
		/// <param name="z">The percentage [-1.0 ~ 1.0] that specifies the size of the drawn box.  0 would indicate no box was drawn, -1 would indicate the largest possible zoom-out box was drawn, and 1 would indicate the largest possible zoom-in box was drawn.</param>
		public void Position3D(float x, float y, float z)
		{
			// Calculate new X/Y position
			x -= 0.5f;
			y -= 0.5f;

			FloatVector3 percentagePosition = GetCurrentPosition();
			IntVector3 camPosition = PercentagePosToCameraPos(percentagePosition);

			double zoomMultiplier = camPosition.Z / 10.0;
			double hfov = cs.ptz_fov_horizontal / zoomMultiplier;
			double vfov = cs.ptz_fov_vertical / zoomMultiplier;

			double offsetDegreesX = hfov * x;
			double offsetDegreesY = vfov * y;

			IntVector3 newCamPosition = new IntVector3();
			newCamPosition.X = camPosition.X + (int)Math.Round(offsetDegreesX * 10);
			newCamPosition.Y = camPosition.Y + (int)Math.Round(offsetDegreesY * 10);

			// Calculate new zoom position
			if (z == 0)
				newCamPosition.Z = camPosition.Z;
			else
			{
				int zoomOffset = 10;
				int zoomRange = (int)(cs.ptz_magnification * 10) - zoomOffset;
				if (z > 0)
				{
					double offsetMultiplierZ = 1.0 / z;
					newCamPosition.Z = (int)Math.Round(camPosition.Z * offsetMultiplierZ);
				}
				else
				{
					//double offsetMultiplierZ = 1.0 / z;
					newCamPosition.Z = (int)Math.Round(camPosition.Z * -z);
				}
			}

			PositionABS_CamPosition(newCamPosition);
		}

		private static Regex rxGetElevation = new Regex("<elevation>(.*?)</elevation>", RegexOptions.Compiled);
		private static Regex rxGetAzimuth = new Regex("<azimuth>(.*?)</azimuth>", RegexOptions.Compiled);
		private static Regex rxGetAbsoluteZoom = new Regex("<absoluteZoom>(.*?)</absoluteZoom>", RegexOptions.Compiled);
		public FloatVector3 GetCurrentPosition()
		{
			string posResponse = DoXmlPTZAction("status", null);

			IntVector3 camPos = new IntVector3();
			Match m = rxGetElevation.Match(posResponse);
			if (!m.Success || !int.TryParse(m.Groups[1].Value, out camPos.Y))
				camPos.Y = 0;

			m = rxGetAzimuth.Match(posResponse);
			if (!m.Success || !int.TryParse(m.Groups[1].Value, out camPos.X))
				camPos.X = 0;

			m = rxGetAbsoluteZoom.Match(posResponse);
			if (!m.Success || !int.TryParse(m.Groups[1].Value, out camPos.Z))
				camPos.Z = 0;

			if (camPos.Z <= 0)
				camPos.Z = 10;

			FloatVector3 percentagePos = CameraPosToPercentagePos(camPos);

			currentPTZPosition.X = percentagePos.X;
			currentPTZPosition.Y = percentagePos.Y;
			currentPTZPosition.Z = percentagePos.Z;

			return percentagePos;
		}

		private FloatVector3 CameraPosToPercentagePos(IntVector3 camPos)
		{
			FloatVector3 percentagePos = new FloatVector3();

			percentagePos.X = (float)Util.RangeValueToPercentage(Util.Modulus(camPos.X - (this.absoluteXOffset * 10), 3600), 0, 3600);
			percentagePos.Y = (float)Util.RangeValueToPercentage(camPos.Y, cs.ptz_tiltlimit_high, cs.ptz_tiltlimit_low);
			percentagePos.Z = (float)Util.RangeValueToPercentage(camPos.Z, 10, cs.ptz_magnification * 10);

			return percentagePos;
		}
		private IntVector3 PercentagePosToCameraPos(FloatVector3 percentagePos)
		{
			IntVector3 camPos = new IntVector3();

			camPos.X = Util.Modulus(Util.PercentageToRangeValueInt(percentagePos.X, 0, 3600) + (this.absoluteXOffset * 10), 3600);
			camPos.Y = Util.PercentageToRangeValueInt(percentagePos.Y, cs.ptz_tiltlimit_high, cs.ptz_tiltlimit_low);
			camPos.Z = Util.PercentageToRangeValueInt(percentagePos.Z, 10, cs.ptz_magnification * 10);

			return camPos;
		}

		private string DoXmlPTZAction(string action, string xml)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(baseCGIURL + action);
			Console.ForegroundColor = ConsoleColor.White;
			if (xml == null)
				Console.WriteLine("null");
			else
				Console.WriteLine(xml);
			Console.ResetColor();
			string response = HttpPost(baseCGIURL + action, xml);
			Console.WriteLine(response);
			Console.WriteLine();
			return response;
		}
		int requestNumber = 0;
		protected string HttpPost(string URI, string data)
		{
			int myRequestNumber = Interlocked.Increment(ref requestNumber);
			//File.AppendAllText(Globals.ApplicationDirectoryBase + "HikvisionPTZ.txt", DateTime.Now.ToString() + " " + requestNumber + " Request " + URI + Environment.NewLine + data + Environment.NewLine);
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				req.Proxy = null;
				req.PreAuthenticate = true;
				req.Method = data == null ? "GET" : "PUT";
				req.Credentials = new NetworkCredential(user, pass);

				if (data != null)
				{
					byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
					req.ContentLength = bytes.Length;

					using (Stream os = req.GetRequestStream())
					{
						os.Write(bytes, 0, bytes.Length);
					}
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					string responseText = sr.ReadToEnd();
					//File.AppendAllText(Globals.ApplicationDirectoryBase + "HikvisionPTZ.txt", DateTime.Now.ToString() + " " + requestNumber + " Response " + URI + Environment.NewLine + responseText + Environment.NewLine);
					return responseText;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
				return ex.ToString();
			}
		}
	}
}

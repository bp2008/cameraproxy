using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Drawing;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy
{
	public class CameraManager
	{
		bool stopping = false;
		ConcurrentDictionary<string, IPCameraBase> cameras = new ConcurrentDictionary<string, IPCameraBase>();

		Thread threadCameraIdleWatcher;
		public CameraManager()
		{
			threadCameraIdleWatcher = new Thread(UnviewedCatcherLoop);
			threadCameraIdleWatcher.Name = "Unviewed Camera Stopper Thread";
			threadCameraIdleWatcher.Start();
		}

		/// <summary>
		/// Can be null if the camera does not exist!
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IPCameraBase GetCamera(string id)
		{
			return GetCameraById(id);
		}
		public IPCameraBase GetCameraAndGetItRunning(string id)
		{
			GetLatestImage(id);
			return GetCamera(id);
		}
		public byte[] GetLatestImage(string id)
		{
			IPCameraBase cam = GetCameraById(id);
			if (cam == null || cam.cameraSpec.type == CameraType.h264)
				return new byte[0];
			lock (this)
			{
				if (!cam.isRunning)
					cam.Start();
			}
			int timesWaited = 0;
			byte[] frame = cam.LastFrame;
			while (frame.Length == 0 && timesWaited++ < 250)
			{
				Thread.Sleep(20);
				frame = cam.LastFrame;
			}
			return frame;
		}
		public string GetRTSPUrl(string id, SimpleHttp.HttpProcessor p)
		{
			IPCameraBase cam = GetCameraById(id);
			if (cam == null || cam.cameraSpec.type != CameraType.h264)
				return "";
			cam.ImageLastViewed = DateTime.Now;
			if (!cam.isRunning)
			{
				cam.Start();
				int timesWaited = 0;
				Thread.Sleep(50);
				while (timesWaited++ < 50)
					Thread.Sleep(20);
				cam.ImageLastViewed = DateTime.Now;
			}
			H264Camera h264cam = (H264Camera)cam;
			return "rtsp://" + h264cam.access_username + ":" + h264cam.access_password + "@$$$HOST$$$:" + cam.cameraSpec.h264_port + "/proxyStream";
		}

		/// <summary>
		/// Returns the minimum permission level required to view the camera (0 to 100).  If the camera does not exist, returns 101.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public int GetCameraMinPermission(string id)
		{
			IPCameraBase cam = GetCameraById(id);
			if (cam == null)
				return 101;
			return cam.cameraSpec.minPermissionLevel;
		}
		public int GetCameraDelayBetweenImageGrabs(string id)
		{
			IPCameraBase cam = GetCameraById(id);
			if (cam == null)
				return 0;
			return cam.cameraSpec.delayBetweenImageGrabs;
		}
		private IPCameraBase GetCameraById(string id)
		{
			if (id == null)
				return null;
			id = id.ToLower();

			IPCameraBase cam;
			// Try to get the camera reference.
			if (!cameras.TryGetValue(id, out cam))
			{
				// Camera object has not been created yet
				CameraSpec cs = MJpegWrapper.cfg.GetCameraSpec(id);
				if (cs != null && cs.enabled)
				{
					// We have a definition for it.
					cam = IPCameraBase.CreateCamera(cs);
					if (cam == null)
						return null;
					if (cameras.TryAdd(id, cam))
					{
						// Camera just got added
					}
					else
					{
						// Camera was added by another thread
						if (!cameras.TryGetValue(id, out cam))
							return null; // And yet it doesn't exist. Fail.
					}
				}
				else
				{
					// Camera has not been created yet, and we have no definition for it.
					return null;
				}
			}
			return cam;
		}
		#region Unviewed Camera Catcher Loop
		/// <summary>
		/// Stops cameras that haven't been viewed recently
		/// </summary>
		private void UnviewedCatcherLoop()
		{
			int counter = 0;
			while (!stopping)
			{
				try
				{
					foreach (IPCameraBase cam in cameras.Values)
						if (cam.isRunning && cam.ImageLastViewed.AddSeconds(cam.cameraSpec.GetMaxBufferTime()) < DateTime.Now)
							cam.Stop();
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
				counter = 0;
				while (!stopping && counter++ < 4)
					Thread.Sleep(250);
			}
		}
		#endregion

		public void KillCamera(string id)
		{
			IPCameraBase cam;
			if (cameras.TryGetValue(id, out cam))
			{
				cam.Stop();
				if (!cameras.TryRemove(id, out cam))
					cameras.TryRemove(id, out cam);
				cam.Stop();
			}
		}

		/// <summary>
		/// Stops all running cameras.
		/// </summary>
		public void Stop()
		{
			stopping = true;
			foreach (IPCameraBase cam in cameras.Values)
			{
				cam.Stop();
			}
		}

		/// <summary>
		/// Returns the minimum permission level required to view the specified camera (0 to 100).  Returns 101 if the camera is null.
		/// </summary>
		/// <param name="cameraId">The camera ID string.</param>
		/// <returns></returns>
		public int GetMinPermissionLevel(string cameraId)
		{
			IPCameraBase cam = GetCameraById(cameraId);
			if (cam == null)
				return 101;
			return cam.cameraSpec.minPermissionLevel;
		}
		public void CleanUpCameraOrder()
		{
			lock (MJpegWrapper.cfg)
			{
				MJpegWrapper.cfg.cameras.Sort(new ComparisonComparer<CameraSpec>((c1, c2) => 
				{
					int diff = c1.order.CompareTo(c2.order);
					if (diff == 0)
						diff = c1.id.CompareTo(c2.id);
					return diff;
				}));
				for (int i = 0; i < MJpegWrapper.cfg.cameras.Count; i++)
					MJpegWrapper.cfg.cameras[i].order = i;
			}
		}
		public List<string> GenerateAllCameraIdList()
		{
			List<string> cams = new List<string>();
			lock (MJpegWrapper.cfg)
			{
				foreach (CameraSpec cs in MJpegWrapper.cfg.cameras)
					if (cs.type == CameraType.jpg || cs.type == CameraType.mjpg)
						cams.Add(cs.id);
			}
			return cams;
		}

		public string GenerateAllCameraIdNameList(int permission)
		{
			List<string> cams = new List<string>();
			lock (MJpegWrapper.cfg)
			{
				foreach (CameraSpec cs in MJpegWrapper.cfg.cameras)
					if (cs.type == CameraType.jpg || cs.type == CameraType.mjpg)
						if (cs.enabled && cs.minPermissionLevel <= permission)
							cams.Add("['" + cs.id + "','" + cs.name + "']");
			}
			return "[" + string.Join(",", cams) + "]";
		}
	}
}

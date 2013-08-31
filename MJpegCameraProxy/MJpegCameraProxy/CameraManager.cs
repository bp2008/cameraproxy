using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Drawing;

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
			if (cam == null)
				return new byte[0];
			if (!cam.isRunning)
				cam.Start();
			int timesWaited = 0;
			byte[] frame = cam.LastFrame;
			while (frame.Length == 0 && timesWaited++ < 250)
			{
				Thread.Sleep(20);
				frame = cam.LastFrame;
			}
			return frame;
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
				// Camera has not been created yet
				if (id.Contains('\\') || id.Contains('/') || id.Contains('.') || id.Contains(':'))
					return null; // Invalid character in camera name.
				if (File.Exists(Globals.CamsDirectoryBase + id))
				{
					// We have a definition file for it.
					cam = IPCameraBase.CreateCamera(id);
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
					// Camera has not been created yet, and we have no definition file for it.
					return null;
				}
			}
			return cam;
		}
		private void UnviewedCatcherLoop()
		{
			int counter = 0;
			while (!stopping)
			{
				try
				{
					DateTime cutoff = DateTime.Now.AddSeconds(-10);
					foreach (IPCameraBase cam in cameras.Values)
						if (cam.isRunning && cam.ImageLastViewed < cutoff)
							cam.Stop();
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
				counter = 0;
				while (!stopping && counter++ < 40)
					Thread.Sleep(250);
			}
		}
		public void Stop()
		{
			stopping = true;
			foreach (IPCameraBase cam in cameras.Values)
			{
				cam.Stop();
			}
		}

		public bool IsPrivate(string cameraId)
		{
			IPCameraBase cam = GetCameraById(cameraId);
			if (cam == null)
				return false;
			return cam.isPrivate;
		}

		public List<string> GenerateAllCameraIdList()
		{
			List<string> cams = new List<string>();
			try
			{
				DirectoryInfo diCams = new DirectoryInfo(Globals.CamsDirectoryBase);
				FileInfo[] files = diCams.GetFiles();
				foreach (FileInfo file in files)
					cams.Add(file.Name);
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return cams;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Drawing;

namespace MJpegCameraProxy
{
	public enum PTZType
	{
		None,
		LoftekCheap,
		Dahua
	}
	public abstract class IPCameraBase
	{
		public SemaphoreSlim ptzLock = new SemaphoreSlim(1, 1);
		public static volatile bool ApplicationExiting = false;
		public DateTime ImageLastViewed = DateTime.Now.AddSeconds(10);
		public volatile bool isRunning = false;
		public bool isPrivate;
		Thread threadDownloadStream;
		protected volatile byte[] lastFrame = new byte[0];

		public PTZType ptzType = PTZType.None;

		public DahuaPTZ dahuaPtz = null;

		protected int width = 0;

		protected string id;
		public string ID
		{
			get { return id; }
		}

		protected bool Exit
		{
			get
			{
				if (!isRunning)
					return true;
				if (ApplicationExiting)
					return true;
				return false;
			}
		}
		protected Size imageSize;
		public Size ImageSize
		{
			get
			{
				if ((imageSize.Width == 0 || imageSize.Height == 0) && LastFrame != null && LastFrame.Length != 0)
				{
					Image img;
					using (MemoryStream ms = new MemoryStream(LastFrame))
					{
						img = Image.FromStream(ms);
					}
					imageSize = img.Size;
				}
				return imageSize;
			}
		}

		public byte[] LastFrame
		{
			get
			{
				ImageLastViewed = DateTime.Now;
				return lastFrame;
			}
		}
		public void Start()
		{
			Stop(true);
			isRunning = true;
			ImageLastViewed = DateTime.Now.AddSeconds(10);
			threadDownloadStream = new Thread(downloadLoop);
			threadDownloadStream.Name = "M/Jpeg Download Stream";
			threadDownloadStream.Start();
		}
		public void Stop(bool isAboutToStart = false)
		{
			if (dahuaPtz != null)
				dahuaPtz.Shutdown(isAboutToStart);
			if (threadDownloadStream != null)
			{
				threadDownloadStream.Abort();
			}
			isRunning = false;
			lastFrame = new byte[0];
		}
		private void downloadLoop()
		{
			try
			{
				DoBackgroundWork();
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
		protected abstract void DoBackgroundWork();

		public static IPCameraBase CreateCamera(string id)
		{
			string[] lines = File.ReadAllLines(Globals.CamsDirectoryBase + id);
			if (lines.Length < 2)
				return null;
			string un = "", pw = "";
			bool bPrivate = false;
			PTZType type = PTZType.None;
			if (lines.Length >= 4)
			{
				un = lines[2];
				pw = lines[3];
			}
			if (lines.Length >= 5)
			{
				bPrivate = Util.ParseBool(lines[4]);
			}
			DahuaPTZ dahuaPtz = null;
			if (lines.Length >= 6)
			{
				if(lines[5].ToLower() == "loftekcheap")
					type = PTZType.LoftekCheap;
				else if (lines[5].ToLower().StartsWith("dahua"))
				{
					type = PTZType.Dahua;
					string[] parts = lines[5].Split('/');
					if (parts.Length >= 5)
						dahuaPtz = new DahuaPTZ(parts[1], parts[2], parts[3], int.Parse(parts[4]));
					else if (parts.Length >= 4)
						dahuaPtz = new DahuaPTZ(parts[1], parts[2], parts[3], 180);
				}
			}
			IPCameraBase cam = null;
			if (lines[0].ToLower() == "mjpg")
			{
				cam = new MJpegCamera(lines[1], un, pw);
			}
			else if (lines[0].ToLower() == "jpg")
			{
				cam = new JpegRefreshCamera(lines[1], un, pw);
			}

			if (cam != null)
			{
				cam.isPrivate = bPrivate;
				cam.ptzType = type;
				cam.dahuaPtz = dahuaPtz;
			}

			cam.id = id;

			return cam;
		}
	}
}

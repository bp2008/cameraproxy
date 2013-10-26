using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Drawing;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy
{
	public abstract class IPCameraBase
	{
		public SemaphoreSlim ptzLock = new SemaphoreSlim(1, 1);
		/// <summary>
		/// Use only with jpg and mjpg cameras only.  This EventWaitHandle is signaled and then reset just after each new frame is made available.
		/// </summary>
		public EventWaitHandle newFrameWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
		public DateTime ImageLastViewed = DateTime.Now.AddSeconds(10);
		public volatile bool isRunning = false;
		Thread threadDownloadStream;
		protected volatile byte[] lastFrame = new byte[0];

		public CameraSpec cameraSpec;

		public DahuaPTZ dahuaPtz = null;

		protected int width = 0;

		/// <summary>
		/// If true, the download loop should return as soon as possible.
		/// </summary>
		protected bool Exit
		{
			get
			{
				if (!isRunning)
					return true;
				if (MJpegWrapper.ApplicationExiting)
					return true;
				return false;
			}
		}

		protected Size imageSize;
		/// <summary>
		/// Gets the size of the image being sent by the camera.
		/// </summary>
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

		/// <summary>
		/// Returns the last frame decoded by the camera.  Only works with jpg and mjpg cameras.  For h264 cameras, this will not return a valid frame.
		/// </summary>
		public virtual byte[] LastFrame
		{
			get
			{
				ImageLastViewed = DateTime.Now;
				return lastFrame;
			}
		}

		#region Start / Stop
		public void Start()
		{
			lock (this)
			{
				Stop(true);
				isRunning = true;
				ImageLastViewed = DateTime.Now.AddSeconds(10);
				threadDownloadStream = new Thread(downloadLoop);
				threadDownloadStream.Name = "M/Jpeg Download Stream";
				threadDownloadStream.Start();
			}
		}
		public void Stop(bool isAboutToStart = false)
		{
			lock (this)
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
		}
		#endregion
		#region Download Loop
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
		#endregion

		public static IPCameraBase CreateCamera(CameraSpec cs)
		{
			IPCameraBase cam = null;
			switch (cs.type)
			{
				case CameraType.jpg:
					cam = new JpegRefreshCamera();
					break;
				case CameraType.mjpg:
					cam = new MJpegCamera();
					break;
				case CameraType.h264_rtsp_proxy:
					// h264_rtsp_proxy cameras do not support jpeg output - only stream reflection via live555ProxyServer
					cam = new H264Camera();
					break;
				case CameraType.vlc_transcode:
					// h264_rtsp_proxy cameras do not support jpeg output - only stream reflection via live555ProxyServer
					cam = new VlcCamera();
					break;
				default:
					break;
			}
			if (cam != null)
			{
				cam.cameraSpec = cs;
				if (cs.ptzType == PtzType.Dahua)
					cam.dahuaPtz = new DahuaPTZ(cs.ptz_hostName, cs.ptz_username, cs.ptz_password, cs.ptz_absoluteXOffset, cs.ptz_panorama_selection_rectangle_width, cs.ptz_panorama_selection_rectangle_height, cs.ptz_panorama_simple, cs.ptz_panorama_degrees_vertical);
			}
			return cam;
		}
	}
}

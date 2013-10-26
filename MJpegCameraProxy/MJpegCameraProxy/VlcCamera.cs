using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// nVlc includes
using Declarations;
using Declarations.Enums;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace MJpegCameraProxy
{
	public class VlcCamera : IPCameraBase
	{
		private IMemoryRenderer memRender;
		private Stopwatch frameCounter = new Stopwatch();
		private long nextFrameDecodeTime = 0;
		private long frameDecodeInterval = 0;
		private bool muted = false;

		//private Bitmap latestBitmap = null;
		//private Bitmap lastEncodedBitmap = null;
		private byte[] latestFrame = new byte[0];
		public override byte[] LastFrame
		{
			get
			{
				if (!Exit)
					ImageLastViewed = DateTime.Now;
				//Bitmap bmp = latestBitmap;
				//if (bmp != null)
				//{
				//    if (bmp != lastEncodedBitmap)
				//    {
				//        lock (bmp)
				//        {
				//            if (bmp != lastEncodedBitmap)
				//            {
				//                try
				//                {
				//                    latestFrame = ImageConverter.GetJpegBytes(bmp);
				//                }
				//                catch (Exception ex)
				//                {
				//                    Logger.Debug(ex);
				//                }
				//                lastEncodedBitmap = bmp;
				//            }
				//        }
				//    }
				//}
				return latestFrame;
			}
		}
		internal VlcCamera()
		{
		}
		protected override void DoBackgroundWork()
		{
			try
			{
				frameDecodeInterval = 1000 / this.cameraSpec.vlc_transcode_fps;
				int w = this.cameraSpec.h264_video_width;
				int h = this.cameraSpec.h264_video_height;
				if (w <= 0 || h <= 0)
				{
					w = 640;
					h = 480;
				}

				IVideoPlayer player = null;

				while (!Exit)
				{
					try
					{
						nextFrameDecodeTime = 0;
						muted = false;
						frameCounter.Start();
						IMediaPlayerFactory factory = new MediaPlayerFactory();
						player = factory.CreatePlayer<IVideoPlayer>();
						int b = cameraSpec.vlc_transcode_buffer_time;
						string[] args = new string[] { ":rtsp-caching=" + b, ":realrtsp-caching=" + b, ":network-caching=" + b, ":udp-caching=" + b, ":volume=0" };
						IMedia media = factory.CreateMedia<IMedia>(this.cameraSpec.imageryUrl, args);
						memRender = player.CustomRenderer;
						memRender.SetCallback(delegate(Bitmap frame)
						{
							if (!player.Mute)
								player.ToggleMute();
							long time = frameCounter.ElapsedMilliseconds;
							if (time >= nextFrameDecodeTime)
							{
								latestFrame = ImageConverter.GetJpegBytes(frame);
								nextFrameDecodeTime = time + frameDecodeInterval;
							}
							//latestBitmap = new Bitmap(frame);  // frame.Clone() actually doesn't copy the data and exceptions get thrown
						});

						memRender.SetFormat(new BitmapFormat(w, h, ChromaType.RV24));
						player.Open(media);
						player.Play();
						while (!Exit)
							Thread.Sleep(50);
					}
					catch (ThreadAbortException ex)
					{
						throw ex;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
					finally
					{
						frameCounter.Stop();
						frameCounter.Reset();
						if (player != null)
							player.Stop();
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
	}
}

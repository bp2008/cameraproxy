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
		private Stopwatch frameTimer = new Stopwatch();
		private long nextFrameEncodeTime = 0;
		private long frameEncodeInterval = 0;

		private uint frameNumber = 0;
		private uint lastFrameEncoded = 0;
		private object frameLock = new object();

		private byte[] latestFrame = new byte[0];
		public override byte[] LastFrame
		{
			get
			{
				if (!Exit)
					ImageLastViewed = DateTime.Now;

				if (lastFrameEncoded == frameNumber)
					return latestFrame; // The current frame was already encoded

				if (frameTimer.ElapsedMilliseconds < nextFrameEncodeTime)
					return latestFrame; // It is not yet time to encode a new frame.

				// If we get here, we need to encode a new frame
				lock (frameLock) // Lock and check conditions again to ensure we don't encode more than once.
				{
					long time = frameTimer.ElapsedMilliseconds;
					if (lastFrameEncoded != frameNumber && time >= nextFrameEncodeTime)
					{
						// If we get here, we still need to encode a new frame.
						Bitmap bmp = null;
						try
						{
							bmp = memRender.CurrentFrame;
							lastFrameEncoded = frameNumber;
							latestFrame = ImageConverter.EncodeBitmap(bmp, 100, cameraSpec.vlc_transcode_image_quality, ImageFormat.Jpeg, cameraSpec.vlc_transcode_rotate_flip);
							EventWaitHandle oldWaitHandle = newFrameWaitHandle;
							newFrameWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
							oldWaitHandle.Set();
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
						finally
						{
							if (bmp != null)
								bmp.Dispose();
						}
						// Now, this is the tricky part.  We must schedule the next frame such that we can come as close as possible to the frame rate goal without ever exceeding it.

						// If this camera's imagery is being requested often enough, all we need to do is add the frame encode interval to the next frame encode time.
						nextFrameEncodeTime += frameEncodeInterval;

						// The above scheduling method will fall behind very often if this camera's imagery is being requested at lower rate.
						if (time >= nextFrameEncodeTime)
						{
							// Image requests are not keeping up with the frame rate goal.  Throttle the encoding schedule to ensure we don't see a CPU usage burst the next time images are requested rapidly.
							nextFrameEncodeTime = time + frameEncodeInterval;
						}
					}
				}
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
				frameEncodeInterval = 1000 / this.cameraSpec.vlc_transcode_fps;
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
						frameNumber = 0;
						lastFrameEncoded = 0;
						nextFrameEncodeTime = 0;
						frameTimer.Start();
						IMediaPlayerFactory factory = new MediaPlayerFactory();
						player = factory.CreatePlayer<IVideoPlayer>();
						int b = cameraSpec.vlc_transcode_buffer_time;
						string[] args = new string[] { ":rtsp-caching=" + b, ":realrtsp-caching=" + b, ":network-caching=" + b, ":udp-caching=" + b, ":volume=0" };
						IMedia media = factory.CreateMedia<IMedia>(this.cameraSpec.imageryUrl, args);
						memRender = player.CustomRenderer;
						memRender.SetCallback(delegate(Bitmap frame)
						{
							frameNumber++;
							if (!player.Mute)
								player.ToggleMute();
							if (frameTimer.ElapsedMilliseconds >= nextFrameEncodeTime)
							{
								EventWaitHandle oldWaitHandle = newFrameWaitHandle;
								newFrameWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
								oldWaitHandle.Set();
							}
							//long time = frameCounter.ElapsedMilliseconds;
							//if (time >= nextFrameEncodeTime)
							//{
							//    latestFrame = ImageConverter.GetJpegBytes(frame);
							//    nextFrameEncodeTime = time + frameEncodeInterval;
							//}
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
						frameTimer.Stop();
						frameTimer.Reset();
						if (player != null)
							player.Stop();
						newFrameWaitHandle.Set();
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
			newFrameWaitHandle.Set();
		}
	}
}

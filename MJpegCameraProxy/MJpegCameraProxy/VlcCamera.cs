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
using BPUtil;

namespace MJpegCameraProxy
{
	/// <summary>
	/// This class transcodes video in-process to jpeg/mjpeg at a variable frame rate (i.e. frames are not encoded as jpeg unless they are needed).  It is the most efficient method of transcoding video to jpeg/mjpeg, but it only works in Windows.
	/// </summary>
	public class VlcCamera : IPCameraBase
	{
		private IMemoryRenderer memRender;
		private Stopwatch frameTimer = new Stopwatch();
		private long nextFrameEncodeTime = 0;
		private long frameEncodeInterval = 0;
		private long lastTimestampUpdateTime = -1;
		private int w, h;

		private uint frameNumber = 0;
		private uint lastFrameEncoded = 0;
		private object frameLock = new object();

		private bool isWatchdogTimeSurpassed(long time)
		{
			return cameraSpec.vlc_watchdog_time > 0 && 
				(
					(time > nextFrameEncodeTime + (cameraSpec.vlc_watchdog_time * 1000))
				||
					(lastTimestampUpdateTime > -1 && time > lastTimestampUpdateTime + (cameraSpec.vlc_watchdog_time * 1000))
				);
		}

		public override byte[] LastFrame
		{
			get
			{

				if (!Exit)
					ImageLastViewed = DateTime.Now;

				long time = frameTimer.ElapsedMilliseconds;

				if (!isWatchdogTimeSurpassed(time) && lastFrameEncoded == frameNumber)
					return lastFrame; // The last encoded frame is still the best we have

				if (time < nextFrameEncodeTime)
					return lastFrame; // It is not yet time to encode a new frame.

				if (cameraSpec.vlc_transcode_throwaway_frames > frameNumber)
					return lastFrame; // This frame should be dropped

				// If we get here, it is time to encode a new frame
				lock (frameLock) // Lock and check conditions again to ensure we don't encode the same frame more than once.
				{
					if (isRunning)
					{
						if (lastFrameEncoded != frameNumber && time >= nextFrameEncodeTime)
						{
							// If we get here, we still need to encode a new frame.  One is ready.
							Bitmap bmp = null;
							WrappedImage wi = null;
							try
							{
								bmp = memRender.CurrentFrame;
								lastFrameEncoded = frameNumber;
								wi = new WrappedImage(bmp);
								lastFrame = ImageConverter.EncodeImage(wi, 100, cameraSpec.vlc_transcode_image_quality, ImageFormat.Jpeg, cameraSpec.vlc_transcode_rotate_flip);
								EventWaitHandle oldWaitHandle = newFrameWaitHandle;
								newFrameWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
								oldWaitHandle.Set();
							}
							catch (Exception ex)
							{
								string frameSize = bmp == null ? "bmp is null" : "bmp size: " + bmp.Width + "x" + bmp.Height;
								Logger.Debug(ex, "vlc_transcode Camera ID: " + this.cameraSpec.id + ", " + frameSize);
							}
							finally
							{
								if (bmp != null)
									bmp.Dispose();
								if (wi != null)
									wi.Dispose();
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
						else if (isWatchdogTimeSurpassed(time))
						{
							// If we get here, the watchdog time has been surpassed and there isn't a frame available for encoding.
							Logger.Debug("Watchdog timeout for camera \"" + cameraSpec.id + "\"");
							Stop();
						}
					}
				}
				return lastFrame;
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
				w = this.cameraSpec.h264_video_width;
				h = this.cameraSpec.h264_video_height;
				if (w <= 0 || h <= 0)
					w = h = 0;

				IVideoPlayer player = null;

				while (!Exit)
				{
					try
					{
						frameNumber = 0;
						lastFrameEncoded = 0;
						nextFrameEncodeTime = 0;
						lastTimestampUpdateTime = -1;
						frameTimer.Start();
						IMediaPlayerFactory factory = new MediaPlayerFactory();
						player = factory.CreatePlayer<IVideoPlayer>();
						player.Events.TimeChanged += new EventHandler<Declarations.Events.MediaPlayerTimeChanged>(Events_TimeChanged);
						int b = cameraSpec.vlc_transcode_buffer_time;
						string[] args = new string[] { ":rtsp-caching=" + b, ":realrtsp-caching=" + b, ":network-caching=" + b, ":udp-caching=" + b, ":volume=0", cameraSpec.wanscamCompatibilityMode ? ":demux=h264" : "", cameraSpec.wanscamCompatibilityMode ? ":h264-fps=" + cameraSpec.wanscamFps : "" };
						string url = cameraSpec.imageryUrl;
						if (cameraSpec.wanscamCompatibilityMode)
							url = "http://127.0.0.1:" + MJpegWrapper.cfg.webport + "/" + cameraSpec.id + ".wanscamstream";
						IMedia media = factory.CreateMedia<IMedia>(url, args);
						memRender = player.CustomRenderer2;
						//memRender.SetExceptionHandler(ExHandler);
						memRender.SetCallback(delegate(Bitmap frame)
						{
							// We won't consume the bitmap here.  For efficiency's sake under light load, we will only encode the bitmap as jpeg when it is requested by a client.
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
							//    lastFrame = ImageConverter.GetJpegBytes(frame);
							//    nextFrameEncodeTime = time + frameEncodeInterval;
							//}
							//latestBitmap = new Bitmap(frame);  // frame.Clone() actually doesn't copy the data and exceptions get thrown
						});
						memRender.SetFormat(new BitmapFormat(w, h, ChromaType.RV24));
						//memRender.SetFormatSetupCallback(formatSetupCallback);
						player.Open(media);
						player.Play();
						if (w == 0)
						{
							// Need to auto-detect video size.
							while (!Exit)
							{
								Thread.Sleep(50);
								Size s = player.GetVideoSize(0);
								if (s.Width > 0 && s.Height > 0)
								{
									lock (MJpegWrapper.cfg)
									{
										w = this.cameraSpec.h264_video_width = (ushort)s.Width;
										h = this.cameraSpec.h264_video_height = (ushort)s.Height;
										MJpegWrapper.cfg.Save(CameraProxyGlobals.ConfigFilePath);
									}
									throw new Exception("Restart");
								}
							}
						}
						else
							while (!Exit)
								Thread.Sleep(50);
					}
					catch (ThreadAbortException ex)
					{
						throw ex;
					}
					catch (Exception ex)
					{
						if (ex.Message != "Restart")
						{
							Logger.Debug(ex);
							int waitedTimes = 0;
							while (!Exit && waitedTimes++ < 100)
								Thread.Sleep(50);
						}
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

		void Events_TimeChanged(object sender, Declarations.Events.MediaPlayerTimeChanged e)
		{
			lastTimestampUpdateTime = frameTimer.ElapsedMilliseconds;
		}
		private void ExHandler(Exception ex)
		{
			Logger.Debug(ex);
		}
	}
}

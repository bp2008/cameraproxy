using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Declarations;
using Declarations.Players;
using Implementation;
using Declarations.Media;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;

namespace MJpegCameraProxy
{
	public class Html5VideoCamera : IPCameraBase
	{
		private int w, h;
		List<ConcurrentQueue<byte[]>> RegisteredOutputStreams = new List<ConcurrentQueue<byte[]>>();

		public void RegisterStreamListener(ConcurrentQueue<byte[]> dataQueue)
		{
			lock (RegisteredOutputStreams)
			{
				List<ConcurrentQueue<byte[]>> newOutputStreams = new List<ConcurrentQueue<byte[]>>();
				foreach (var item in RegisteredOutputStreams)
					newOutputStreams.Add(item);
				newOutputStreams.Add(dataQueue);
				RegisteredOutputStreams = newOutputStreams;
			}
		}
		public void UnregisterStreamListener(ConcurrentQueue<byte[]> dataQueue)
		{
			lock (RegisteredOutputStreams)
			{
				List<ConcurrentQueue<byte[]>> newOutputStreams = new List<ConcurrentQueue<byte[]>>();
				foreach (var item in RegisteredOutputStreams)
					if (item != dataQueue)
						newOutputStreams.Add(item);
				RegisteredOutputStreams = newOutputStreams;
			}
		}
		protected override void DoBackgroundWork()
		{
			try
			{ // Unfortunately, the standard output stream method is fundamentally incompatible with the ogg container, which cannot simply be opened except at the beginning.  This would work if we were having VLC transcode to MJPG, because we can pull images from the stream.
				int bitrate = 1600;
				w = this.cameraSpec.h264_video_width;
				h = this.cameraSpec.h264_video_height;
				while (!Exit)
				{
					try
					{
						string path = "C:/Program Files (x86)/VideoLAN/VLC/vlc.exe";

						string url = cameraSpec.imageryUrl;
						int bufferTime = cameraSpec.vlc_transcode_buffer_time;
						string size = w <= 0 || h <= 0 ? "" : ",width=" + w + ",height=" + h;
						string args = "\"" + url + "\" :network-caching=" + bufferTime + " :sout=#transcode{vcodec=theo,vb=" + bitrate + ",scale=1" + size + ",acodec=none}:file{mux=ogg,dst=-} :no-sout-rtp-sap :no-sout-standard-sap :sout-keep";

						Process p = null;
						try
						{
							ProcessStartInfo psi = new ProcessStartInfo(path, args);
							psi.CreateNoWindow = false;
							psi.UseShellExecute = false;
							psi.RedirectStandardOutput = true;
							psi.RedirectStandardError = true;
							p = Process.Start(psi);
							BufferedStream bs = new BufferedStream(p.StandardOutput.BaseStream);
							byte[] buffer = new byte[100000];
							while (!Exit && !p.HasExited)
							{
								int read = bs.Read(buffer, 0, buffer.Length);
								if (read > 0)
								{
									byte[] tmp = new byte[read];
									Array.Copy(buffer, tmp, read);

									List<ConcurrentQueue<byte[]>> currentOutputStreams = RegisteredOutputStreams;
									foreach (ConcurrentQueue<byte[]> dataQueue in currentOutputStreams)
										dataQueue.Enqueue(tmp);
								}
								//Thread.Sleep(1);
							}
						}
						finally
						{
							if (p != null && !p.HasExited)
							{
								p.CloseMainWindow();
								Thread.Sleep(500);
								if (!p.HasExited)
									p.Kill();
							}
						}
					}
					catch (ThreadAbortException ex)
					{
						throw ex;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
						if (!Exit)
							Thread.Sleep(5000);
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
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Declarations;
using Declarations.Players;
using Implementation;
using Declarations.Media;
using System.Drawing;

namespace MJpegCameraProxy
{
	public class Html5VideoCamera : IPCameraBase
	{
		private int w, h;
		protected override void DoBackgroundWork()
		{
			try
			{
				int bitrate = 1600;
				w = this.cameraSpec.h264_video_width;
				h = this.cameraSpec.h264_video_height;
				//access_password = Session.GenerateSid();
				//access_username = Session.GenerateSid();
				IVideoPlayer player = null;
				while (!Exit)
				{
					try
					{
						IMediaPlayerFactory factory = new MediaPlayerFactory();
						player = factory.CreatePlayer<IVideoPlayer>();

						string size = w == 0 || h == 0 ? "" : ",width=" + w + ",height=" + h;
						string sout = ":sout=#transcode{vcodec=theo,vb=" + bitrate + ",scale=1" + size + ",acodec=none}";
						int b = cameraSpec.vlc_transcode_buffer_time;

						string[] args = new string[] { ":rtsp-caching=" + b
							, ":realrtsp-caching=" + b
							, ":network-caching=" + b
							, ":udp-caching=" + b
							, ":volume=0"
							, sout + ":http{mux=ogg,dst=:8181/desktop}",":no-sout-rtp-sap",":no-sout-standard-sap",":sout-keep"};
						string url = cameraSpec.imageryUrl;

						IMedia media = factory.CreateMedia<IMedia>(url, args);
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
										MJpegWrapper.cfg.Save(Globals.ConfigFilePath);
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
*/
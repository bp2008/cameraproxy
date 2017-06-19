using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using BPUtil;

namespace MJpegCameraProxy
{
	public class H264Camera : IPCameraBase
	{
		public string access_password = "";
		public string access_username = "";

		internal H264Camera()
		{
		}
		protected override void DoBackgroundWork()
		{
			try
			{
				access_password = Session.GenerateSid();
				access_username = Session.GenerateSid();
				while (!Exit)
				{
					try
					{
						string path = Globals.ApplicationDirectoryBase + "live555\\live555ProxyServer.exe";
						string args = "-u " + access_username + " " + access_password + " -p " + this.cameraSpec.h264_port + " \"" + this.cameraSpec.imageryUrl + "\"";
						//Logger.Info("Path: " + path);
						//Logger.Info("Args: " + args);
						Process p = null;
						try
						{
							ProcessStartInfo psi = new ProcessStartInfo(path, args);
							psi.CreateNoWindow = false;
							psi.UseShellExecute = false;
							psi.RedirectStandardError = true;
							p = Process.Start(psi);
							while (!Exit && !p.HasExited)
								Thread.Sleep(500);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MJpegCameraProxy.Configuration;
using System.Diagnostics;
using System.IO;

namespace MJpegCameraProxy
{
	public class MJpegWrapper
	{
		public static volatile bool ApplicationExiting = false;

		MJpegServer httpServer;
		public static DateTime startTime = DateTime.MinValue;
		public static ProxyConfig cfg;

		public MJpegWrapper()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 640;

			cfg = new ProxyConfig();
			if (File.Exists(Globals.ConfigFilePath))
				cfg.Load(Globals.ConfigFilePath);
			else
			{
				if (cfg.users.Count == 0)
					cfg.users.Add(new User("admin", "admin", 100));
				cfg.Save(Globals.ConfigFilePath);
			}
			SimpleHttp.SimpleHttpLogger.RegisterLogger(Logger.httpLogger);
			bool killed = false;
			try
			{
				foreach (var process in Process.GetProcessesByName("live555ProxyServer"))
				{
					try
					{
						process.Kill();
						killed = true;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex, "Trying to kill existing live555ProxyServer process");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Trying to iterate through existing live555ProxyServer processes");
			}
			if (killed)
				Thread.Sleep(500);
		}
		#region Start / Stop
		public void Start()
		{
			Stop();

			startTime = DateTime.Now;

			httpServer = new MJpegServer(cfg.webport, cfg.webport_https);
			httpServer.Start();
		}
		public void Stop()
		{
			if (httpServer != null)
			{
				httpServer.Stop();
				httpServer.Join(1000);
			}
		}
		#endregion
	}
}

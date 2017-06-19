using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MJpegCameraProxy.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using BPUtil;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy
{
	public class MJpegWrapper
	{
		public static volatile bool ApplicationExiting = false;

		MJpegServer httpServer;
		public static CameraProxyWebSocketServer webSocketServer;
		public static DateTime startTime = DateTime.MinValue;
		public static ProxyConfig cfg;

		public event EventHandler<string> SocketBound = delegate { };

		public MJpegWrapper()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			System.Net.ServicePointManager.Expect100Continue = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 640;

			cfg = new ProxyConfig();
			if (File.Exists(CameraProxyGlobals.ConfigFilePath))
				cfg.Load(CameraProxyGlobals.ConfigFilePath);
			else
			{
				if (cfg.users.Count == 0)
					cfg.users.Add(new User("admin", "admin", 100));
				cfg.Save(CameraProxyGlobals.ConfigFilePath);
			}
			SimpleHttpLogger.RegisterLogger(Logger.httpLogger);
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

			X509Certificate2 cert = null;
			if (!string.IsNullOrWhiteSpace(cfg.certificate_pfx_path))
			{
				if (!string.IsNullOrEmpty(cfg.certificate_pfx_password))
					cert = new X509Certificate2(cfg.certificate_pfx_path, cfg.certificate_pfx_password);
				else
					cert = new X509Certificate2(cfg.certificate_pfx_path);
			}
			httpServer = new MJpegServer(cfg.webport, cfg.webport_https, cert);
			httpServer.SocketBound += Server_SocketBound;
			httpServer.Start();

			webSocketServer = new CameraProxyWebSocketServer(cfg.webSocketPort, cfg.webSocketPort_secure, cert);
			webSocketServer.SocketBound += Server_SocketBound;
			webSocketServer.Start();
		}

		private void Server_SocketBound(object sender, string e)
		{
			SocketBound(sender, e);
		}

		public void Stop()
		{
			if (httpServer != null)
			{
				httpServer.Stop();
				httpServer.Join(1000);
			}
			// webSocketServer does not support closing/stopping
			webSocketServer = null;
		}
		#endregion

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject == null)
			{
				Logger.Debug("UNHANDLED EXCEPTION - null exception");
			}
			else
			{
				try
				{
					Logger.Debug((Exception)e.ExceptionObject, "UNHANDLED EXCEPTION");
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "UNHANDLED EXCEPTION - Unable to report exception of type " + e.ExceptionObject.GetType().ToString());
				}
			}
		}
	}
}

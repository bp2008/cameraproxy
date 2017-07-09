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
using System.Threading.Tasks;

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

		Thread thrCertificateMaintainer;
		public static string letsEncrypt_certificateGenerationStatus = "";
		public static volatile bool letsEncrypt_forceRenewCertificate = false;

		public MJpegWrapper()
		{
			if (Environment.UserInteractive)
				Logger.logType = LoggingMode.Console | LoggingMode.File;
			string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			Globals.Initialize(exePath);
			PrivateAccessor.SetStaticFieldValue(typeof(Globals), "errorFilePath", Globals.WritableDirectoryBase + "MJpegCameraErrors.txt");

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
			if (!string.IsNullOrWhiteSpace(cfg.certificate_pfx_path) && File.Exists(cfg.certificate_pfx_path))
			{
				if (!string.IsNullOrEmpty(cfg.certificate_pfx_password))
					cert = new X509Certificate2(cfg.certificate_pfx_path, cfg.certificate_pfx_password);
				else
					cert = new X509Certificate2(cfg.certificate_pfx_path);
			}
			httpServer = new MJpegServer(cfg.webport, cfg.webport_https, cert);
			httpServer.SocketBound += Server_SocketBound;
			httpServer.CertificateExpirationWarning += HttpServer_CertificateExpirationWarning;
			httpServer.Start();

			webSocketServer = new CameraProxyWebSocketServer(cfg.webSocketPort, cfg.webSocketPort_secure, cert);
			webSocketServer.SocketBound += Server_SocketBound;
			webSocketServer.Start();

			thrCertificateMaintainer = new Thread(maintainLetsEncryptCertificate);
			thrCertificateMaintainer.Name = "Maintain LetsEncrypt Certificate";
			thrCertificateMaintainer.IsBackground = true;
			thrCertificateMaintainer.Start();
		}

		private void HttpServer_CertificateExpirationWarning(object sender, TimeSpan e)
		{
			if (cfg.certificateMode == CertificateMode.LetsEncrypt)
			{
				if (e > TimeSpan.Zero)
					Logger.Info("WARNING: LetsEncrypt SSL certificate is going to expire in " + e);
				else
					Logger.Info("WARNING: LetsEncrypt SSL certificate has expired");
			}
		}

		private void maintainLetsEncryptCertificate()
		{
			try
			{
				Stopwatch sw = new Stopwatch();
				sw.Start();
				long nextCertificateCheck = 0;
				while (true)
				{
					try
					{
						// Sleep while configuration is not set to use LetsEncrypt.
						while (cfg.certificateMode != CertificateMode.LetsEncrypt
							|| string.IsNullOrEmpty(cfg.certificate_pfx_path)
							|| string.IsNullOrEmpty(cfg.certificate_pfx_password)
							|| string.IsNullOrEmpty(cfg.letsEncrypt_email)
							|| cfg.letsEncrypt_domains?.Length == 0)
							Thread.Sleep(2000);

						// Sleep while httpServer is null
						if (httpServer == null)
						{
							Thread.Sleep(2000);
							continue;
						}

						if (nextCertificateCheck > sw.ElapsedMilliseconds)
						{
							// Not near expiration
							Thread.Sleep(60000);
						}
						else if (httpServer.GetCertificateFriendlyName() != "LetsEncryptAuto"
							|| httpServer.GetCertificateExpiration() < DateTime.Now.AddDays(14))
						{
							try
							{
								// Current certificate is not from LetsEncrypt or expiration is near.
								// Either way, we should renew.
								nextCertificateCheck = sw.ElapsedMilliseconds + 360000; // 1 hour later

								string wwwRoot = cfg.letsEncrypt_www_root;
								Task<byte[]> certTask = LetsEncrypt.GetCertificate(cfg.letsEncrypt_email, cfg.letsEncrypt_domains, cfg.certificate_pfx_password, (c) =>
								{
									if (string.IsNullOrWhiteSpace(wwwRoot))
										httpServer.PrepareCertificationChallenge(c);
									else
									{
										string baseDir = wwwRoot.Replace('\\', '/') + "/.well-known/acme-challenge/";
										Directory.CreateDirectory(baseDir);
										File.WriteAllText(baseDir + c.challengeToken, c.expectedResponse, Encoding.ASCII);
									}
								}
								, (status) =>
								{
									letsEncrypt_certificateGenerationStatus = status;
								});
								certTask.Wait();
								byte[] certificate = certTask.Result;
								letsEncrypt_certificateGenerationStatus = "";
								if (string.IsNullOrWhiteSpace(wwwRoot))
									httpServer.ClearChallenges();
								if (certificate != null && certificate.Length > 0)
								{
									File.WriteAllBytes(Globals.WritableDirectoryBase + cfg.certificate_pfx_path, certificate);
									X509Certificate2 cert = new X509Certificate2(certificate, cfg.certificate_pfx_password);
									httpServer.SetCertificate(cert);
									Logger.Info("LetsEncrypt certificate renewed successfully");
								}
							}
							catch (Exception)
							{
								letsEncrypt_certificateGenerationStatus = "Error in last certificate renewal";
								throw;
							}
						}
						else
						{
							// Not near expiration
							Thread.Sleep(60000);
						}
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex)
					{
						Logger.Debug(ex);
						Thread.Sleep(60000);
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		private void Server_SocketBound(object sender, string e)
		{
			SocketBound(sender, e);
		}

		public void Stop()
		{
			Try.Catch(() => { thrCertificateMaintainer?.Abort(); });

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

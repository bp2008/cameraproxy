using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MJpegCameraProxy
{
	public class MJpegWrapper
	{
		Thread thrHttp;
		MJpegServer httpServer;
		public static DateTime startTime = DateTime.MinValue;
		int port;

		public MJpegWrapper(int port)
		{
			this.port = port;
		}
		#region Start / Stop
		public void Start()
		{
			Stop();

			startTime = DateTime.Now;

			httpServer = new MJpegServer(port);
			thrHttp = new Thread(httpServer.listen);
			thrHttp.Name = "MJpegCameraProxy HTTP Listener";
			thrHttp.Start();
		}
		public void Stop()
		{
			if (httpServer != null)
				httpServer.stop();

			if (thrHttp != null)
			{
				thrHttp.Abort();
				thrHttp.Join(1000);
			}
		}
		#endregion
	}
}

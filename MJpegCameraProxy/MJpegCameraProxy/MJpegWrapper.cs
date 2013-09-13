using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MJpegCameraProxy
{
	public class MJpegWrapper
	{
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

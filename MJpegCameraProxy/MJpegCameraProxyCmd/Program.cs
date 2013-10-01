using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy;

namespace MJpegCameraProxyCmd
{
	class Program
	{
		static MJpegWrapper mjpegServer;
		static void Main(string[] args)
		{
			mjpegServer = new MJpegWrapper();
			mjpegServer.Start();
			Console.WriteLine("MJpegServer listening on port " + MJpegWrapper.cfg.webport);
			Console.ReadLine();
			mjpegServer.Stop();
		}
	}
}

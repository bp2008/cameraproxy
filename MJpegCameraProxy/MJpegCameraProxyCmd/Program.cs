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
			int port = 44456;
			mjpegServer = new MJpegWrapper(port);
			mjpegServer.Start();
			Console.WriteLine("MJpegServer listening on port " + port);
			Console.ReadLine();
			mjpegServer.Stop();
		}
	}
}

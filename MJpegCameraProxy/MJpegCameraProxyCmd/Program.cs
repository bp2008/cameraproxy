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
			mjpegServer = new MJpegWrapper(8077);
			mjpegServer.Start();
			Console.ReadLine();
			mjpegServer.Stop();
		}
	}
}

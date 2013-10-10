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
			List<string> portStrings = new List<string>();
			if (MJpegWrapper.cfg.webport > -1 && MJpegWrapper.cfg.webport < 65535)
				portStrings.Add(MJpegWrapper.cfg.webport + " (http)");
			if (MJpegWrapper.cfg.webport_https > -1 && MJpegWrapper.cfg.webport_https < 65535)
				portStrings.Add(MJpegWrapper.cfg.webport_https + " (https)");
			if (portStrings.Count == 0)
				Console.WriteLine("MJpegServer is not configured to listen on any valid ports.");
			else
				Console.WriteLine("MJpegServer listening on port " + string.Join(" and ", portStrings));
			Console.ReadLine();
			mjpegServer.Stop();
		}
	}
}

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

			AddPort(portStrings, MJpegWrapper.cfg.webport, "http");
			AddPort(portStrings, MJpegWrapper.cfg.webport_https, "https");
			AddPort(portStrings, MJpegWrapper.cfg.webSocketPort, "Web Socket, ws://");
			AddPort(portStrings, MJpegWrapper.cfg.webSocketPort_secure, "Secure Web Socket, wss://");

			if (portStrings.Count == 0)
				Console.WriteLine("CameraProxy Server is not configured to listen on any valid ports.");
			else
				Console.WriteLine("CameraProxy Server listening on port " + string.Join(" and ", portStrings));

			Console.ReadLine();

			mjpegServer.Stop();
		}
		static void AddPort(List<string> portStrings, int port, string description)
		{
			if (port > -1 && port < 65535)
				portStrings.Add(port + " (" + description + ")");
		}
	}
}

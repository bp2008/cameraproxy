using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPUtil;
using MJpegCameraProxy;

namespace MJpegCameraProxyCmd
{
	class Program
	{
		static MJpegWrapper mjpegServer;
		static void Main(string[] args)
		{
			Logger.logType = LoggingMode.Console | LoggingMode.File;
			string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			Globals.Initialize(exePath);
			PrivateAccessor.SetStaticFieldValue(typeof(Globals), "errorFilePath", Globals.WritableDirectoryBase + "MJpegCameraErrors.txt");

			Console.WriteLine("CameraProxy service as command line app");
			MJpegWrapper mjpegServer = new MJpegWrapper();
			mjpegServer.SocketBound += MjpegServer_SocketBound;
			mjpegServer.Start();

			do
			{
				Console.WriteLine("Type \"exit\" to close.");
			}
			while (Console.ReadLine().ToLower() != "exit");

			mjpegServer.Stop();
		}

		private static void MjpegServer_SocketBound(object sender, string e)
		{
			Console.WriteLine(e);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy;

namespace MJpegCameraProxyCmd
{
	/// <summary>
	/// A static wrapper class intended to workaround a Costury.Fody issue on mono according to https://github.com/Fody/Costura/issues/70
	/// </summary>
	internal static class MainStatic
	{
		public static void MainMethod(string[] args)
		{
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

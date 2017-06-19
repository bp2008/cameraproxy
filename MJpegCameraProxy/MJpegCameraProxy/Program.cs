using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using BPUtil;
using BPUtil.Forms;

namespace MJpegCameraProxy
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			Globals.Initialize(exePath);
			PrivateAccessor.SetStaticFieldValue(typeof(Globals), "errorFilePath", Globals.WritableDirectoryBase + "MJpegCameraErrors.txt");

			if (Environment.UserInteractive)
			{
				string Title = "CameraProxy " + CameraProxyGlobals.Version + " Service Manager";
				string ServiceName = "MJpegCameraProxy";
				ButtonDefinition btnCmd = new ButtonDefinition("Command Line Test", btnCmd_Click);
				ButtonDefinition[] customButtons = new ButtonDefinition[] { btnCmd };

				Application.Run(new ServiceManager(Title, ServiceName, customButtons));
			}
			else
			{
				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[]
				{
					new Service1()
				};
				ServiceBase.Run(ServicesToRun);
			}
		}
		private static void btnCmd_Click(object sender, EventArgs e)
		{
			FileInfo fiExe = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string path = fiExe.Directory.FullName.TrimEnd('\\', '/') + "/MJpegCameraProxyCmd.exe";
			if (File.Exists(path))
				Process.Start(path, "cmd");
			else
				MessageBox.Show("MJpegCameraProxyCmd.exe could not be found.");
		}
	}
}

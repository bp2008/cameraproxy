using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace MJpegCameraProxy
{
	public partial class CameraProxyService : ServiceBase
	{
		static MJpegWrapper mjpegServer;
		public CameraProxyService()
		{
			InitializeComponent();
			mjpegServer = new MJpegWrapper();
		}

		protected override void OnStart(string[] args)
		{
			mjpegServer.Start();
		}

		protected override void OnStop()
		{
			mjpegServer.Stop();
		}
	}
}

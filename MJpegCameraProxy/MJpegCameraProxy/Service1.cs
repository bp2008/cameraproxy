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
	public partial class Service1 : ServiceBase
	{
		static MJpegWrapper mjpegServer;
		public Service1()
		{
			InitializeComponent();
			mjpegServer = new MJpegWrapper(8077);
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

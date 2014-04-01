using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MJpegCameraProxy.Configuration;
using System.Threading;

namespace MJpegCameraProxy.PanTiltZoom.IPS_EYE01
{
	class IPS_EYE01_PTZ : IPTZSimple, IPTZHtml
	{
		CameraSpec camSpec;
		public IPS_EYE01_PTZ(CameraSpec camSpec)
		{
			this.camSpec = camSpec;
		}

		#region IPTZSimple Members
		// 1 - up
		// 2 - down
		// 3 - left
		// 4 - right
		// 5 - stop
		public void MoveSimple(PTZDirection direction)
		{
			int commandStart = 5;
			int commandEnd = 5;
			if ((direction & PTZDirection.Up) > 0)
				commandStart = 1;
			else if ((direction & PTZDirection.Down) > 0)
				commandStart = 2;
			else if ((direction & PTZDirection.Left) > 0)
				commandStart = 3;
			else if ((direction & PTZDirection.Right) > 0)
				commandStart = 4;
			Util.HttpPost("http://" + camSpec.ptz_hostName + "/cgi-bin/cmd.cgi", "opType=set&cmd=ptz&ptz_cmd=" + commandStart + "&protocol=1&band=2400&speed=60&addr=1");
			Thread.Sleep(500);
			Util.HttpPost("http://" + camSpec.ptz_hostName + "/cgi-bin/cmd.cgi", "opType=set&cmd=ptz&ptz_cmd=" + commandEnd + "&protocol=1&band=2400&speed=60&addr=1");
		}

		public bool SupportsZoom()
		{
			return false;
		}

		public void Zoom(ZoomDirection direction, ZoomAmount amount)
		{
			return;
		}

		#endregion

		#region IPTZHtml Members

		public string GetHtml(string camId, IPCameraBase cam)
		{
			return PTZHtml.GetHtml(camId, cam, new HtmlOptions());
		}

		#endregion
	}
}

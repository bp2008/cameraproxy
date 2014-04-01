using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Text.RegularExpressions;

namespace MJpegCameraProxy.PanTiltZoom.Dev
{
	public class Dev : IPTZSimple, IPTZHtml, IZoomAbs
	{
		CameraSpec camSpec;
		Renci.SshNet.SshClient sshc;
		public Dev(CameraSpec camSpec)
		{
			this.camSpec = camSpec;
			sshc = new Renci.SshNet.SshClient(camSpec.ptz_hostName, camSpec.ptz_username, camSpec.ptz_password);
		}
		~Dev()
		{
		}

		#region IPTZSimple Members
		public void MoveSimple(PTZDirection direction)
		{
		}

		public bool SupportsZoom()
		{
			return true;
		}

		public void Zoom(ZoomDirection direction, ZoomAmount amount)
		{
			MaintainConnection();
			int[] range = GetZoomRange();
			int currentZoom = GetCurrentZoom();
			int newZoom = currentZoom;
			// Modify zoom level
			if (direction == ZoomDirection.In)
				newZoom += (range[1] - range[0]) / 6;
			else
				newZoom -= (range[1] - range[0]) / 6;

			// If close to the zoom limit, just fudge it to make it equal
			if (newZoom < range[0] + 50)
				newZoom = range[0];
			else if (newZoom > range[1] - 50)
				newZoom = range[1];

			// Execute zoom command
			if (newZoom != currentZoom)
				sshc.RunCommand("IspCtrl zoom direct " + newZoom);
			sshc.Disconnect();
		}

		#endregion

		#region IPTZHtml Members

		public string GetHtml(string camId, IPCameraBase cam)
		{
			HtmlOptions o = new HtmlOptions();
			o.showZoomButtons = true;
			o.showPtzArrows = false;
			o.showZoomLevels = true;
			return PTZHtml.GetHtml(camId, cam, o);
		}

		#endregion

		#region IZoomAbs Members

		public int GetCurrentZoom()
		{
			// Learn current zoom level
			Renci.SshNet.SshCommand cmd = sshc.RunCommand("IspCtrl zoom direct");
			Match m = Regex.Match(cmd.Result, "direct=(\\d+)$");
			if (m.Success)
				return int.Parse(m.Groups[1].Value);
			return -1;
		}

		public int[] GetZoomRange()
		{
			// Learn zoom limit
			int[] range = new int[2];
			Renci.SshNet.SshCommand cmd = sshc.RunCommand("IspCtrl zoom limit");
			Match m = Regex.Match(cmd.Result, "limit=(\\d+),(\\d+)");
			if (m.Success)
			{
				range[0] = int.Parse(m.Groups[1].Value);
				range[1] = int.Parse(m.Groups[2].Value);
			}
			return range;
		}

		public void ZoomAbs(int level)
		{
			MaintainConnection();
			int[] range = GetZoomRange();
			double zoomFraction = (double)level / 6.0;
			int zoomRange = range[1] - range[0];
			int newZoom = (int)(zoomRange * zoomFraction) + range[0];

			// If close to the zoom limit, just fudge it to make it equal
			if (newZoom < range[0] + 5)
				newZoom = range[0];
			else if (newZoom > range[1] - 5)
				newZoom = range[1];

			int currentZoom = GetCurrentZoom();
			// Execute zoom command
			if (newZoom != currentZoom)
				sshc.RunCommand("IspCtrl zoom direct " + newZoom);
			sshc.Disconnect();
		}

		#endregion

		public void MaintainConnection()
		{
			// Connect to camera
			if (!sshc.IsConnected)
				sshc.Connect();
		}
	}
}

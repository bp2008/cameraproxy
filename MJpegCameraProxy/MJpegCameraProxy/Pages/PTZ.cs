using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Web;

namespace MJpegCameraProxy
{
	public class PTZ
	{
		public static void RunCommand(string cameraId, string cmd)
		{
			IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
			if (cam == null)
				return;

			if (cam.cameraSpec.ptz_proxy)
			{
				string auth = (!string.IsNullOrEmpty(cam.cameraSpec.ptz_username) && !string.IsNullOrEmpty(cam.cameraSpec.ptz_password)) ? "rawauth=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_username) + ":" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_password) + "&" : "";
				SimpleProxy.GetData("http://" + cam.cameraSpec.ptz_hostName + "/PTZ?" + auth + "id=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_proxy_cameraId) + "&cmd=" + HttpUtility.UrlEncode(cmd));
			}
			else if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				LoftekCheapPTZ.RunCommand(cam, cameraId, cmd);
			else if (cam.cameraSpec.ptzType == PtzType.Dahua)
				DahuaPTZ.RunCommand(cam, cmd);
			else if (cam.cameraSpec.ptzType == PtzType.WanscamCheap)
				WanscamCheapPTZ.RunCommand(cam, cameraId, cmd);
			//else if (cam.cameraSpec.ptzType == PtzType.TrendnetIP672)
			//	IP672SeriesPTZ.RunCommand(cam, cameraId, cmd);
			else if (cam.cameraSpec.ptzType == PtzType.IPS_EYE01)
			{
				PanTiltZoom.PTZDirection dir = PanTiltZoom.PTZDirection.Up;
				if (cmd.StartsWith("d"))
					dir = PanTiltZoom.PTZDirection.Down;
				else if (cmd.StartsWith("l"))
					dir = PanTiltZoom.PTZDirection.Left;
				else if (cmd.StartsWith("r"))
					dir = PanTiltZoom.PTZDirection.Right;
				new PanTiltZoom.IPS_EYE01.IPS_EYE01_PTZ(cam.cameraSpec).MoveSimple(dir);
			}
			else if (cam.cameraSpec.ptzType == PtzType.TrendnetTVIP400)
			{
				PanTiltZoom.PTZDirection dir = PanTiltZoom.PTZDirection.Up;
				if (cmd.StartsWith("d"))
					dir = PanTiltZoom.PTZDirection.Down;
				else if (cmd.StartsWith("l"))
					dir = PanTiltZoom.PTZDirection.Left;
				else if (cmd.StartsWith("r"))
					dir = PanTiltZoom.PTZDirection.Right;
				new PanTiltZoom.TrendNet.TV_IP400(cam.cameraSpec).MoveSimple(dir);
			}
		}

		public static string GetHtml(string camId, IPCameraBase cam)
		{
			if (cam == null)
				return "";

			if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				return LoftekCheapPTZ.GetHtml(camId);
			else if (cam.cameraSpec.ptzType == PtzType.Dahua)
				return DahuaPTZ.GetHtml(camId, cam);
			else if (cam.cameraSpec.ptzType == PtzType.WanscamCheap)
				return WanscamCheapPTZ.GetHtml(camId);
			//else if (cam.cameraSpec.ptzType == PtzType.TrendnetIP672)
			//	return IP672SeriesPTZ.GetHtml(camId, cam);
			else
				return PanTiltZoom.PtzController.GetHtml(camId, cam);
		}
	}
}

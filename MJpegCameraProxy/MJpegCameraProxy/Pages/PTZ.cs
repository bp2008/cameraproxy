using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Web;
using MJpegCameraProxy.PanTiltZoom;

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
				SimpleProxy.GetData("http://" + cam.cameraSpec.ptz_hostName + "/control/PTZ?" + auth + "id=" + HttpUtility.UrlEncode(cam.cameraSpec.ptz_proxy_cameraId) + "&cmd=" + HttpUtility.UrlEncode(cmd));
			}
			else if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				LoftekCheapPTZ.RunCommand(cam, cameraId, cmd);
			//else if (cam.cameraSpec.ptzType == PtzType.Dahua)
			//    DahuaPTZ.RunCommand(cam, cmd);
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
			else if (cam.cameraSpec.ptzType == PtzType.CustomPTZProfile || cam.cameraSpec.ptzType == PtzType.Huisun)
			{
				object ptzProfile = null;
				if (cam.cameraSpec.ptzType == PtzType.CustomPTZProfile)
					ptzProfile = new PanTiltZoom.Custom.CustomPTZProfile(cam.cameraSpec, cam);
				else if (cam.cameraSpec.ptzType == PtzType.Huisun)
					ptzProfile = new PanTiltZoom.HuisunBullet(cam.cameraSpec, cam);

				int presetNum;
				if (cmd == "u")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.Up);
				else if (cmd == "d")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.Down);
				else if (cmd == "l")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.Left);
				else if (cmd == "r")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.Right);
				else if (cmd == "ul")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.UpLeft);
				else if (cmd == "ur")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.UpRight);
				else if (cmd == "dl")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.DownLeft);
				else if (cmd == "dr")
					((IPTZSimple)ptzProfile).MoveSimple(PanTiltZoom.PTZDirection.DownRight);
				else if (cmd == "i3")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.In, PanTiltZoom.ZoomAmount.Long);
				else if (cmd == "o3")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.Out, PanTiltZoom.ZoomAmount.Long);
				else if (cmd == "i2")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.In, PanTiltZoom.ZoomAmount.Medium);
				else if (cmd == "o2")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.Out, PanTiltZoom.ZoomAmount.Medium);
				else if (cmd == "i1")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.In, PanTiltZoom.ZoomAmount.Short);
				else if (cmd == "o1")
					((IPTZSimple)ptzProfile).Zoom(PanTiltZoom.ZoomDirection.Out, PanTiltZoom.ZoomAmount.Short);
				else if (cmd.Length == 3 && cmd.StartsWith("pl") && int.TryParse(cmd.Substring(2), out presetNum))
					((IPTZPresets)ptzProfile).LoadPreset(presetNum);
				else if (cmd.Length == 3 && cmd.StartsWith("ps") && int.TryParse(cmd.Substring(2), out presetNum))
					((IPTZPresets)ptzProfile).SavePreset(presetNum);
			}
		}

		public static string GetHtml(string camId, IPCameraBase cam)
		{
			if (cam == null)
				return "";

			if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				return LoftekCheapPTZ.GetHtml(camId);
			//else if (cam.cameraSpec.ptzType == PtzType.Dahua)
			//	return DahuaPTZ.GetHtml(camId, cam);
			else if (cam.cameraSpec.ptzType == PtzType.WanscamCheap)
				return WanscamCheapPTZ.GetHtml(camId);
			//else if (cam.cameraSpec.ptzType == PtzType.TrendnetIP672)
			//	return IP672SeriesPTZ.GetHtml(camId, cam);
			else
				return PanTiltZoom.PtzController.GetHtml(camId, cam);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class PTZ
	{
		public static void RunCommand(string cameraId, string cmd)
		{
			IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
			if (cam == null)
				return;

			if (cam.ptzType == PTZType.LoftekCheap)
				LoftekCheapPTZ.RunCommand(cam, cameraId, cmd);
			else if (cam.ptzType == PTZType.Dahua)
				DahuaPTZ.RunCommand(cam, cmd);
		}

		public static string GetHtml(string camId, IPCameraBase cam)
		{
			if (cam.ptzType == PTZType.LoftekCheap)
				return LoftekCheapPTZ.GetHtml(camId);
			else if (cam.ptzType == PTZType.Dahua)
				return DahuaPTZ.GetHtml(camId, cam);
			return "";
		}
	}
}

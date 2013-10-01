using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy
{
	public class PTZ
	{
		public static void RunCommand(string cameraId, string cmd)
		{
			IPCameraBase cam = MJpegServer.cm.GetCamera(cameraId);
			if (cam == null)
				return;

			if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				LoftekCheapPTZ.RunCommand(cam, cameraId, cmd);
			else if (cam.cameraSpec.ptzType == PtzType.Dahua)
				DahuaPTZ.RunCommand(cam, cmd);
		}

		public static string GetHtml(string camId, IPCameraBase cam)
		{
			if (cam == null)
				return "";

			if (cam.cameraSpec.ptzType == PtzType.LoftekCheap)
				return LoftekCheapPTZ.GetHtml(camId);
			else if (cam.cameraSpec.ptzType == PtzType.Dahua)
				return DahuaPTZ.GetHtml(camId, cam);
			return "";
		}
	}
}

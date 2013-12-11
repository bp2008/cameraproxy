using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy.PanTiltZoom.TrendNet
{
	class TV_IP400 : IPTZSimple, IPTZHtml
	{
		CameraSpec camSpec;
		public TV_IP400(CameraSpec camSpec)
		{
			this.camSpec = camSpec;
		}

		#region IPTZSimple Members
		/*
	-- PANTILTCONTROL.CGI
	-- Movement Table
	-- 0 = UP/LEFT
	-- 1 = UP
	-- 2 = UP/RIGHT
	-- 3 = LEFT
	-- 4 = HOME (RESET)
	-- 5 = RIGHT
	-- 6 = DOWN/LEFT
	-- 7 = DOWN
	-- 8 = DOWN/RIGHT
	-- MUST USE HTTP POST REQ, NOT GET
	-- Ex. os.execute("curl -d 'PanSingleMoveDegree=1&TiltSingleMoveDegree=1&PanTiltSingleMove=1' http://myusername:mypassword@ipaddress/PANTILTCONTROL.CGI")
	-- PanSingleMoveDegree=x   This is for the pan function, where x is the step size
	-- TiltSingleMoveDegree=x    This is for the tilt function, where x is the step size
	-- PanTiltSingleMove=x         This is for the movement variable, where x is the direction from above table
	-- If moving in any single direction pan or tilt, the other command is not required. 
		 */
		public void MoveSimple(PTZDirection direction)
		{
			int PanSingleMoveDegree = 5;
			int TiltSingleMoveDegree = 5;
			int PanTiltSingleMove = 4;
			if ((direction & PTZDirection.Up) > 0 && (direction & PTZDirection.Left) > 0)
				PanTiltSingleMove = 0;
			else if ((direction & PTZDirection.Up) > 0 && (direction & PTZDirection.Right) > 0)
				PanTiltSingleMove = 2;
			else if ((direction & PTZDirection.Down) > 0 && (direction & PTZDirection.Left) > 0)
				PanTiltSingleMove = 6;
			else if ((direction & PTZDirection.Down) > 0 && (direction & PTZDirection.Right) > 0)
				PanTiltSingleMove = 8;
			else if ((direction & PTZDirection.Up) > 0)
				PanTiltSingleMove = 1;
			else if ((direction & PTZDirection.Left) > 0)
				PanTiltSingleMove = 3;
			else if ((direction & PTZDirection.Right) > 0)
				PanTiltSingleMove = 5;
			else if ((direction & PTZDirection.Down) > 0)
				PanTiltSingleMove = 7;
			string s = Util.HttpPost("http://" + camSpec.ptz_hostName + "/PANTILTCONTROL.CGI", "PanSingleMoveDegree=" + PanSingleMoveDegree + "&TiltSingleMoveDegree=" + TiltSingleMoveDegree + "&PanTiltSingleMove=" + PanTiltSingleMove, credentials: new System.Net.NetworkCredential(camSpec.ptz_username, camSpec.ptz_password));
			Console.WriteLine(s);
		}

		public bool SupportsZoom()
		{
			return false;
		}

		public void Zoom(ZoomDirection direction)
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

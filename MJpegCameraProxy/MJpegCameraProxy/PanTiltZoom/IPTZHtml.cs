using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	interface IPTZHtml
	{
		string GetHtml(string camId, IPCameraBase cam);
	}
}

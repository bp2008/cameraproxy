using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	interface IZoomAbs
	{
		int GetCurrentZoom();
		int[] GetZoomRange();
		void ZoomAbs(int level);
	}
}

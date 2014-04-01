using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	interface IPTZSimple
	{
		void MoveSimple(PTZDirection direction);
		bool SupportsZoom();
		void Zoom(ZoomDirection direction, ZoomAmount amount);
	}
}

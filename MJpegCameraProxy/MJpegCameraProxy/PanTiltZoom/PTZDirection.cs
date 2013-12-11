using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	public enum PTZDirection
	{
		Up = 1,
		Down = 1 << 1,
		Left = 1 << 2,
		Right = 1 << 3,
		UpLeft = Up + Left,
		UpRight = Up + Right,
		DownLeft = Down + Left,
		DownRight = Down + Right
	}
}

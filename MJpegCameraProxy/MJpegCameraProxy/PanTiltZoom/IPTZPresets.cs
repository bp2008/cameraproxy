using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	interface IPTZPresets
	{
		void LoadPreset(int presetNum);
		void SavePreset(int presetNum);
	}
}

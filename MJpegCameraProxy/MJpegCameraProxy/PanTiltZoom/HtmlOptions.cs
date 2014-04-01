using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.PanTiltZoom
{
	internal class HtmlOptions
	{
		public bool showPtzArrows = true;
		public bool showZoomButtons = false;
		public bool showZoomLevels = false;
		public bool showPtzDiagonals = false;
		public bool showZoomInShort = false;
		public bool showZoomInMedium = false;
		public bool showZoomInLong = true;
		public bool showZoomOutShort = false;
		public bool showZoomOutMedium = false;
		public bool showZoomOutLong = true;
		public int panDelay = 250;
		public int tiltDelay = 250;
		public int zoomShortDelay = 100;
		public int zoomMediumDelay = 500;
		public int zoomLongDelay = 1000;
		public bool showPresets = false;
		public bool[] settablePresets = new bool[] { true, true, true, true, true, true, true, true };
		public bool[] gettablePresets = new bool[] { true, true, true, true, true, true, true, true };
	}
}

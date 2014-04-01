using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	[EditorName("Custom PTZ Profile Version 1")]
	public class PTZSpecV1 : PTZSpec
	{
		[EditorCategory("Pan/Tilt Commands")]
		[EditorName("Up")]
		public string up = "ptz.cgi?command=up";
		[EditorName("Down")]
		public string down = "ptz.cgi?command=down";
		[EditorName("Left")]
		public string left = "ptz.cgi?command=left";
		[EditorName("Right")]
		public string right = "ptz.cgi?command=right";

		[EditorName("Camera Supports Diagonal Movement")]
		public bool EnableDiagonals = false;

		[EditorCategory("Diagonal Movement Commands")]
		[EditorCondition_FieldMustBe("EnableDiagonals", "true")]
		[EditorName("Up and Left")]
		public string upleft = "ptz.cgi?command=upleft";
		[EditorName("Down and Left")]
		public string downleft = "ptz.cgi?command=downleft";
		[EditorName("Up and Right")]
		public string upright = "ptz.cgi?command=upright";
		[EditorName("Down and Right")]
		public string downright = "ptz.cgi?command=downright";

		[EditorCategory("Pan/Tilt Stop Command")]
		[EditorName("Pan/Tilt requires Stop command")]
		[EditorHint(" Some cameras will continue moving until a stop command is received.")]
		public bool SendStopCommandAfterPanTilt = false;

		[EditorCategory("")]
		[EditorCondition_FieldMustBe("SendStopCommandAfterPanTilt", "true")]
		[EditorName("Pan/Tilt Stop command")]
		public string stopPanTilt = "ptz.cgi?command=ptstop";
		[EditorName("Pan (Left/Right) duration")]
		[EditorHint("ms.  Time the camera should move before it is stopped.")]
		public int PanRunTimeMs = 500;
		[EditorName("Tilt (Up/Down) duration")]
		[EditorHint("ms.  Time the camera should move before it is stopped.")]
		public int TiltRunTimeMs = 250;

		[EditorCategory("Zoom Commands")]
		[EditorName("Camera Supports Zoom")]
		public bool EnableZoom = false;

		[EditorCategory("")]
		[EditorCondition_FieldMustBe("EnableZoom", "true")]
		[EditorName("Zoom in a little")]
		[EditorHint(" (optional)")]
		public string zoomInShort = "ptz.cgi?command=zoomin";
		[EditorName("Zoom in a medium amount")]
		[EditorHint(" (optional)")]
		public string zoomInMedium = "ptz.cgi?command=zoomin";
		[EditorName("Zoom in a lot")]
		public string zoomInLong = "ptz.cgi?command=zoomin";
		[EditorName("Zoom out a little")]
		[EditorHint(" (optional)")]
		public string zoomOutShort = "ptz.cgi?command=zoomout";
		[EditorName("Zoom out a medium amount")]
		[EditorHint(" (optional)")]
		public string zoomOutMedium = "ptz.cgi?command=zoomout";
		[EditorName("Zoom out a lot")]
		public string zoomOutLong = "ptz.cgi?command=zoomout";

		[EditorName("Zoom requires Stop command")]
		[EditorHint(" Some cameras will continue zooming until a stop command is received.")]
		public bool SendStopCommandAfterZoom = false;

		[EditorCategory("")]
		[EditorCondition_FieldMustBe("SendStopCommandAfterZoom", "true")]
		[EditorName("Zoom Stop command")]
		public string stopZoom = "ptz.cgi?command=zoomstop";
		[EditorName("Zoom a little duration")]
		[EditorHint("ms.  Time the lens should zoom when a short zoom button is clicked.")]
		public int ZoomRunTimeShortMs = 100;
		[EditorName("Zoom a medium amount duration")]
		[EditorHint("ms.  Time the lens should zoom when a medium zoom button is clicked.")]
		public int ZoomRunTimeMediumMs = 500;
		[EditorName("Zoom a lot duration")]
		[EditorHint("ms.  Time the lens should zoom when a long zoom button is clicked.")]
		public int ZoomRunTimeLongMs = 1000;

		[EditorCategory("Preset Commands")]
		[EditorName("Camera Supports Presets")]
		public bool EnablePresets = false;

		[EditorCategory("")]
		[EditorCondition_FieldMustBe("EnablePresets", "true")]
		[EditorName("Load Preset 1")]
		public string load_preset_1 = "ptz.cgi?command=load1";
		[EditorName("Load Preset 2")]
		public string load_preset_2 = "ptz.cgi?command=load2";
		[EditorName("Load Preset 3")]
		public string load_preset_3 = "ptz.cgi?command=load3";
		[EditorName("Load Preset 4")]
		public string load_preset_4 = "ptz.cgi?command=load4";
		[EditorName("Load Preset 5")]
		public string load_preset_5 = "ptz.cgi?command=load5";
		[EditorName("Load Preset 6")]
		public string load_preset_6 = "ptz.cgi?command=load6";
		[EditorName("Load Preset 7")]
		public string load_preset_7 = "ptz.cgi?command=load7";
		[EditorName("Load Preset 8")]
		public string load_preset_8 = "ptz.cgi?command=load8";

		[EditorName("Save Preset 1")]
		public string save_preset_1 = "ptz.cgi?command=save1";
		[EditorName("Save Preset 2")]
		public string save_preset_2 = "ptz.cgi?command=save2";
		[EditorName("Save Preset 3")]
		public string save_preset_3 = "ptz.cgi?command=save3";
		[EditorName("Save Preset 4")]
		public string save_preset_4 = "ptz.cgi?command=save4";
		[EditorName("Save Preset 5")]
		public string save_preset_5 = "ptz.cgi?command=save5";
		[EditorName("Save Preset 6")]
		public string save_preset_6 = "ptz.cgi?command=save6";
		[EditorName("Save Preset 7")]
		public string save_preset_7 = "ptz.cgi?command=save7";
		[EditorName("Save Preset 8")]
		public string save_preset_8 = "ptz.cgi?command=save8";

		protected override string validateFieldValues()
		{
			if (string.IsNullOrWhiteSpace(name))
				return "0PTZ Profile name must not contain only whitespace.";
			if (!Util.IsAlphaNumeric(name, true))
				return "0PTZ Profile name must be alphanumeric, but may contain spaces.";
			if (name.Length > 64)
				return "0PTZ Profile name must be 64 characters or less.";
			if (ZoomRunTimeShortMs < 0)
				return "0Zoom a little duration must not be negative.";
			if (ZoomRunTimeMediumMs < 0)
				return "0Zoom a medium amount duration must not be negative.";
			if (ZoomRunTimeLongMs < 0)
				return "0Zoom a lot duration must not be negative.";
			if (PanRunTimeMs < 0)
				return "0Pan duration must not be negative.";
			if (TiltRunTimeMs < 0)
				return "Tilt duration must not be negative.";
			if (ZoomRunTimeShortMs > 10000)
				return "0Zoom a little duration must be less than 10001 milliseconds.";
			if (ZoomRunTimeMediumMs > 10000)
				return "0Zoom a medium amount duration must be less than 10001 milliseconds.";
			if (ZoomRunTimeLongMs > 10000)
				return "0Zoom a lot duration must be less than 10001 milliseconds.";
			if (PanRunTimeMs > 10000)
				return "0Pan duration must be less than 10001 milliseconds.";
			if (TiltRunTimeMs > 10000)
				return "Tilt duration must be less than 10001 milliseconds.";
			return "1";
		}
		public override string ToString()
		{
			return name;
		}
		public PTZSpecV1()
		{
			version = 1;
		}
	}
}

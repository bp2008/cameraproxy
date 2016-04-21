using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	[EditorName("Camera Configuration")]
	public class CameraSpec : FieldSettable
	{
		[EditorName("Enabled")]
		public bool enabled = false;
		[EditorName("Name")]
		public string name = "New Camera";
		[EditorName("ID")]
		public string id = "";
		[EditorName("Camera Type")]
		[EditorHint("<br/>Note: Type vlc_transcode_to_html5 is mostly non-functional.")]
		public CameraType type = CameraType.jpg;
		[EditorName("Imagery URL")]
		public string imageryUrl = "";
		[EditorHint("seconds. The server will stop requesting imagery from the source this long after the last image request is made by a client.")]
		[EditorName("Idle Timeout")]
		public int maxBufferTime = 10;
		[EditorHint("If checked, the last buffered frame will be preserved when the camera's idle timeout is triggered.")]
		[EditorName("Keep Last Image")]
		public bool keepLastImageAfterCameraTimeout = true;
		[EditorHint("0 to 100.  Anonymous users have permission 0.")]
		[EditorName("Minimum User Permission")]
		public int minPermissionLevel = 0;
		[EditorName("PTZ Type")]
		public PtzType ptzType = PtzType.None;

		[EditorCategory("Camera's Http Authentication (if required)")]
		[EditorCondition_FieldMustBe("type", CameraType.jpg, CameraType.mjpg)]
		[EditorName("Camera User Name")]
		public string username = "";
		[IsPasswordField(true)]
		[EditorName("Camera Password")]
		public string password = "";

		[EditorCategory("Pan-Tilt-Zoom")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.LoftekCheap, PtzType.Dahua, PtzType.WanscamCheap, PtzType.IPS_EYE01, PtzType.TrendnetTVIP400, PtzType.CustomPTZProfile, PtzType.Hikvision, PtzType.Huisun)]
		[EditorName("PTZ Host Name")]
		public string ptz_hostName = "";
		[EditorName("PTZ User Name")]
		public string ptz_username = "";
		[IsPasswordField(true)]
		[EditorName("PTZ Password")]
		public string ptz_password = "";
		[EditorName("Proxy Control")]
		[EditorHint("If checked, the PTZ host is another instance of the CameraProxy service.")]
		public bool ptz_proxy = false;

		[EditorCategory("PTZ Proxy Settings")]
		[EditorCondition_FieldMustBe("ptz_proxy", "true")]
		[EditorName("Remote Camera ID")]
		[EditorHint("<br/>ID of the remote camera.  This may be different from the local camera ID.")]
		public string ptz_proxy_cameraId = "";

		[EditorCategory("Custom PTZ Profile")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.CustomPTZProfile)]
		[EditorName("Profile Name")]
		[EditorHint(" Check the PTZProfiles page for a list of valid profile names.")]
		public string ptz_customPTZProfile = "";
		
		[EditorCategory("Dahua/Hikvision PTZ Settings")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.Dahua, PtzType.Hikvision)]
		[EditorName("PTZ Absolute X Position Offset")]
		[EditorHint("degrees")]
		public int ptz_absoluteXOffset = 0;
		[EditorName("PTZ Panorama Selection Rectangle Width")]
		[EditorHint("percentage [0.0-1.0] of panorama's width")]
		public double ptz_panorama_selection_rectangle_width_percent = 0.17;
		[EditorName("PTZ Panorama Selection Rectangle Height")]
		[EditorHint("percentage [0.0-1.0] of panorama's height")]
		public double ptz_panorama_selection_rectangle_height_percent = 0.3;
		[EditorName("Simple Panorama")]
		[EditorHint("If checked, thumbnails 0 through 27 will be displayed in a grid.  If unchecked, thumbnail number 99999 will be loaded as a full size panorama.")]
		public bool ptz_panorama_simple = true;
		[EditorName("Panorama represented vertical angle")]
		[EditorHint("degrees.  Adjust this as needed to calibrate vertical positioning in the panorama rectangle.  Default: 90 degrees")]
		public int ptz_panorama_degrees_vertical = 90;
		[EditorName("Zoom Magnification")]
		[EditorHint("x Zoom (common values are 3 or 12 or 20 or 30)")]
		public int ptz_magnification = 12;
		[EditorName("Idle Reset")]
		[EditorHint("If checked, the camera will return to the specified coordinates after a certain amount of time has passed")]
		public bool ptz_enableidleresetposition = false;
		[EditorName("Idle Position Timeout")]
		[EditorHint("seconds.  If Idle Reset is enabled, camera will move to Idle Position this many seconds after the last PTZ command was received from a user.")]
		public int ptz_idleresettimeout = 600;
		[EditorName("Idle Position X")]
		[EditorHint("[0.0-1.0] Absolute pan position.")]
		public double ptz_idleresetpositionX = 0;
		[EditorName("Idle Position Y")]
		[EditorHint("[0.0-1.0] Absolute tilt position.")]
		public double ptz_idleresetpositionY = 0;
		[EditorName("Idle Position Z")]
		[EditorHint("[0.0-1.0] Absolute zoom position.")]
		public double ptz_idleresetpositionZ = 0;
		[EditorName("Horizontal FOV")]
		[EditorHint("degrees field of view horizontally when the camera is fully zoomed out.")]
		public double ptz_fov_horizontal = 0;
		[EditorName("Vertical FOV")]
		[EditorHint("degrees field of view vertically when the camera is fully zoomed out.")]
		public double ptz_fov_vertical = 0;

		[EditorCategory("Hikvision PTZ Settings")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.Hikvision)]
		[EditorName("High tilt limit")]
		[EditorHint("The high point of the tilt range. (e.g. -100 or 0)  Note, camera elevation typically works where 0 is horizon level and 900 is straight down, though the range of accepted values can vary.")]
		public int ptz_tiltlimit_high = -100;
		[EditorName("Low tilt limit")]
		[EditorHint("The low point of the tilt range (e.g. 710 or 900).  Note, camera elevation typically works where 0 is horizon level and 900 is straight down, though the range of accepted values can vary.")]
		public int ptz_tiltlimit_low = 710;

		//IO_00000000_PT_157_066

		[EditorCategory("Trendnet TV-IP400 PTZ Settings")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.TrendnetTVIP400)]
		[EditorName("Parse Absolute Position")]
		[EditorHint("If checked, the absolute position of the camera will be parsed from the video stream. (use mjpeg.cgi in the imagery URL)")]
		public bool ptz_trendnet_absolute_position_parse = false;

		[EditorCategory("Configuration: <b>jpg</b>")]
		[EditorCondition_FieldMustBe("type", CameraType.jpg)]
		[EditorHint("Minimum number of milliseconds between image grabs. Use this to conserve bandwidth or improve resource usage when the source is frame-rate limited.")]
		[EditorName("Image Grab Delay")]
		public int delayBetweenImageGrabs = 0;

		[EditorCategory("Configuration: <b>vlc_transcode</b>")]
		[EditorCondition_FieldMustBe("type", CameraType.vlc_transcode)]
		[EditorName("Transcode Frame Rate Limit")]
		[EditorHint("Maximum frames per second for jpeg encoding of the video frames.  Strongly affects CPU usage.  Actual produced frame rate may be lower.")]
		public int vlc_transcode_fps = 10;
		[EditorName("Buffer Time")]
		[EditorHint("milliseconds.  Lower values cause the live view to be less delayed, but too-low values may be unreliable and may cause video corruption and dropped frames.  Default: 1000")]
		public int vlc_transcode_buffer_time = 1000;
		[EditorName("Jpeg Compression Quality")]
		[EditorHint("0 to 100")]
		public int vlc_transcode_image_quality = 80;
		[EditorName("Rotate / Flip")]
		public System.Drawing.RotateFlipType vlc_transcode_rotate_flip = System.Drawing.RotateFlipType.RotateNoneFlipNone;
		[EditorName("Throwaway Frames")]
		[EditorHint("Number of frames to drop upon beginning to receive the stream. Recommended 0 value unless video corruption is a problem at stream start.")]
		public int vlc_transcode_throwaway_frames = 0;
		[EditorName("Watchdog Time")]
		[EditorHint("Time in seconds to wait for an unresponsive stream to recover before restarting the camera. (0 or below to disable watchdog)")]
		public int vlc_watchdog_time = 20;

		[EditorCategory("Wanscam Compatibility Mode")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.WanscamCheap)]
		[EditorName("Enable Compatibility Mode")]
		[EditorHint("Check this box if using a Wanscam camera with vlc_transcode camera type and you are not getting imagery from the camera.")]
		public bool wanscamCompatibilityMode = false;
		[EditorName("Wanscam Frame Rate")]
		[EditorHint("This should match the frame rate set in the Wanscam interface.  Only required if Wanscam Compatibility Mode is enabled.")]
		public int wanscamFps = 25;

		[EditorCategory("Configuration: <b>h264_rtsp_proxy</b>")]
		[EditorCondition_FieldMustBe("type", CameraType.h264_rtsp_proxy)]
		[EditorName("Port to serve RTSP on")]
		[EditorHint("<br/>Only affects h264 cameras - the port specified here must be available or the camera will be inaccessible")]
		public ushort h264_port = 554;

		[EditorCategory("Configuration: <b>vlc_transcode</b> and <b>h264_rtsp_proxy</b>")]
		[EditorCondition_FieldMustBe("type", CameraType.vlc_transcode, CameraType.h264_rtsp_proxy, CameraType.vlc_transcode_to_html5)]
		[EditorName("Video Width")]
		[EditorHint("pixels. If 0, this value is autodetected.")]
		public ushort h264_video_width = 0;
		[EditorName("Video Height")]
		[EditorHint("pixels. If 0, this value is autodetected.")]
		public ushort h264_video_height = 0;

		public int order = -1;

		protected override string validateFieldValues()
		{
			id = id.ToLower();
			if ((type == CameraType.h264_rtsp_proxy || type == CameraType.vlc_transcode) && Environment.OSVersion.Platform != PlatformID.Win32NT)
				return "0The " + type.ToString() + " camera type is incompatible with the current server environment, which is " + Environment.OSVersion.Platform.ToString();
			if (string.IsNullOrWhiteSpace(name))
				return "0Camera name must not contain only whitespace.";
			if (!Util.IsAlphaNumeric(name, true))
				return "0Camera name must be alphanumeric, but may contain spaces.";
			if (!Util.IsAlphaNumeric(id, false))
				return "0Camera ID must be alphanumeric and not contain any spaces.";
			if (minPermissionLevel < 0 || minPermissionLevel > 100)
				return "0Permission Level must be between 0 and 100.";
			if (maxBufferTime < 3 && (type == CameraType.jpg || type == CameraType.mjpg))
				return "0Idle Timeout can't be below 3 for a " + type.ToString() + " camera.";
			if (maxBufferTime < 10 && (type == CameraType.h264_rtsp_proxy || type == CameraType.vlc_transcode))
				return "0Idle Timeout can't be below 10 for a " + type.ToString() + " camera.";
			if (maxBufferTime > 86400)
				return "0Idle Timeout can't be above 86400.  A short value (below 30) is recommended.";
			if (delayBetweenImageGrabs < 0)
				return "0The Image Grab Delay can't be less than 0.";
			if (delayBetweenImageGrabs > 600000)
				return "0The Image Grab Delay can't be greater than 600000.";
			if (this.h264_video_width < 0)
				return "0Video Width must be >= 0.";
			if (this.h264_video_height < 0)
				return "0Video Height must be >= 0.";
			if (type == CameraType.vlc_transcode && this.vlc_transcode_fps <= 0)
				return "0Transcode Frame Rate must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && this.vlc_transcode_buffer_time <= 0)
				return "0Buffer Time must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && (this.vlc_transcode_image_quality < 0 || this.vlc_transcode_image_quality > 100))
				return "0Image Quality must be between 0 and 100 (inclusive) for a " + type.ToString() + " camera.";
			if (this.ptzType == PtzType.Dev)
				return "0PTZ Type Dev is obsolete and will be removed from a future version.  Please choose another PTZ type.";
			if (ptz_panorama_selection_rectangle_width_percent < 0.0 || ptz_panorama_selection_rectangle_width_percent > 1.0)
				return "0PTZ Panorama Selection Rectangle Width must be between 0.0 and 1.0";
			if (ptz_panorama_selection_rectangle_height_percent < 0.0 || ptz_panorama_selection_rectangle_height_percent > 1.0)
				return "0PTZ Panorama Selection Rectangle Height must be between 0.0 and 1.0";
			if (ptz_idleresetpositionX < 0.0 || ptz_idleresetpositionX > 1.0)
				return "0PTZ Idle Pan Position (X) must be between 0.0 and 1.0";
			if (ptz_idleresetpositionY < 0.0 || ptz_idleresetpositionY > 1.0)
				return "0PTZ Idle Tilt Position (Y) must be between 0.0 and 1.0";
			if (ptz_idleresetpositionZ < 0.0 || ptz_idleresetpositionZ > 1.0)
				return "0PTZ Idle Zoom Position (Z) must be between 0.0 and 1.0";
			if (ptz_idleresettimeout < 10 || ptz_idleresettimeout > 604800)
				return "0PTZ Idle Position timeout must be between 10 and 604800";
			return "1";
		}

		public int GetMaxBufferTime()
		{
			if (type == CameraType.vlc_transcode_to_html5)
				return Math.Max(60, maxBufferTime);
			if (type == CameraType.h264_rtsp_proxy || type == CameraType.vlc_transcode)
				return Math.Max(10, maxBufferTime);
			return Math.Max(3, maxBufferTime);
		}
		public override string ToString()
		{
			return id;
		}
	}

	public enum CameraType
	{
		jpg, mjpg, h264_rtsp_proxy, vlc_transcode, vlc_transcode_to_html5
	}
	public enum PtzType
	{
		None, LoftekCheap, Dahua,
		WanscamCheap, TrendnetIP672,
		IPS_EYE01, TrendnetTVIP400,
		CustomPTZProfile, Dev, Hikvision,
		Huisun
	}
}

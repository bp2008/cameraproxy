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
		public CameraType type = CameraType.jpg;
		[EditorName("Imagery URL")]
		public string imageryUrl = "";
		[EditorHint("seconds. The server will stop requesting imagery from the source this long after the last image request is made by a client.")]
		[EditorName("Idle Timeout")]
		public int maxBufferTime = 10;
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
		[EditorCondition_FieldMustBe("ptzType", PtzType.LoftekCheap, PtzType.Dahua, PtzType.WanscamCheap, PtzType.IPS_EYE01, PtzType.TrendnetTVIP400)]
		[EditorName("PTZ Host Name")]
		public string ptz_hostName = "";
		[EditorName("PTZ User Name")]
		public string ptz_username = "";
		[IsPasswordField(true)]
		[EditorName("PTZ Password")]
		public string ptz_password = "";

		[EditorCategory("Dahua PTZ Settings")]
		[EditorCondition_FieldMustBe("ptzType", PtzType.Dahua)]
		[EditorName("PTZ Absolute X Position Offset")]
		[EditorHint("degrees")]
		public int ptz_absoluteXOffset = 0;
		[EditorName("PTZ Panorama Selection Rectangle Width")]
		[EditorHint("pixels")]
		public int ptz_panorama_selection_rectangle_width = 96;
		[EditorName("PTZ Panorama Selection Rectangle Height")]
		[EditorHint("pixels")]
		public int ptz_panorama_selection_rectangle_height = 54;
		[EditorName("Simple Panorama")]
		[EditorHint("If checked, thumbnails 0 through 27 will be displayed in a grid.  If unchecked, thumbnail number 99999 will be loaded as a full size panorama.")]
		public bool ptz_panorama_simple = true;
		[EditorName("Panorama represented vertical angle")]
		[EditorHint("degrees.  Adjust this as needed to calibrate vertical positioning in the panorama rectangle.  Default: 90 degrees")]
		public int ptz_panorama_degrees_vertical = 90;

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
		[EditorCondition_FieldMustBe("type", CameraType.vlc_transcode, CameraType.h264_rtsp_proxy)]
		[EditorName("Video Width")]
		[EditorHint("pixels. Required for vlc_transcode cameras.  Optional for h264_rtsp_proxy cameras.")]
		public ushort h264_video_width = 0;
		[EditorName("Video Height")]
		[EditorHint("pixels. Required for vlc_transcode cameras.  Optional for h264_rtsp_proxy cameras.")]
		public ushort h264_video_height = 0;
		
		public int order = -1;

		protected override string validateFieldValues()
		{
			id = id.ToLower();
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
			if (type == CameraType.vlc_transcode && this.h264_video_width <= 0)
				return "0Video Width must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && this.h264_video_height <= 0)
				return "0Video Height must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && this.vlc_transcode_fps <= 0)
				return "0Transcode Frame Rate must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && this.vlc_transcode_buffer_time <= 0)
				return "0Buffer Time must be > 0 for a " + type.ToString() + " camera.";
			if (type == CameraType.vlc_transcode && (this.vlc_transcode_image_quality < 0 || this.vlc_transcode_image_quality > 100))
				return "0Image Quality must be between 0 and 100 (inclusive) for a " + type.ToString() + " camera.";
			return "1";
		}

		public int GetMaxBufferTime()
		{
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
		jpg, mjpg, h264_rtsp_proxy, vlc_transcode
	}
	public enum PtzType
	{
		None, LoftekCheap, Dahua,
		WanscamCheap, TrendnetIP672,
		IPS_EYE01, TrendnetTVIP400
	}
}

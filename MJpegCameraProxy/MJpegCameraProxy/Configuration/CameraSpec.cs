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

		[EditorCategory("Camera Authentication (if required)")]
		[EditorName("Camera User Name")]
		public string username = "";
		[IsPasswordField(true)]
		[EditorName("Camera Password")]
		public string password = "";

		[EditorCategory("Minimum User Permission for Viewing")]
		[EditorHint("0 to 100.  A value of 0 means the camera does not require authentication.")]
		[EditorName("Minimum Permission")]
		public int minPermissionLevel = 0;

		[EditorCategory("Other Settings")]
		[EditorHint("<br/>Minimum number of milliseconds between image grabs (affects <b>jpg</b> cameras only). Use this to conserve bandwidth or improve resource usage when the source is frame-rate limited.")]
		[EditorName("Image Grab Delay")]
		public int delayBetweenImageGrabs = 0;
		[EditorHint("in seconds. (default: 10) (max: 86400) (min-jpg: 3) (min-mjpg: 3) (min-h264: 10)<br/>The server will stop requesting imagery from the source this long after the last image request is made by a client.")]
		[EditorName("Idle Timeout")]
		public int maxBufferTime = 10;

		[EditorCategory("Pan-Tilt-Zoom")]
		[EditorName("PTZ Type")]
		public PtzType ptzType = PtzType.None;
		[EditorName("PTZ Host Name")]
		public string ptz_hostName = "";
		[EditorName("PTZ User Name")]
		public string ptz_username = "";
		[IsPasswordField(true)]
		[EditorName("PTZ Password")]
		public string ptz_password = "";
		[EditorName("PTZ Absolute X Position Offset (Degrees)")]
		[EditorHint("<br/>Only affects Dahua PTZ cameras")]
		public int ptz_absoluteXOffset = 0;
		[EditorCategory("Dahua PTZ panorama control settings")]
		[EditorName("PTZ Panorama Selection Rectangle Width")]
		[EditorHint("pixels. Only affects Dahua PTZ cameras")]
		public int ptz_panorama_selection_rectangle_width = 96;
		[EditorName("PTZ Panorama Selection Rectangle Height")]
		[EditorHint("pixels. Only affects Dahua PTZ cameras")]
		public int ptz_panorama_selection_rectangle_height = 54;
		[EditorName("Simple Panorama")]
		[EditorHint("Only affects Dahua PTZ cameras. If checked, thumbnails 0 through 27 will be displayed in a grid.  If unchecked, thumbnail number 99999 will be loaded as a full size panorama.")]
		public bool ptz_panorama_simple = true;
		[EditorName("Panorama represented vertical angle")]
		[EditorHint("degrees. Only affects Dahua PTZ cameras.  Adjust this as needed to calibrate the vertical position of the panorama rectangle.  Default: 90 degrees")]
		public int ptz_panorama_degrees_vertical = 90;

		[EditorCategory("H264-Only Configuration (requires VLC Web Plugin to view)")]
		[EditorName("Port to serve RTSP on")]
		[EditorHint("<br/>Only affects h264 cameras - the port specified here must be available or the camera will be inaccessible")]
		public ushort h264_port = 554;
		[EditorName("(Optional) Video Width")]
		[EditorHint("if nonzero, overrides detected width")]
		public ushort h264_video_width = 0;
		[EditorName("(Optional) Video Height")]
		[EditorHint("if nonzero, overrides detected height")]
		public ushort h264_video_height = 0;
		
		public int order = -1;
		protected override string validateFieldValues()
		{
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
			if (maxBufferTime < 10 && (type == CameraType.h264))
				return "0Idle Timeout can't be below 10 for a " + type.ToString() + " camera.";
			if (maxBufferTime > 86400)
				return "0Idle Timeout can't be above 86400.  A short value (below 30) is recommended.";
			if (delayBetweenImageGrabs < 0)
				return "0The Image Grab Delay can't be less than 0.";
			if (delayBetweenImageGrabs > 10000)
				return "0The Image Grab Delay can't be greater than 10000.";
			return "1";
		}

		public int GetMaxBufferTime()
		{
			if (type == CameraType.h264)
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
		jpg, mjpg, h264
	}
	public enum PtzType
	{
		None, LoftekCheap, Dahua
	}
}

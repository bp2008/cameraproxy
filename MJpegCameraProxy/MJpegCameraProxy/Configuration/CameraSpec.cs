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
		public int ptz_absoluteXOffset = 0;

		[EditorCategory("H264-Only Configuration")]
		[EditorName("Port to serve RTSP on")]
		[EditorHint("<br/>Only affects h264 cameras - the port specified here must be available or the camera will be inaccessible")]
		public ushort h264_port = 554;

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
			return "1";
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

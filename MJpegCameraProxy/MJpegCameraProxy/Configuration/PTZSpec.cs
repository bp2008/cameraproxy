using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	public abstract class PTZSpec : FieldSettable
	{
		public int version;
		[EditorName("Profile Name")]
		public string name = "New PTZ Spec";
	}
}

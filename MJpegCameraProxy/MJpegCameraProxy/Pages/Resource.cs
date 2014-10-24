using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Reflection;

namespace MJpegCameraProxy.Pages
{
	public class Resource
	{
		public static string getStringResource(string key)
		{
			//if (key == "jquery_ui_1_10_2_custom_min.js")
			//    return MJpegCameraProxy.Properties.Resources.jquery_ui_1_10_2_custom_min_js;
			//if (key == "jquery_ui_1_10_2_custom_min.css")
			//    return MJpegCameraProxy.Properties.Resources.jquery_ui_1_10_2_custom_min_css;
			return "";
		}
		public static byte[] getBinaryResource(string key)
		{
			//key = key.Replace('-', '_');

			//object obj = MJpegCameraProxy.Properties.Resources.ResourceManager.GetObject(key);
			//if (obj != null && obj.GetType() == typeof(byte[]))
			//    return (byte[])obj;
			return new byte[0];
		}
	}
}

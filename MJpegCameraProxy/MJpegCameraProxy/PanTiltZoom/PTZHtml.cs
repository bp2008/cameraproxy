using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MJpegCameraProxy.PanTiltZoom
{
	internal static class PTZHtml
	{
		internal static string GetHtml(string camId, IPCameraBase cam, HtmlOptions options)
		{
			int arrowSize = 40;
			StringBuilder sb = new StringBuilder();
			sb.Append("<td id=\"pthCell\" class=\"nounderline\"><div id=\"pthDiv\">");

			sb.Append("<table style=\"width: 100%;\"><tbody><tr><td>");

			List<string> cells = new List<string>();
			if (options.showPtzArrows)
			{
				StringBuilder sbCell = new StringBuilder();
				sbCell.Append("<table id=\"pthTable\"><tbody><tr><td></td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-up.png", arrowSize, arrowSize, true), "u", keyboardKey: 38, keyboardDelay: 250));
				sbCell.Append("</td><td></td></tr><tr><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-left.png", arrowSize, arrowSize, true), "l", keyboardKey: 37, keyboardDelay: 250));
				sbCell.Append("</td><td></td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-right.png", arrowSize, arrowSize, true), "r", keyboardKey: 39, keyboardDelay: 250));
				sbCell.Append("</td></tr><tr><td></td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-down.png", arrowSize, arrowSize, true), "d", keyboardKey: 40, keyboardDelay: 250));
				sbCell.Append("</td><td></td></tr></tbody></table>");
				cells.Add(sbCell.ToString());
			}

			sb.Append(string.Join("</td><td>", cells));
			sb.Append("</td></tr></tbody></table>");

			sb.Append("</div></td></tr><tr>");
			return sb.ToString();
		}


		private static string img(string imageName, int width, int height, bool border = false)
		{
			return "<img src=\"Images/" + imageName + "\" style=\"width: " + width + "px; height: " + height + "px;" + (border ? " border: 1px dotted #777777;" : "") + "\" />";
		}

		private static string GetLinkToPTControl(string camId, string label, string command, string verification = "", int keyboardKey = -1, int keyboardDelay = 1000)
		{
			string jsCommand = "$.get('PTZ?id=" + camId + "&cmd=" + command + "');";

			if (!string.IsNullOrEmpty(verification))
				verification = "if(confirm('" + HttpUtility.HtmlAttributeEncode(HttpUtility.JavaScriptStringEncode(verification)) + "')) ";

			string keyboardBinding = "";
			if (keyboardKey > -1)
			{
				if (keyboardDelay < 0)
					keyboardDelay = 0;
				keyboardBinding = "<script type=\"text/javascript\">\r\nvar key" + keyboardKey + "Pressed = 0;\r\n$(document).keydown(function(event)\r\n{\r\n\tif(event.which == " + keyboardKey + " && key" + keyboardKey + "Pressed + " + keyboardDelay + " < new Date().getTime())\r\n\t{\r\n\t\tevent.preventDefault();\r\n\t\tkey" + keyboardKey + "Pressed = new Date().getTime();\r\n\t\t" + jsCommand + "\r\n\t}\r\n});\r\n</script>";
			}

			return keyboardBinding + "<a href=\"javascript:void(0)\" onclick=\"" + verification + jsCommand + " return false;\">" + label + "</a>";
		}
	}
}

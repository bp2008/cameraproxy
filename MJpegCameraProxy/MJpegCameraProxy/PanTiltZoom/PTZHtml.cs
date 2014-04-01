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
			sb.Append("<td id=\"pthCell\" class=\"nounderline\"><div id=\"pthDiv\"><script type=\"text/javascript\">");
			sb.Append(Javascript.Longclick());
			sb.Append("</script>");
			sb.Append(@"<style type=""text/css"">
.presettbl
{
	border-collapse: collapse;
}
.presettbl td
{
	width: 66px;
	height: 50px;
	padding: 1px;
}
.presettbl img
{
	width: 64px;
	height: 48px;
	border: 1px Solid #666666;
	margin-bottom: -7px;
}
</style>
<script type=""text/javascript"">
$(function()
{
	for(var i = 1; i <= 8; i++)
	{
		$('#preset' + i).longAndShortClick(
		function(ele)
		{
			if(ele.getAttribute('settable') == '1')
			{
				var mynum = parseInt(ele.getAttribute('mynum'));
				if(confirm('Are you sure you want to assign preset ' + mynum + '?'))
					$.get('PTZ?id=" + camId + @"&cmd=ps' + mynum).done(function()
					{
						$('#preset' + mynum).attr('src', 'PTZPRESETIMG?id=" + camId + @"&index=' + mynum + '&nocache=' + new Date().getTime());
					});
			}
			else
				alert('This preset is not settable.');
		},
		function(ele)
		{
			var mynum = parseInt(ele.getAttribute('mynum'));
			$.get('PTZ?id=" + camId + @"&cmd=pl' + mynum);
		});
	}
});
</script>");

			sb.Append("<table style=\"width: 100%;\"><tbody><tr><td>");

			List<string> cells = new List<string>();
			if (options.showPtzArrows)
			{
				StringBuilder sbCell = new StringBuilder();
				sbCell.Append("<table id=\"pthTable\"><tbody><tr><td>");
				if (options.showPtzDiagonals)
					sbCell.Append(GetLinkToPTControl(camId, img("arrow-up-left.png", arrowSize, arrowSize, true), "ul"));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-up.png", arrowSize, arrowSize, true), "u", keyboardKey: 38, keyboardDelay: options.tiltDelay));
				sbCell.Append("</td><td>");
				if (options.showPtzDiagonals)
					sbCell.Append(GetLinkToPTControl(camId, img("arrow-up-right.png", arrowSize, arrowSize, true), "ur"));
				sbCell.Append("</td></tr><tr><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-left.png", arrowSize, arrowSize, true), "l", keyboardKey: 37, keyboardDelay: options.tiltDelay));
				sbCell.Append("</td><td>");
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-right.png", arrowSize, arrowSize, true), "r", keyboardKey: 39, keyboardDelay: options.panDelay));
				sbCell.Append("</td></tr><tr><td>");
				if (options.showPtzDiagonals)
					sbCell.Append(GetLinkToPTControl(camId, img("arrow-down-left.png", arrowSize, arrowSize, true), "dl"));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, img("arrow-down.png", arrowSize, arrowSize, true), "d", keyboardKey: 40, keyboardDelay: options.panDelay));
				sbCell.Append("</td><td>");
				if (options.showPtzDiagonals)
					sbCell.Append(GetLinkToPTControl(camId, img("arrow-down-right.png", arrowSize, arrowSize, true), "dr"));
				sbCell.Append("</td></tr></tbody></table>");
				cells.Add(sbCell.ToString());
			}
			if (options.showZoomButtons)
			{
				StringBuilder sbCell = new StringBuilder();
				sbCell.Append("<table id=\"zTable\"><tbody>");
				if (options.showZoomInLong || options.showZoomOutLong)
				{
					sbCell.Append("<tr><td>");
					if (options.showZoomInLong)
						sbCell.Append(GetLinkToPTControl(camId, img("plus.png", arrowSize, arrowSize, true), "i3", keyboardKey: 73, keyboardDelay: options.zoomLongDelay));
					sbCell.Append("</td><td style=\"text-align: right;\">");
					if (options.showZoomOutLong)
						sbCell.Append(GetLinkToPTControl(camId, img("minus.png", arrowSize, arrowSize, true), "o3", keyboardKey: 79, keyboardDelay: options.zoomLongDelay));
					sbCell.Append("</td></tr>");
				}
				if (options.showZoomInMedium || options.showZoomOutMedium)
				{
					sbCell.Append("<tr><td>");
					if (options.showZoomInMedium)
						sbCell.Append(GetLinkToPTControl(camId, img("plus.png", (int)(arrowSize * 0.66), (int)(arrowSize * 0.66), true), "i2", keyboardDelay: options.zoomMediumDelay));
					sbCell.Append("</td><td style=\"text-align: right;\">");
					if (options.showZoomOutMedium)
						sbCell.Append(GetLinkToPTControl(camId, img("minus.png", (int)(arrowSize * 0.66), (int)(arrowSize * 0.66), true), "o2", keyboardDelay: options.zoomMediumDelay));
					sbCell.Append("</td></tr>");
				}
				if (options.showZoomInShort || options.showZoomOutShort)
				{
					sbCell.Append("<tr><td>");
					if (options.showZoomInShort)
						sbCell.Append(GetLinkToPTControl(camId, img("plus.png", (int)(arrowSize * 0.5), (int)(arrowSize * 0.5), true), "i1", keyboardDelay: options.zoomShortDelay));
					sbCell.Append("</td><td style=\"text-align: right;\">");
					if (options.showZoomOutShort)
						sbCell.Append(GetLinkToPTControl(camId, img("minus.png", (int)(arrowSize * 0.5), (int)(arrowSize * 0.5), true), "o1", keyboardDelay: options.zoomShortDelay));
					sbCell.Append("</td></tr>");
				}
				sbCell.Append("</tbody></table>");
				cells.Add(sbCell.ToString());
			}
			if (options.showZoomLevels)
			{
				StringBuilder sbCell = new StringBuilder();
				sbCell.Append("<style type=\"text/css\">#zTable { font-size: 2em; } #zTable td { padding: 6px; }</style>");
				sbCell.Append("<table id=\"zTable\"><tbody><tr><td></td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "0", "z0", keyboardKey: 49, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "1", "z1", keyboardKey: 49, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "2", "z2", keyboardKey: 50, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "3", "z3", keyboardKey: 51, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "4", "z4", keyboardKey: 52, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "5", "z5", keyboardKey: 53, keyboardDelay: 500));
				sbCell.Append("</td><td>");
				sbCell.Append(GetLinkToPTControl(camId, "6", "z6", keyboardKey: 54, keyboardDelay: 500));
				sbCell.Append("</td><td></td></tr></tbody></table>");
				cells.Add(sbCell.ToString());
			}

			if (options.showPresets)
			{
				StringBuilder sbCell = new StringBuilder();
				sbCell.Append("<table class=\"presettbl\"><tbody><tr>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 1, options)).Append("</td>"); // 49
				sbCell.Append("<td>").Append(GetPresetControl(camId, 2, options)).Append("</td>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 3, options)).Append("</td>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 4, options)).Append("</td>");
				sbCell.Append("</tr><tr>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 5, options)).Append("</td>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 6, options)).Append("</td>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 7, options)).Append("</td>");
				sbCell.Append("<td>").Append(GetPresetControl(camId, 8, options)).Append("</td>");
				sbCell.Append("</tr></tbody></table>");
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

		private static string GetPresetControl(string camId, int index, HtmlOptions options)
		{
			if (index < 1 || index > 8)
				throw new Exception("Invalid Preset Index");
			if (options.gettablePresets[index - 1])
			{
				string html = "<img mynum=\"" + index + "\" id=\"preset" + index + "\" alt=\"" + index + "\" src=\"PTZPRESETIMG?id=" + camId + "&index=" + index + "&nocache=" + DateTime.Now.Ticks + "\" settable=\"" + (options.settablePresets[index - 1] ? "1" : "0") + "\" />";
				return html;
			}
			return "";
		}
	}
}

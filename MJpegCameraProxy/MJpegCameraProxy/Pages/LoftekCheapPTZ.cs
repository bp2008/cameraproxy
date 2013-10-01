using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading;
using System.IO;

namespace MJpegCameraProxy
{
	public class LoftekCheapPTZ
	{
		public static string GetHtml(string camId)
		{
			return Get_PTZ_Markup_Loftek_Sentinel_D1(camId);
		}
		private static string Get_PTZ_Markup_Loftek_Sentinel_D1(string camId)
		{
			int arrowSize = 40;
			int magnifySize = 20;
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
			var mynum = parseInt(ele.getAttribute('mynum'));
			if(confirm('Are you sure you want to assign preset ' + mynum + '?'))
				$.get('PTZ?id=" + camId + @"&cmd=' + (28 + (mynum * 2))).done(function()
				{
					$('#preset' + mynum).attr('src', 'PTZPRESETIMG?id=" + camId + @"&index=' + mynum + '&nocache=' + new Date().getTime());
				});
		},
		function(ele)
		{
			var mynum = parseInt(ele.getAttribute('mynum'));
			$.get('PTZ?id=" + camId + @"&cmd=' + (29 + (mynum * 2)));
		});
	}
});
</script>");

			sb.Append("<table style=\"width: 100%;\"><tbody><tr><td>");


			sb.Append("<table id=\"pthTable\"><tbody><tr><td></td><td>");
			sb.Append(GetLinkToPTControl(camId, img("arrow-up.png", arrowSize, arrowSize, true), "0", keyboardKey: 38, keyboardDelay: 100));
			sb.Append("</td><td></td></tr><tr><td>");
			sb.Append(GetLinkToPTControl(camId, img("arrow-left.png", arrowSize, arrowSize, true), "4", keyboardKey: 37, keyboardDelay: 250));
			sb.Append("</td><td></td><td>");
			sb.Append(GetLinkToPTControl(camId, img("arrow-right.png", arrowSize, arrowSize, true), "6", keyboardKey: 39, keyboardDelay: 250));
			sb.Append("</td></tr><tr><td></td><td>");
			sb.Append(GetLinkToPTControl(camId, img("arrow-down.png", arrowSize, arrowSize, true), "2", keyboardKey: 40, keyboardDelay: 100));
			sb.Append("</td><td></td></tr></tbody></table>");

			sb.Append("</td><td>");

			sb.Append("<table><tbody><tr><td style=\"text-align:left;\">");
			string imgPlus = img("plus.png", magnifySize, magnifySize);
			sb.Append(GetLinkToPTControl(camId, imgPlus + imgPlus + imgPlus, "18", keyboardKey: 73, keyboardDelay: 1000)).Append("<br/>");
			sb.Append(GetLinkToPTControl(camId, imgPlus + imgPlus, "118", keyboardDelay: 1000)).Append("<br/>");
			sb.Append(GetLinkToPTControl(camId, imgPlus, "1118", keyboardDelay: 100));
			sb.Append("</td><td style=\"text-align:right; padding-left: 20px;\">");
			string imgMinus = img("minus.png", magnifySize, magnifySize);
			sb.Append(GetLinkToPTControl(camId, imgMinus + imgMinus + imgMinus, "16", keyboardKey: 79, keyboardDelay: 1000)).Append("<br/>");
			sb.Append(GetLinkToPTControl(camId, imgMinus + imgMinus, "116", keyboardDelay: 1000)).Append("<br/>");
			sb.Append(GetLinkToPTControl(camId, imgMinus, "1116", keyboardDelay: 1000));
			sb.Append("</td>");
			sb.Append("</tr></tbody></table>");

			sb.Append("</td><td style=\"font-size: 1.5em;\">");

			sb.Append("<table class=\"presettbl\"><tbody><tr>");
			sb.Append("<td>").Append(GetPresetControl(camId, 1)).Append("</td>"); // 49
			sb.Append("<td>").Append(GetPresetControl(camId, 2)).Append("</td>");
			sb.Append("<td>").Append(GetPresetControl(camId, 3)).Append("</td>");
			sb.Append("<td>").Append(GetPresetControl(camId, 4)).Append("</td>");
			sb.Append("</tr><tr>");
			sb.Append("<td>").Append(GetPresetControl(camId, 5)).Append("</td>");
			sb.Append("<td>").Append(GetPresetControl(camId, 6)).Append("</td>");
			sb.Append("<td>").Append(GetPresetControl(camId, 7)).Append("</td>");
			sb.Append("<td>").Append(GetPresetControl(camId, 8)).Append("</td>");
			sb.Append("</tr></tbody></table>");
			sb.Append("<table><tbody><tr><td>QUALITY </td>");
			sb.Append("<td>").Append(GetLinkToPTControl(camId, "1", "q1")).Append("</td>");
			sb.Append("<td>").Append(GetLinkToPTControl(camId, "2", "q2")).Append("</td>");
			sb.Append("<td>").Append(GetLinkToPTControl(camId, "3", "q3")).Append("</td>");
			sb.Append("</tr></tbody></table>");

			sb.Append("</td></tr></tbody></table>");

			sb.Append("</div></td></tr><tr>");
			return sb.ToString();
		}

		private static string GetPresetControl(string camId, int index)
		{
			if(index < 1 || index > 8)
				throw new Exception("Invalid Preset Index");
			string html = "<img mynum=\"" + index + "\" id=\"preset" + index + "\" alt=\"" + index + "\" src=\"PTZPRESETIMG?id=" + camId + "&index=" + index + "&nocache=" + DateTime.Now.Ticks + "\" />";
			return html;
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

		private enum CommandType { decoder_control, camera_control }
		public static void RunCommand(IPCameraBase ipCam, string cam, string command)
		{
			try
			{
				int preset_number = 0;
				if (ipCam == null)
					return;

				if (!string.IsNullOrEmpty(cam))
					cam = cam.ToLower();
				string cmdEnd = "";
				int duration = 500;
				CommandType commandType = CommandType.decoder_control;
				#region Commands
				if (command.StartsWith("q"))
				{
					commandType = CommandType.camera_control;
					if (command == "q1")
						command = "param=0&value=2";
					else if (command == "q1.5")
						command = "param=0&value=4";
					else if (command == "q2")
						command = "param=0&value=8";
					else if (command == "q2.5")
						command = "param=0&value=16";
					else if (command == "q3")
						command = "param=0&value=32";
					else
						return;
				}
				else if (command == "0") // UP
				{
					cmdEnd = "1";
					duration = 100;
				}
				else if (command == "2") // DOWN
				{
					cmdEnd = "3";
					duration = 100;
				}
				else if (command == "4") // LEFT
				{
					cmdEnd = "5";
					duration = 250;
				}
				else if (command == "6") // RIGHT
				{
					cmdEnd = "7";
					duration = 250;
				}
				else if (command == "16") // ZOOM OUT
				{
					cmdEnd = "19";
					duration = 1000;
				}
				else if (command == "18") // ZOOM IN
				{
					cmdEnd = "17";
					duration = 1000;
				}
				else if (command == "116") // ZOOM OUT FINE
				{
					command = "16";
					cmdEnd = "19";
					duration = 333;
				}
				else if (command == "118") // ZOOM IN FINE
				{
					command = "18";
					cmdEnd = "17";
					duration = 333;
				}
				else if (command == "1116") // ZOOM OUT FINER
				{
					command = "16";
					cmdEnd = "19";
					duration = 100;
				}
				else if (command == "1118") // ZOOM IN FINER
				{
					command = "18";
					cmdEnd = "17";
					duration = 100;
				}
				else if (command == "30" || command == "31" // Preset 1 - Mark / Recall
					|| command == "32" || command == "33" // 2
					|| command == "34" || command == "35" // 3
					|| command == "36" || command == "37" // 4
					|| command == "38" || command == "39" // 5
					|| command == "40" || command == "41" // 6
					|| command == "42" || command == "43" // 7
					|| command == "44" || command == "45" // 8
					)
				{
					int cmdNum = int.Parse(command);
					if (cmdNum % 2 == 0)
						preset_number = (cmdNum - 28) / 2;
				}
				else
					return; // Invalid Command
				#endregion

				string auth = "&user=" + ipCam.cameraSpec.ptz_username + "&pwd=" + ipCam.cameraSpec.ptz_password;

				if (commandType == CommandType.decoder_control)
				{
					SimpleProxy.GetData("http://" + ipCam.cameraSpec.ptz_hostName + "/decoder_control.cgi?command=" + command + auth);
					if (cmdEnd != "")
					{
						Thread.Sleep(duration);
						SimpleProxy.GetData("http://" + ipCam.cameraSpec.ptz_hostName + "/decoder_control.cgi?command=" + cmdEnd + auth);
					}
				}
				else if (commandType == CommandType.camera_control)
				{
					SimpleProxy.GetData("http://" + ipCam.cameraSpec.ptz_hostName + "/camera_control.cgi?" + command + auth);
				}
				if (preset_number > 0)
				{
					try
					{
						byte[] image = MJpegServer.cm.GetLatestImage(cam);
						if (image.Length > 0)
						{
							Util.WriteImageThumbnailToFile(image, Globals.ThumbsDirectoryBase + cam.ToLower() + preset_number + ".jpg");
						}
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
	}
}

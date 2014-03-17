using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;

namespace MJpegCameraProxy
{
	public class DahuaPTZ
	{
		public enum Direction
		{
			Up, Down, Left, Right, LeftUp, RightUp, LeftDown, RightDown
		}

		string baseCGIURL;
		string host, user, pass;
		System.Threading.Timer keepAliveTimer = null;
		int session = -1;
		int absoluteXOffset, thumbnailBoxWidth, thumbnailBoxHeight, panoramaVerticalDegrees;
		bool simplePanorama;
		LoginManager loginManager;
		long inner_id = 0;
		protected long CmdID
		{
			get
			{
				return Interlocked.Increment(ref inner_id);
			}
		}

		static DahuaPTZ()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			if (System.Net.ServicePointManager.DefaultConnectionLimit < 16)
				System.Net.ServicePointManager.DefaultConnectionLimit = 16;
		}
		public DahuaPTZ(string host, string user, string password, int absoluteXOffset, int thumbnailBoxWidth = 96, int thumbnailBoxHeight = 54, bool simplePanorama = true, int panoramaVerticalDegrees = 90)
		{
			this.host = host;
			this.user = user;
			this.pass = password;
			this.absoluteXOffset = absoluteXOffset;
			this.thumbnailBoxWidth = thumbnailBoxWidth;
			this.thumbnailBoxHeight = thumbnailBoxHeight;
			this.simplePanorama = simplePanorama;
			this.panoramaVerticalDegrees = panoramaVerticalDegrees;
			baseCGIURL = "http://" + host + "/cgi-bin/ptz.cgi?";
			loginManager = new LoginManager();
		}

		#region Login / Session Management
		private void DoLogin()
		{
			if (loginManager.AllowLogin())
				try
				{
					if (keepAliveTimer != null)
						keepAliveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

					session = -1;
					// Log in now
					// First, we need to get some info from the server
					string url = "http://" + host + "/RPC2_Login";
					string request = "{\"method\":\"global.login\",\"params\":{\"userName\":\"" + user + "\",\"password\":\"\",\"clientType\":\"Web3.0\"},\"id\":10000}";
					string response = HttpPost(url, request);

					var r1 = JSON.ParseJson(response);
					if (r1.error.code == 401)
					{
						// Unauthorized error; we expect this
						session = r1.session;
						string encryptionType = getJsonStringValue(response, "encryption");
						string pwtoken;
						if (encryptionType == "Basic")
							pwtoken = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(user + ":" + pass));
						else if (encryptionType == "Default")
						{
							string realm = getJsonStringValue(response, "realm");
							string random = getJsonStringValue(response, "random");
							pwtoken = Hash.GetMD5Hex(user + ":" + realm + ":" + pass).ToUpper();
							pwtoken = Hash.GetMD5Hex(user + ":" + random + ":" + pwtoken).ToUpper();
							pwtoken += pass; // Hey, I didn't invent this "encryption" type. LOL
						}
						else
							pwtoken = pass;

						// Finally, we can try to log in.
						request = "{\"method\":\"global.login\",\"session\":" + session + ",\"params\":{\"userName\":\"" + user + "\",\"password\":\"" + pwtoken + "\",\"clientType\":\"Web3.0\"},\"id\":10000}";
						response = HttpPost(url, request);
					}
					else
						Logger.Debug("Login error code not 401. Request Info Follows" + Environment.NewLine + "URL: " + url + Environment.NewLine + "Request: " + request + Environment.NewLine + "Response: " + response);

					keepAliveTimer = new System.Threading.Timer(new TimerCallback(timerTick), this, 120000, 120000);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
		}
		private string getJsonStringValue(string json, string key)
		{
			Match m = Regex.Match(json, "\"" + key + "\" *: *\"(.*?)\"");
			if (!m.Success)
				m = Regex.Match(json, "\"" + key + "\" *: *\\d+");
			return m.Success ? m.Groups[1].Value : null;
		}
		private void timerTick(object state)
		{
			KeepAlive();
		}
		private void KeepAlive()
		{
			string response = HttpPost("http://" + host + "/RPC2", "{\"method\":\"global.keepAlive\",\"params\":{\"timeout\": 300},\"session\":" + session + ",\"id\":" + CmdID + "}");
		}
		#endregion

		public void Shutdown(bool isAboutToStart)
		{
			SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=Up&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=ZoomWide&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=FocusFar&arg1=0&arg2=0&arg3=0", user, pass, true, false);
			SimpleProxy.GetData(baseCGIURL + "action=stop&channel=0&code=IrisSmall&arg1=0&arg2=0&arg3=0", user, pass, false, false);
			if (keepAliveTimer != null)
				keepAliveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
			keepAliveTimer = null;
			session = -1;
			loginManager = new LoginManager();
			if (isAboutToStart)
				DoLogin();
		}

		public static string GetHtml(string camId, IPCameraBase cam)
		{
			StringBuilder sb = new StringBuilder();
			#region HTML
			sb.Append("<td id=\"pthCell\" class=\"nounderline\"><div id=\"pthDiv\"><script type=\"text/javascript\">");
			sb.Append(Javascript.Longclick());
			sb.Append("</script><script type=\"text/javascript\">");
			sb.Append(Javascript.JqueryMousewheel());
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
#pthDiv
{
	background-color: #5C5C5C !important;
}
.ptztable
{
	background-color: #5C5C5C;
}
.ptzButtonCell
{
	width: 118px;
}
.ptzbuttons, .zfi
{
	width: 111px;
	height: 110px;
	padding: 5px 2px 2px 5px;
}
.zfi
{
	padding: 5px 5px 2px 5px;
	color: White;
	font-size: 10pt;
	font-family: Verdana, Arial, Sans-Serif;
}
.ptzarrow
{
	background: url(""../Images/ptzarrows.png"") no-repeat scroll 0 0 transparent;
}
.ptzbutton
{
	margin: 0 3px 3px 0;
	height: 34px;
	width: 34px;
	display: block;
	float: left;
}
#yt5
{
	background: url(""../Images/ptzclick.png"") no-repeat scroll 0 0 transparent;
}
#yt1 { background-position: 0 0; }
#yt2 { background-position: 0 -31px; }
#yt3 { background-position: 0 -62px; }
#yt4 { background-position: 0 -93px; }
#yt5 { background-position: 0 0; }
#yt6 { background-position: 0 -124px; }
#yt7 { background-position: 0 -155px; }
#yt8 { background-position: 0 -186px; }
#yt9 { background-position: 0 -217px; }
#yt1:hover { background-position: -34px 0; }
#yt2:hover { background-position: -34px -31px; }
#yt3:hover { background-position: -34px -62px; }
#yt4:hover { background-position: -34px -93px; }
#yt5:hover, #yt5.enabled { background-position: -34px 0; }
#yt6:hover { background-position: -34px -124px; }
#yt7:hover { background-position: -34px -155px; }
#yt8:hover { background-position: -34px -186px; }
#yt9:hover { background-position: -34px -217px; }

.label
{
	float: left;
	height: 28px;
	line-height: 28px;
	margin: 3px 0;
	text-align: center;
	text-decoration: none;
	width: 55px;
}
.increase, .decrease
{
	background: url(""../Images/increase.png"") no-repeat scroll 0 0 transparent;
	margin: 3px 0;
	height: 28px;
	width: 28px;
	display: block;
	float: left;
}
.decrease
{
	background-position: 0 -28px;
}
.increase:hover { background-position: -28px 0; }
.decrease:hover { background-position: -28px -28px; }

#joystickDiv
{
	width: 100px;
	height: 100px;
	cursor: crosshair;
}
.joystickBox50
{
	width: 48px;
	height: 48px;
	border: 1px solid black;
	float: left;
}
#gridDiv
{
	width: 672px;
	height: 216px;
	line-height: 0px;
}
#gridTbl
{
	border-collapse: collapse;
}
.gridcell
{
	padding: 0px;
	width: 96px;
	height: 54px;
	line-height: 0px;
}
.gridImg
{
	width: 96px;
	height: 54px;
}
#thumbnailBox
{
	background-color: transparent;
	border: 1px solid Red;
	position: relative;
	margin-top: -216px;
}
#thumbnailBoxInner
{
	background-color: transparent;
	border: 1px solid Blue;
	position: relative;
}
</style>
<script type=""text/javascript"">
$(function()
{
	$('#imgFrame').click(handleTargetClick);
	$('#zoomControls').mousewheel(function(e, delta, deltaX, deltaY)
	{
		e.preventDefault();
		if(deltaY < 0)
			WheelZoom--;
		else if(deltaY > 0)
			WheelZoom++;
		UpdateZoom();
	});
	InitializeGrid();
//	for(var i = 1; i <= 8; i++)
//	{
//		$('#preset' + i).longAndShortClick(
//		function(ele)
//		{
//			var mynum = parseInt(ele.getAttribute('mynum'));
//			if(confirm('Are you sure you want to assign preset ' + mynum + '?'))
//				$.get('PTZ?id=" + camId + @"&cmd=' + (28 + (mynum * 2))).done(function()
//				{
//					$('#preset' + mynum).attr('src', 'PTZPRESETIMG?id=" + camId + @"&index=' + mynum + '&nocache=' + new Date().getTime());
//				});
//		},
//		function(ele)
//		{
//			var mynum = parseInt(ele.getAttribute('mynum'));
//			$.get('PTZ?id=" + camId + @"&cmd=' + (29 + (mynum * 2)));
//		});
//	}
//	// Set up virtual joystick
//	var leftButtonDown = false;
//	var isMovingSpeed = false;
//	var joystickCommandDelayMs = 100;
//	var nextJoystickCommand = 0;
//	$(document).mousedown(function(e)
//	{
//		if(isMovingSpeed)
//		{
//			isMovingSpeed = false;
//			StopPTZ();
//		}
//		// Left mouse button was pressed, set flag
//		if(e.which === 1)
//			leftButtonDown = true;
//		return true;
//	});
//	$(document).mouseup(function(e)
//	{
//		if(isMovingSpeed)
//		{
//			isMovingSpeed = false;
//			StopPTZ();
//		}
//		// Left mouse button was released, clear flag
//		if(e.which === 1)
//			leftButtonDown = false;
//		return true;
//	});
//	$('#joystickDiv').mousemove(function(e)
//	{
//		if(leftButtonDown && nextJoystickCommand < new Date().getTime())
//		{
//			nextJoystickCommand = new Date().getTime() + joystickCommandDelayMs;
//			var offsets = $('#joystickDiv').offset();
//			var clickX = e.pageX - offsets.left;
//			var clickY = e.pageY - offsets.top;
//			var percentX = clickX / $('#joystickDiv').width();
//			var percentY = clickY / $('#joystickDiv').height();
//			isMovingSpeed = true;
//			MoveSpeed(percentX, percentY);
//		}
//	});
});
var ThumbX = -10000;
var ThumbY = -10000;
var ThumbZoom = 0;
var WheelZoom = 0;
var ThumbMouseDown = false;
var lastThumbX = -10000;
var lastThumbY = -10000;
var lastThumbZ = 0;
var ThumbBoxW = 96;
var ThumbBoxH = 54;
var ThumbBoxInnerMaxW = 94;
var ThumbBoxInnerMaxH = 52;
var ThumbBoxInnerMinW = 4;
var ThumbBoxInnerMinH = 2.25;
var ThumbBoxInnerStepW = ((ThumbBoxInnerMaxW - ThumbBoxInnerMinW) / 16.0);
var ThumbBoxInnerStepH = ((ThumbBoxInnerMaxH - ThumbBoxInnerMinH) / 16.0);
var EnableThumbZoom = false;
function InitializeGrid()
{
	$('#gridDiv').mousedown(function(e)
	{
		if(e.which == 1)
		{
			if(ThumbX == -10000)
				CreateThumbnailBox();
			e.preventDefault();
			ThumbMouseDown = true;
			UpdateThumbnailBox(e);
		}
	});
	$('#gridDiv').mousemove(function(e)
	{
		UpdateThumbnailBox(e);
	});
	$(document).mouseup(function(e)
	{
		ThumbMouseDown = false;
		
		clearTimeout(moveThumbnailBoxTimeout);
		moveThumbnailBoxTimeout = setTimeout(function()
		{
			ExecuteThumbnailMove();
		}, delayBetweenThumbnailBoxMove + 100);
	});
}
function UpdateThumbnailBox(e)
{
	if(!ThumbMouseDown)
		return;
	var gridDiv = $('#gridDiv');
	var offsets = gridDiv.offset();
	var clickX = e.pageX - offsets.left;
	var clickY = e.pageY - offsets.top;

	var thumbnailBox = $('#thumbnailBox');
	var tw = thumbnailBox.width();
	var th = thumbnailBox.height();
	ThumbX = (clickX - (tw / 2)) / gridDiv.width();
	ThumbY = (clickY - (th / 2)) / (gridDiv.height() - th);

	thumbnailBox.css('left', (clickX - (tw / 2)) + 'px');
	thumbnailBox.css('top', (clickY - (th / 2)) + 'px');

	var thumbnailBoxInner = $('#thumbnailBoxInner');
	var tiw = (ThumbBoxInnerStepW * (16 - ThumbZoom)) + ThumbBoxInnerMinW;
	var tih = (ThumbBoxInnerStepH * (16 - ThumbZoom)) + ThumbBoxInnerMinH;
	thumbnailBoxInner.css('width', tiw + 'px');
	thumbnailBoxInner.css('height', tih + 'px');
	thumbnailBoxInner.css('left', ((ThumbBoxInnerMaxW - tiw) / 2) + 'px');
	thumbnailBoxInner.css('top', ((ThumbBoxInnerMaxH - tih) / 2) + 'px');
	if(!EnableThumbZoom)
		thumbnailBoxInner.css('display', 'none');

	MoveCameraToThumbnailBoxPosition();
}
var nextThumbnailBoxMove = 0;
var delayBetweenThumbnailBoxMove = 500;
var nextZoomAdjust = 0;
var zoomChangeDelay = 1000;
var moveThumbnailBoxTimeout = null;
function MoveCameraToThumbnailBoxPosition()
{
	var time = new Date().getTime();
	if(time < nextThumbnailBoxMove)
	{
		clearTimeout(moveThumbnailBoxTimeout);
		moveThumbnailBoxTimeout = setTimeout(function()
		{
			ExecuteThumbnailMove();
		}, delayBetweenThumbnailBoxMove + 100);
		return;
	}
	clearTimeout(moveThumbnailBoxTimeout);
	nextThumbnailBoxMove = time + delayBetweenThumbnailBoxMove;
	ExecuteThumbnailMove();
}
function ExecuteThumbnailMove()
{
	var useZoom = lastThumbZ;
	if(EnableThumbZoom)
	{
		if(new Date().getTime() > nextZoomAdjust && ThumbZoom != lastThumbZ)
		{
			useZoom = ThumbZoom;
			nextZoomAdjust = new Date().getTime() + zoomChangeDelay;
		}
	}
	if(ThumbX != lastThumbX || ThumbY != lastThumbY || useZoom != lastThumbZ)
	{
		lastThumbX = ThumbX;
		lastThumbY = ThumbY;
		lastThumbZ = useZoom;
		ExecuteCommand('percent/' + ThumbX + '/' + ThumbY + '/' + useZoom);
	}
}
function CreateThumbnailBox()
{
	$('#gridDiv').append('<div id=""thumbnailBox"" style=""width:" + cam.dahuaPtz.thumbnailBoxWidth + @"px;height:" + cam.dahuaPtz.thumbnailBoxHeight + @"px;""><div id=""thumbnailBoxInner"" style=""width:" + (cam.dahuaPtz.thumbnailBoxWidth - 2) + @"px;height:" + (cam.dahuaPtz.thumbnailBoxHeight - 2) + @"px;""></div></div>');
	$('#thumbnailBox').css('margin-top', '-' + $('#gridDiv').height() + 'px');
	$('#thumbnailBox').mousemove(function(e)
	{
		UpdateThumbnailBox(e);
	});
	$('#thumbnailBox').mousedown(function(e)
	{
		if(e.which == 1)
		{
			ThumbMouseDown = true;
			e.preventDefault();
			UpdateThumbnailBox(e);
		}
	});
	$('#thumbnailBox').mousewheel(function(e, delta, deltaX, deltaY)
	{
		if(ThumbMouseDown)
		{
			e.preventDefault();
			if(deltaY < 0)
				ThumbZoom--;
			else if(deltaY > 0)
				ThumbZoom++;
			if(ThumbZoom < 0)
				ThumbZoom = 0;
			else if(ThumbZoom > 16)
				ThumbZoom = 16;
			UpdateThumbnailBox(e);
		}
	});
}
var lastZoom = 0;
var nextZoomTime = 0;
var zoomTimeDelay = 250;
function UpdateZoom()
{
	if(lastZoom != WheelZoom && new Date().getTime() > nextZoomTime)
	{
		nextZoomTime = new Date().getTime() + zoomTimeDelay;
		var zoomAmount = (WheelZoom - lastZoom) * 4;
		lastZoom = WheelZoom;
		Zoom(zoomAmount);
	}
}
function PTZDirection(buttonIndex)
{
	ExecuteCommand('move_simple/' + buttonIndex);
}
function Zoom(amount)
{
	ExecuteCommand('zoom/' + amount);
}
function Focus(amount)
{
	//ExecuteCommand('focus/' + amount);
}
function Iris(amount)
{
	ExecuteCommand('iris/' + amount);
}
function MoveSpeed(xPercent, yPercent)
{
	ExecuteCommand('m/' + xPercent + '/' + yPercent);
}
function StopPTZ()
{
	ExecuteCommand('stopall');
}
function ExecuteCommand(cmd)
{
	if(!refreshDisabled)
		$.get('PTZ?id=" + camId + @"&cmd=' + cmd);
}
// Click to center //
var clickToCenterEnabled = false;
function ToggleClickToCenter()
{
	clickToCenterEnabled = !clickToCenterEnabled;
	clickCausesResize = !clickToCenterEnabled;
	if(clickToCenterEnabled)
		$('#yt5').addClass('enabled');
	else
		$('#yt5').removeClass('enabled');
}
function handleTargetClick(e)
{
	if(clickToCenterEnabled)
	{
		var offsets = $('#imgFrame').offset();
		var clickX = e.pageX - offsets.left;
		var clickY = e.pageY - offsets.top;
		var percentX = clickX / $('#imgFrame').width();
		var percentY = clickY / $('#imgFrame').height();
		ExecuteCommand('frame/' + percentX + '/' + percentY);
	}
}
function fullPanoramaImageLoaded()
{
	var fullPanoramaImage = document.getElementById('fullPanoramaImage');
	var w = parseInt(fullPanoramaImage.naturalWidth / 2);
	var h = parseInt(fullPanoramaImage.naturalHeight / 2);
	$('#gridDiv').css('width', w + 'px');
	$('#gridDiv').css('height', h + 'px');
	$(fullPanoramaImage).css('width', w + 'px');
	$(fullPanoramaImage).css('height', h + 'px');
	$('#thumbnailBox').css('margin-top', '-' + h + 'px');
}
/////////////////////
</script>");
			#endregion
			sb.Append("<table style=\"width: 100%;\" class=\"ptztable\"><tbody><tr>");

			// Write PTZ arrows and "click-to-move" button
			sb.Append("<td class=\"ptzButtonCell\"><div class=\"ptzbuttons\">");
			for (int i = 1; i <= 9; i++)
			{
				sb.Append("<a href=\"javascript:");
				if (i != 5)
					sb.Append("PTZDirection(" + i + ")");
				else
					sb.Append("ToggleClickToCenter()");
				sb.Append("\" id=\"yt" + i + "\" class=\"ptzbutton");
				if (i != 5)
					sb.Append(" ptzarrow");
				sb.Append("\"></a>");
			}
			sb.Append("</div></td>");

			// Write Zoom, Focus, and Iris buttons
			sb.Append("<td class=\"ptzButtonCell\"><div class=\"zfi\">");
			sb.Append("<div id=\"zoomControls\"><a href=\"javascript:Zoom(10)\" class=\"increase\"></a><span class=\"label\"> Zoom </span><a href=\"javascript:Zoom(-10)\" class=\"decrease\"></a></div>");
			sb.Append("<a href=\"javascript:Focus(1000)\" class=\"increase\"></a><span class=\"label\"> Focus </span><a href=\"javascript:Focus(-1000)\" class=\"decrease\"></a>");
			sb.Append("<a href=\"javascript:Iris(5)\" class=\"increase\"></a><span class=\"label\"> Iris </span><a href=\"javascript:Iris(-5)\" class=\"decrease\"></a>");
			sb.Append("</div></td>");

			//sb.Append("<td class=\"joystickCell\"><div id=\"joystickDiv\"><div class=\"joystickBox50\"></div><div class=\"joystickBox50\"></div>");
			//sb.Append("<div class=\"joystickBox50\"></div><div class=\"joystickBox50\"></div></div></td>");

			if (cam.dahuaPtz.simplePanorama)
			{
				// Write pseudo-panorama image grid
				sb.Append("<td><div id=\"gridDiv\"><table id=\"gridTbl\"><tbody>");
				for (int j = 0; j < 4; j++)
				{
					sb.Append("<tr>");
					for (int i = 0; i < 7; i++)
					{
						sb.Append("<td class=\"gridcell\">");
						sb.Append(GetPresetControl(camId, i + (j * 7)));
						sb.Append("</td>");
					}
					sb.Append("</tr>");
				}
				sb.Append("</tbody></table></div></td>");
			}
			else
				sb.Append("<td><div id=\"gridDiv\"><img id=\"fullPanoramaImage\" src=\"PTZPRESETIMG?id=" + camId + "&index=99999&nocache=" + DateTime.Now.Ticks + "\" onload=\"fullPanoramaImageLoaded(); return false;\"></div></td>");


			sb.Append("</tr></tbody></table>");

			// Close wrapping container
			sb.Append("</div></td></tr><tr>");
			return sb.ToString();
		}

		private static string GetPresetControl(string camId, int index)
		{
			string html = "<img mynum=\"" + index + "\" id=\"preset" + index + "\" alt=\"" + index + "\" class=\"gridImg\" src=\"PTZPRESETIMG?id=" + camId + "&index=" + index + "&nocache=" + DateTime.Now.Ticks + "\" />";
			return html;
		}

		/// <summary>
		/// Runs the specified PTZ command.  Note that this is the only way to perform the commands in a thread-safe manner.
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="cmd"></param>
		public static void RunCommand(IPCameraBase cam, string cmd)
		{
			if (cam == null || cam.dahuaPtz == null || string.IsNullOrWhiteSpace(cmd))
				return;
			if (cam.ptzLock != null && cam.ptzLock.Wait(0))
			{
				try
				{

					string[] parts = cmd.Split('/');
					if (parts.Length < 1)
						return;

					if (parts[0] == "move_simple")
					{
						int moveButtonIndex = int.Parse(parts[1]);
						if (moveButtonIndex < 1 || moveButtonIndex > 9)
							return;

						int x = 0;
						int y = 0;

						if (moveButtonIndex < 4)
							y = -6000;
						else if (moveButtonIndex > 6)
							y = 6000;

						if (moveButtonIndex % 3 == 1)
							x = -6000;
						else if (moveButtonIndex % 3 == 0)
							x = 6000;

						cam.dahuaPtz.MoveSimple(x, y, 0);
					}
					else if (parts[0] == "zoom")
					{
						int amount = int.Parse(parts[1]);
						cam.dahuaPtz.MoveSimple(0, 0, amount);
					}
					else if (parts[0] == "focus")
					{
						//int amount = int.Parse(parts[1]);
					}
					else if (parts[0] == "iris")
					{
						int amount = int.Parse(parts[1]);
						cam.dahuaPtz.Iris(amount);
					}
					else if (parts[0] == "frame")
					{
						double x = double.Parse(parts[1]);
						double y = double.Parse(parts[2]);
						x -= 0.5;
						y -= 0.5;
						int amountX = (int)(20900.0 * x);
						int amountY = (int)(16500.0 * y);
						cam.dahuaPtz.MoveSimple(amountX, amountY, 0);
					}
					else if (parts[0] == "m" && parts.Length == 3)
					{
						// Constant motion from joystick-like control; disabled due to usability and reliability concerns.  The camera's http interface simply can't accept the necessary level of control.
						//
						//double x = double.Parse(parts[1]);
						//double y = double.Parse(parts[2]);
						//x -= 0.5;
						//y -= 0.5;
						//x *= 10;
						//y *= 10;
						//int speedX = (int)Math.Round(x);
						//int speedY = (int)Math.Round(y);
						//Stopwatch watch = new Stopwatch();
						//watch.Start();
						//cam.dahuaPtz.StopMoving();
						//watch.Stop();
						//Console.WriteLine(watch.ElapsedMilliseconds);
						//watch.Reset();
						//watch.Start();
						//cam.dahuaPtz.StartMoving(speedX, speedY);
						//watch.Stop();
						//Console.WriteLine(watch.ElapsedMilliseconds);
						//Console.WriteLine();
					}
					else if (parts[0] == "stopall")
					{
						cam.dahuaPtz.StopAll();
					}
					else if (parts[0] == "generatepseudopanorama" || parts[0] == "generatepseudopanoramafull")
					{
						bool full = parts[0].EndsWith("full");
						bool isFirstTime = true;
						int numImagesHigh = full ? 6 : 4;
						int numImagesWide = full ? 14 : 7;
						double degreesSeparationVertical = 900.0 / (numImagesHigh - 1);
						double degreesSeparationHorizontal = 3600.0 / -numImagesWide;
						for (int j = 0; j < numImagesHigh; j++)
						{
							for (int i = 0; i < numImagesWide; i++)
							{
								cam.dahuaPtz.PositionABS((int)(i * degreesSeparationHorizontal), (int)(j * degreesSeparationVertical), 0);
								Thread.Sleep(isFirstTime ? 7000 : 4500);
								isFirstTime = false;
								try
								{
									byte[] input = cam.LastFrame;
									if (input.Length > 0)
									{
										if (!full)
											input = ImageConverter.ConvertImage(input, maxWidth: 240, maxHeight: 160, useImageMagick: MJpegWrapper.cfg.UseImageMagick);
										FileInfo file = new FileInfo(Globals.ThumbsDirectoryBase + cam.cameraSpec.id.ToLower() + (i + (j * numImagesWide)) + ".jpg");
										Util.EnsureDirectoryExists(file.Directory.FullName);
										File.WriteAllBytes(file.FullName, input);
									}
								}
								catch (Exception ex)
								{
									Logger.Debug(ex);
								}
							}
						}
					}
					else if (parts[0] == "percent" && parts.Length == 4)
					{
						double x = 1 - double.Parse(parts[1]);
						double y = double.Parse(parts[2]);
						x *= 3600;
						y *= cam.dahuaPtz.panoramaVerticalDegrees * 10;
						cam.dahuaPtz.PositionABS((int)x, (int)y, int.Parse(parts[3]) * 8);
					}
					//if (preset_number > 0)
					//{
					//    try
					//    {
					//        byte[] image = MJpegServer.cm.GetLatestImage(cam);
					//        if (image.Length > 0)
					//        {
					//            Util.WriteImageThumbnailToFile(image, Globals.ThumbsDirectoryBase + cam.ToLower() + preset_number + ".jpg");
					//        }
					//    }
					//    catch (Exception ex)
					//    {
					//        Logger.Debug(ex);
					//    }
					//}
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
				finally
				{
					cam.ptzLock.Release();
				}
			}
		}

		public void MoveSimple(int xAmount, int yAmount, int zAmount)
		{
			DoAction("start", "Position", xAmount, yAmount, zAmount);
		}

		public void PositionABS(int x, int y, int z)
		{
			x = (x + (this.absoluteXOffset * 10)) % 3600;
			if (x < 0) x = 3600 + x;
			if (y < 0) y = 0;
			if (y > 900) y = 900;
			if (z < 1) z = 1;
			if (z > 128) z = 128;
			DoAction("start", "PositionABS", x, y, z);
		}

		public void FocusStart(int amount)
		{
			DoStartAction("start", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}
		public void FocusStop(int amount)
		{
			DoAction("stop", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}
		public void StartMoving(int speedX, int speedY)
		{
			int lowerLimit = -5;
			int upperLimit = 5;
			if (speedX < lowerLimit) speedX = lowerLimit;
			if (speedX > upperLimit) speedX = upperLimit;
			if (speedY < lowerLimit) speedY = lowerLimit;
			if (speedY > upperLimit) speedY = upperLimit;
			bool up = speedY < 0;
			bool down = speedY > 0;
			bool left = speedX < 0;
			bool right = speedX > 0;
			speedX = Math.Abs(speedX);
			speedY = Math.Abs(speedY);

			speedX += 3;
			speedY += 3;

			if (left && up) DoStartAction("start", "LeftUp", speedY, speedX, 0);
			else if (right && up) DoStartAction("start", "RightUp", speedY, speedX, 0);
			else if (left && down) DoStartAction("start", "LeftDown", speedY, speedX, 0);
			else if (right && down) DoStartAction("start", "RightDown", speedY, speedX, 0);
			else if (left) DoStartAction("start", "Left", speedX, 0, 0);
			else if (right) DoStartAction("start", "Right", speedX, 0, 0);
			else if (up) DoStartAction("start", "Up", speedY, 0, 0);
			else DoStartAction("start", "Down", speedY, 0, 0);
		}
		public void StopMoving()
		{
			DoAction("stop", "Up", 0, 0, 0);
		}

		public void StopAll()
		{
			DoAction("stop", "Up", 0, 0, 0);
			DoAction("stop", "ZoomWide", 0, 0, 0);
		}

		public void Iris(int amount)
		{
			if (amount < 1)
				amount = 1;
			if (amount > 8)
				amount = 8;
			DoAction("start", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
			DoAction("stop", amount > 0 ? "IrisLarge" : "IrisSmall", Math.Abs(amount), 0, 0);
		}

		public void GotoPreset(int preset)
		{
			DoAction("start", "GotoPreset", preset, 0, 0);
		}

		public void SetPreset(int preset)
		{
			DoAction("start", "SetPreset", preset, 0, 0);
		}

		protected void DoStartAction(string action, string code, int arg1, int arg2, int arg3)
		{
			DoAction(action, code, arg1, arg2, arg3);
		}
		protected void DoAction(string action, string code, int arg1, int arg2, int arg3)
		{
			//string url = baseCGIURL + "action=" + action + "&channel=0&code=" + code + "&arg1=" + arg1 + "&arg2=" + arg2 + "&arg3=" + arg3;
			PTZCommand c = new PTZCommand();
			c.id = (int)CmdID;
			c.method = "ptz." + action;
			c.param = new PTZParams(code, arg1, arg2, arg3);
			c.session = session;
			string jsonString = c.GetJson();
			string url = "http://" + host + "/RPC2";
			string response = HttpPost(url, jsonString);
			int errorcode;
			if (!int.TryParse(getJsonStringValue(response, "code"), out errorcode))
			{
				errorcode = -1;
				try
				{
					var r1 = JSON.ParseJson(response);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "Request Info Follows" + Environment.NewLine + "URL: " + url + Environment.NewLine + "Request: " + jsonString + Environment.NewLine + "Response: " + response);
					errorcode = -2;
				}
			}
			if (errorcode == 404 || errorcode == -2)
				DoLogin();
		}

		protected string HttpPost(string URI, string data)
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				req.Proxy = null;
				req.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
				req.Headers["Accept-Encoding"] = "gzip, deflate";
				req.Headers["Accept-Language"] = "en-US,en;q=0.5";
				req.KeepAlive = true;
				//req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
				req.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
				req.Method = "POST";
				//req.UserAgent = "Mozilla/5.0 (DahuaPTZ Service)";
				req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:22.0) Gecko/20100101 Firefox/22.0";
				req.Headers["X-Request"] = "JSON";
				req.Headers["X-Requested-With"] = "XMLHttpRequest";
				req.Referer = "http://" + req.Host + "/";
				req.CookieContainer = new CookieContainer();
				req.CookieContainer.Add(new System.Net.Cookie("DHLangCookie30", "%2Fcustom_lang%2FEnglish.txt", "/", req.Host));
				if (session != -1)
					req.CookieContainer.Add(new System.Net.Cookie("DhWebClientSessionID", session.ToString(), "/", req.Host));

				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
				req.ContentLength = bytes.Length;

				using (Stream os = req.GetRequestStream())
				{
					os.Write(bytes, 0, bytes.Length);
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					return sr.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Will log in again.");
				DoLogin();
				return ex.ToString();
			}
		}
	}

	#region JSON Control (much faster than cgi API)
	public class PTZCommand
	{
		public int id;
		public string method;
		public Params param;
		public long session;
		public string GetJson()
		{
			return "{\"method\" : \"" + method + "\", \"session\" : " + session + ",\"params\" : " + param.GetJson() + ",\"id\" : " + id + "}";
		}
	}
	public abstract class Params
	{
		public abstract string GetJson();
	}
	public class PTZParams : Params
	{
		public int arg1, arg2, arg3, channel;
		public string code;
		public PTZParams(string code, int arg1, int arg2, int arg3)
		{
			this.code = code;
			this.channel = 0;
			//if (speed < 1)
			//    speed = 1;
			//if (speed > 8)
			//    speed = 8;
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.arg3 = arg3;
		}
		public override string GetJson()
		{
			return "{\"channel\" : " + channel + ", \"code\" : \"" + code + "\",\"arg1\" : " + arg1 + ",\"arg2\" : " + arg2 + ",\"arg3\" : " + arg3 + "}";
		}
	}
	public class LoginManager
	{
		Queue<DateTime> lastLogins = new Queue<DateTime>();
		public LoginManager()
		{
		}
		public bool AllowLogin()
		{
			DateTime now = DateTime.Now;
			try
			{
				if (lastLogins.Count < 5)
					return true;
				if (lastLogins.Peek() < now.Subtract(TimeSpan.FromMinutes(1)))
				{
					lastLogins.Dequeue(); // Oldest login in queue was older than a minute ago
					return true;
				}
				else
					return false; // Oldest login in queue was newer than a minute ago.
			}
			finally
			{
				lastLogins.Enqueue(now);
			}
		}
	}
	#endregion
}

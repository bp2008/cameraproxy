using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using MJpegCameraProxy.Configuration;
using System.Web;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy
{
	public class CamPage
	{
		public static string GetHtml(string camId, bool enableAutoResize, int refreshTime = 250, int disableRefreshAfter = 600000, HttpProcessor httpProcessor = null)
		{
			IPCameraBase cam = MJpegServer.cm.GetCameraAndGetItRunning(camId);
			if (cam == null)
				return "NO";
			int width = 0;
			int height = 0;
			int patience = (cam.cameraSpec.delayBetweenImageGrabs > 0 ? cam.cameraSpec.delayBetweenImageGrabs : 0) + 5000;
			string cameraImgLink = @"<img id=""imgFrame"" class=""CamImg"" />";
			string keepalive = "";
			if (cam.cameraSpec.type == CameraType.h264_rtsp_proxy)
			{
				bool sizeOverridden = cam.cameraSpec.h264_video_width > 0 && cam.cameraSpec.h264_video_height > 0;
				width = sizeOverridden ? cam.cameraSpec.h264_video_width : 640;
				height = sizeOverridden ? cam.cameraSpec.h264_video_height : 360;

				keepalive = @"
				var keepaliveInterval;
				$(function()
				{
					sizeOverridden = " + (sizeOverridden ? "true" : "false") + @";
					keepaliveInterval = setInterval(keepalive, 4000);
					var vlc = document.getElementById('vlc');
					registerVLCEvent('MediaPlayerPlaying', handlePlayerPlaying);
					var url = '" + HttpUtility.HtmlEncode(HttpUtility.JavaScriptStringEncode(MJpegServer.cm.GetRTSPUrl(cam.cameraSpec.id, httpProcessor))) + @"';
					url = url.replace('$$$HOST$$$', location.hostname);
					vlc.playlist.add(url);
					vlc.playlist.play();
					if(sizeOverridden)
						vlc.video.aspectRatio = '" + width + ":" + height + @"';
				});
				function keepalive()
				{
					if(refreshDisabled)
						clearInterval(keepaliveInterval);
					else
						$.ajax('keepalive?id=" + camId + @"');
				}
				function handlePlayerPlaying()
				{
					if(!sizeOverridden)
					{
						setTimeout(resize, 500);
						setTimeout(resize, 1000);
						setTimeout(resize, 1500);
					}
				}
				function registerVLCEvent(event, handler)
				{
					var vlc = document.getElementById('vlc');
					if (vlc)
					{
						if (vlc.attachEvent)
						{
							// Microsoft
							vlc.attachEvent(event, handler);
						} else if (vlc.addEventListener)
						{
							// Mozilla: DOM level 2
							vlc.addEventListener(event, handler, false);
						} else
						{
							// DOM level 0
							vlc['on' + event] = handler;
						}
					}
				}";
				cameraImgLink = @"<div id=""vlcFrame"" style=""width:" + width + @"px;height:" + height + @"px;""><embed type=""application/x-vlc-plugin"" id=""vlc"" pluginspage=""http://www.videolan.org"" width=""" + width + @""" height=""" + height + @""" toolbar=""false"" src="""" mute=""false"" /></div>";
			}
			else
			{
				for (int i = 0; i < 50; i++)
				{
					if (cam.ImageSize.Width != 0 && cam.ImageSize.Height != 0)
						break;
					Thread.Sleep(100);
				}
				if (cam.ImageSize.Width == 0 || cam.ImageSize.Height == 0)
					return @"<!DOCTYPE HTML>
						<html>
						<head>
							<title>" + HttpUtility.HtmlEncode(cam.cameraSpec.name) + @"</title>
						</head>
						<body>
						This camera is starting up.<br/>
						Please <a href=""javascript:top.location.reload();"">try again in a few moments</a>.
						</body>
						</html>";
				width = cam.ImageSize.Width;
				height = cam.ImageSize.Height;
			}

			string minutesLabel = disableRefreshAfter < 0 ? "a very long time" : (TimeSpan.FromMilliseconds(disableRefreshAfter).TotalMinutes + " minutes");

			return @"<!DOCTYPE HTML>
<html>
<head>
	<title>" + HttpUtility.HtmlEncode(cam.cameraSpec.name) + @"</title>
	<script src=""../" + CameraProxyGlobals.jQueryPath + @""" type=""text/javascript""></script>
	<script type=""text/javascript"">
		var disableRefreshAfter = " + disableRefreshAfter + @";
		var refreshDisabled = false;
		var originwidth = parseInt(" + width + @");
		var originheight = parseInt(" + height + @");
		var lastUpdate = 0;
		var clickCausesResize = true;
		var showPTH = " + (cam.cameraSpec.ptzType == PtzType.None ? "false" : "true") + @";
		var sizeOverridden = false;
		$(function()
		{
			if(disableRefreshAfter > -1)
				setTimeout(""disableRefresh()"", disableRefreshAfter);
		});
		" + keepalive + @"
		function myOnLoad()
		{
			$(""#imgFrame"").load(function ()
			{
" + (refreshTime < 0 ? "" : @"
				if (!this.complete || typeof this.naturalWidth == ""undefined"" || this.naturalWidth == 0)
				{
					alert('Bad image data was received.  A data transmission error may have occurred, or this camera may be offline.  Please reload this page to try again.');
				}
				else if(!refreshDisabled)
				{
					lastUpdate = new Date().getTime();
					setTimeout(""GetNewImage();"", " + refreshTime + @");
				}
") + @"
			});
			$(""#imgFrame"").error(function ()
			{
				setTimeout(""GetNewImage();"", " + (refreshTime < 0 ? 1000 : refreshTime) + @");
			});
			GetNewImage();
		}
		function PopupMessage(msg)
		{
			var pm = $(""#popupMessage"");
			if(pm.length < 1)
				$(""#camFrame"").after('<div id=""popupFrame""><div id=""popupMessage"">' + msg + '</div><center><input type=""button"" value=""Close Message"" onclick=""CloseMessage()""/></center></div>');
			else
				pm.append(msg);
		}
		function CloseMessage()
		{
			$(""#popupFrame"").remove();
		}
		function GetNewImage()
		{
			$(""#imgFrame"").attr('src', '" + camId + "." + (refreshTime < 0 ? "m" : "") + @"jpg?patience=" + patience + "&" + httpProcessor.GetParam("imgargs") + @"&nocache=' + new Date().getTime());
		}
		function disableRefresh()
		{
			refreshDisabled = true;
			PopupMessage('This page has been open for " + minutesLabel + @".  To save resources, the image will no longer refresh automatically.  <a href=""javascript:top.location.reload();"">Click here</a> to reload this page.');" + (refreshTime < 0 ? @"$(""#imgFrame"").attr('src', '" + camId + @".jpg?nocache=' + new Date().getTime());" : "") + @"
		}
		" + (enableAutoResize ? "$(window).load(resize); $(window).resize(resize);" : "") + @"
		function resize(width, height)
		{
			var newHeight;
			var newWidth;
			var currentImage = document.getElementById(""imgFrame"");
			if (currentImage == null)
			{
				currentImage = document.getElementById(""vlcFrame"");
				
				var vlc = document.getElementById('vlc')
				if(vlc && !sizeOverridden)
				{
					originwidth = parseInt(vlc.video.width);
					originheight = parseInt(vlc.video.height);
					if(originwidth == 0)
						originwidth = 640;
					if(originheight == 0)
						originheight = 360;
				}
			}
			if (currentImage == null)
				return;
			if (typeof (width) == 'undefined' || typeof (height) == 'undefined')
			{
				var imgOff = FindOffsets(currentImage);
				var screenOff = GetViewportDims();
				// Calculate available dimensions
				var availableHeight = (screenOff.height - imgOff.top) - 7 - (showPTH ? 130 : 0);
				var availableWidth = (screenOff.width - imgOff.left) - 21;
				// Take into consideration the original width and height for the image
				newHeight = originheight < availableHeight ? originheight : availableHeight;
				newWidth = originwidth < availableWidth ? originwidth : availableWidth;
				// Calculate ratios
				var originRatio = originwidth / originheight;
				var newRatio = newWidth / newHeight;
				if (newRatio < originRatio)
					newHeight = newWidth / originRatio;
				else
					newWidth = newHeight * originRatio;
				currentImage.onclick = function ()
				{
					if(clickCausesResize)
						resize(originwidth, originheight);
				}
			}
			else
			{
				var newHeight = height;
				var newWidth = width;
				currentImage.onclick = function ()
				{
					if(clickCausesResize)
						resize();
				}
			}
			$(currentImage).height(newHeight);
			$(currentImage).width(newWidth);

			$(""#vlcFrame"").width(newWidth);
			$(""#vlcFrame"").height(newHeight);
			$(""#vlc"").attr('width', newWidth + 'px');
			$(""#vlc"").attr('height', newHeight + 'px');
			
			$(""#camFrame"").width(newWidth);
		}
		function GetViewportDims()
		{
			var w;
			var h;
			if (typeof (window.innerWidth) != 'undefined')
			{
				w = window.innerWidth;
				h = window.innerHeight;
			}
			else if (typeof (document.documentElement) != 'undefined' && typeof (document.documentElement.clientWidth) != 'undefined' && document.documentElement.clientWidth != 0)
			{
				w = document.documentElement.clientWidth;
				h = document.documentElement.clientHeight;
			}
			else
			{
				w = document.getElementsByTagName('body')[0].clientWidth;
				h = document.getElementsByTagName('body')[0].clientHeight;
			}
			return { width: w, height: h };
		}
		function FindOffsets(node)
		{
			var oLeft = 0;
			var oTop = 0;
			var oWidth = node.offsetWidth;
			var oHeight = node.offsetHeight;
			if (oWidth == 0)
				oWidth += node.scrollWidth;
			oLeft = node.offsetLeft;
			oTop = node.offsetTop;
			if (oHeight == 0)
			{
				if (node.childNodes.length > 0)
				{
					oHeight += node.childNodes[0].offsetHeight;
					oTop = node.childNodes[0].offsetTop;
				}
				if (oHeight == 0)
					oHeight += node.scrollHeight;
			}
			node = node.offsetParent;
			while (node)
			{
				oLeft += node.offsetLeft;
				oTop += node.offsetTop;
				node = node.offsetParent;
			}
			return { left: oLeft, top: oTop, width: oWidth, height: oHeight };
		}
	</script>
	<style type=""text/css"">
		.CamImg
		{
			width: " + width + @"px;
			height: " + height + @"px;
		}
		#vlcFrame
		{
			width: " + width + @"px;
			height: " + height + @"px;
		}
		body
		{
			margin: 3px;
		}
		#camFrame
		{
			background-color: #666666;
			border: 1px Solid Black;
			border-radius: 5px;
			padding: 3px;
			width: " + width + @"px;
		}
		#popupFrame
		{
			position: fixed;
			background-color: #BB0000;
			border: 1px Solid White;
			border-radius: 5px;
			padding: 5px;
			top: 15px;
			width: 300px;
			left: 20px;
		}
		#popupMessage
		{
			color: White;
			margin-bottom: 5px;
		}
		#popupMessage a
		{
			color: #DDDDFF;
		}
		#pthDiv, #camControls
		{
			margin-top: 3px;
			padding: 0px 3px 0px 3px;
			background-color: #AAAAAA;
		}
		#pthCell
		{
			vertical-align: top;
		}
		#pthTable td
		{
			text-align: center;
		}
		.nounderline, .nounderline a
		{
			text-decoration: none;
		}
		.flip-horizontal
		{
			-moz-transform: scaleX(-1);
			-webkit-transform: scaleX(-1);
			-o-transform: scaleX(-1);
			transform: scaleX(-1);
			-ms-filter: fliph; /*IE*/
			filter: fliph; /*IE*/
		}
		.flip-vertical
		{
			-moz-transform: scaleY(-1);
			-webkit-transform: scaleY(-1);
			-o-transform: scaleY(-1);
			transform: scaleY(-1);
			-ms-filter: flipv; /*IE*/
			filter: flipv; /*IE*/
		}
		.flip-both
		{
			-moz-transform: scale(-1,-1);
			-webkit-transform: scale(-1,-1);
			-o-transform: scale(-1,-1);
			transform: scale(-1,-1);
			-ms-filter: flipv fliph; /*IE*/
			filter: flipv fliph; /*IE*/
		}
		.arrow-up
		{
			width: 0px;
			height: 0px;
			border-bottom: 20px solid #00AA00;
			border-left: 20px solid transparent;
			border-right: 20px solid transparent;
		}
		.arrow-down
		{
			width: 0px;
			height: 0px;
			border-top: 20px solid #00AA00;
			border-left: 20px solid transparent;
			border-right: 20px solid transparent;
		}
		.arrow-left
		{
			width: 0px;
			height: 0px;
			border-bottom: 10px solid transparent;
			border-top: 10px solid transparent;
			border-right: 10px solid #00AA00;
		}
		.arrow-right
		{
			width: 0px;
			height: 0px;
			border-bottom: 10px solid transparent;
			border-left: 10px solid #00AA00;
			border-top: 10px solid transparent;
		}
	</style>
</head>
<body onload=""myOnLoad();"">
	<div id=""camFrame"">
		" + cameraImgLink + @"
		<table style=""width: 100%"">
			<tbody>
				<tr>
					" + PTZ.GetHtml(camId, cam) + @"
				</tr>
			</tbody>
		</table>
	</div>
</body>
</html>";
		}
	}
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.IO;
//using System.Drawing;

//namespace MJpegCameraProxy
//{

//    public class IP672SeriesPTZ
//    {
//        public const int imageWidth = 1280;
//        public const int imageHeight = 800;
//        public const int panLimitLeft = -164;
//        public const int panLimitRight = 156;
//        public const int tiltLimitDown = -31;
//        public const int tiltLimitUp = 100;
//        public const int panRegionStep = 46;
//        public const int tiltRegionStep = 30; // 40
//        public const int thumbCountX = 8;
//        public const int thumbCountY = 5; // 4
//        public const int degreesPerSecond = 50;
//        public int pan_abs = 0;
//        public int tilt_abs = 0;
//        private string host;
//        private string movePath;
//        private int pan_home_abs = 0;
//        private int tilt_home_abs = 0;
//        EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);

//        public IP672SeriesPTZ(string host)
//        {
//            this.host = host;
//            movePath = "http://" + host + "/cgi/ptdc.cgi?command=set_relative_pos";
//        }
//        public void GenerateThumbnailRegion()
//        {
//            lock (this)
//            {
//                innerCalibrate();

//                for (int y = 0, j = tiltLimitUp; y < thumbCountY; y++, j -= tiltRegionStep)
//                {
//                    for (int x = 0, i = panLimitLeft; x < thumbCountX; x++, i += panRegionStep)
//                    {
//                        Move_Absolute_Synchronous(i, j);
//                        byte[] imgData = null;
//                        using (MemoryStream ms = new MemoryStream(imgData))
//                        {
//                            Bitmap bmp = new Bitmap(ms);
//                        }
//                    }
//                }
//            }
//        }
//        private void Move_Relative(int x, int y)
//        {
//            Console.WriteLine("Relative " + x + ", " + y);
//            SimpleProxy.GetData(movePath + "&posX=" + x + "&posY=" + y);
//            pan_abs += x;
//            tilt_abs += y;
//            if (pan_abs < panLimitLeft)
//                pan_abs = panLimitLeft;
//            else if (pan_abs > panLimitRight)
//                pan_abs = panLimitRight;
//            if (tilt_abs < tiltLimitDown)
//                tilt_abs = tiltLimitDown;
//            else if (tilt_abs > tiltLimitUp)
//                tilt_abs = tiltLimitUp;
//        }
//        public void Move_Absolute_Synchronous(int x, int y)
//        {
//            lock (this)
//            {
//                Console.WriteLine("Ordered to " + x + ", " + y);

//                int dx = x - pan_abs;
//                int dy = y - tilt_abs;
//                Move_Relative_Synchronous(dx, dy);
//            }
//        }
//        public void Move_Relative_Synchronous(int x, int y)
//        {
//            lock (this)
//            {
//                int absDx = Math.Abs(x);
//                int absDy = Math.Abs(y);
//                if (absDx > 200)
//                {
//                    int dx1 = absDx / 2;
//                    int dx2 = absDx - dx1;
//                    int dy1 = absDy / 2;
//                    int dy2 = absDy - dy1;

//                    if (x < 0)
//                    {
//                        dx1 *= -1;
//                        dx2 *= -1;
//                    }
//                    if (y < 0)
//                    {
//                        dy1 *= -1;
//                        dy2 *= -1;
//                    }

//                    Move_Relative_Synchronous(dx1, dy1);
//                    Console.WriteLine("Current Position is " + pan_abs + ", " + tilt_abs);
//                    Move_Relative_Synchronous(dx2, dy2);
//                }
//                else
//                {
//                    Move_Relative(x, y);
//                    int WaitTime = (int)(((double)Math.Max(Math.Abs(x), Math.Abs(y)) / degreesPerSecond) * 1000);
//                    ewh.WaitOne(WaitTime);
//                }

//                Console.WriteLine("Current Position is " + pan_abs + ", " + tilt_abs);
//            }
//        }

//        public void Calibrate()
//        {
//            lock (this)
//            {
//                innerCalibrate();
//            }
//        }
//        private void innerCalibrate()
//        {
//            Move_Relative_Synchronous(-200, -100);
//            Move_Relative_Synchronous(-200, -100);
//            Move_Relative_Synchronous(-panLimitLeft, -tiltLimitDown);
//        }

//        public void Move_Home()
//        {
//            Move_Absolute_Synchronous(pan_home_abs, tilt_home_abs);
//        }

//        public void Set_Home()
//        {
//            pan_home_abs = pan_abs;
//            tilt_home_abs = tilt_abs;
//        }

//        public void Move_Relative_Pixel_Synchronous(double x, double y)
//        {
//            double dx = (x - 0.5) * 50;
//            double dy = (y - 0.5) * -30;
//            Move_Relative_Synchronous((int)dx, (int)dy);
//        }

//        public void Move_Absolute_NavGrid_Synchronous(double x, double y)
//        {
//            if (x < 0)
//                x = 0;
//            else if (x > 1)
//                x = 1;
//            if (y < 0)
//                y = 0;
//            else if (y > 1)
//                y = 1;

//            double dx = (x * (-panLimitLeft + panLimitRight)) + panLimitLeft;
//            double dy = ((1 - y) * (-tiltLimitDown + tiltLimitUp)) + tiltLimitDown;
//            Move_Absolute_Synchronous((int)dx, (int)dy);
//        }

//        public string GetHtml(int refreshTime = 200, int disableRefreshAfter = 600000, bool useAutoResize = false)
//        {
//            StringBuilder sb = new StringBuilder();
//            sb.Append(@"<!DOCTYPE HTML>
//<html>
//<head>
//	<title>BetterPtz (TV-IP672 Series)</title>
//	<script src=""Scripts/jquery.js"" type=""text/javascript""></script>
//<script type=""text/javascript"">
//var disableRefreshAfter = " + disableRefreshAfter + @";
//var refreshDisabled = false;
//var originwidth = 1280;
//var originheight = 800;
//var lastUpdate = 0;
//var allowNavigation = true;
//$(function()
//{
//	if(disableRefreshAfter > -1)
//		setTimeout('disableRefresh()', disableRefreshAfter);
//	myOnLoad();
//});
//
//function myOnLoad()
//{
//	$(""#cameraImageImg"").load(function ()
//	{
//		if (!this.complete || typeof this.naturalWidth == ""undefined"" || this.naturalWidth == 0)
//		{
//			alert('Bad image data was received.  A data transmission error may have occurred, or this camera may be offline.  Please reload this page to try again.');
//		}
//		else if(refreshDisabled)
//		{
//			PopupMessage('This page has been open for ten minutes.  To save resources, the image will no longer refresh automatically.  <a href=""javascript:top.location.reload();"">Click here</a> to reload this page.');
//		}
//		else
//		{
//			lastUpdate = new Date().getTime();
//			setTimeout(""GetNewImage();"", " + refreshTime + @");
//		}
//	});
//	$(""#cameraImageImg"").error(function ()
//	{
//		setTimeout(""GetNewImage();"", " + refreshTime + @");
//	});
//	GetNewImage();
//	$('#cameraImageDiv').click(function(e)
//	{
//		if(!allowNavigation)
//			return;
//		allowNavigation = false;
//		var srcEle = $('#cameraImageDiv');
//		var pos = srcEle.position();
//		var x = (e.pageX - pos.left);
//		var y = (e.pageY - pos.top);
//		x = x / srcEle.width();
//		y = y / srcEle.height();
//		$('#status').html(x + ', ' + y);
//		$.ajax('cmd?cmd=move_rel_pixel&x=' + x + '&y=' + y).done(allowNav).fail(allowNav);
//	});
//	$('#cameraNavGridDiv').click(function(e)
//	{
//		if(!allowNavigation)
//			return;
//		allowNavigation = false;
//		var srcEle = $('#cameraNavGridDiv');
//		var pos = srcEle.position();
//		var offsetX = parseInt(srcEle.attr('thumbwidth')) / 2;
//		var offsetY = parseInt(srcEle.attr('thumbheight')) / 2;
//		var x = (e.pageX - (pos.left + offsetX));
//		var y = (e.pageY - (pos.top + offsetY)) ;
//		x = x / (srcEle.width() - (2 * offsetX));
//		y = y / (srcEle.height() - (2 * offsetY));
//		$('#status').html(x + ', ' + y);
//		$.ajax('cmd?cmd=move_abs_navgrid&x=' + x + '&y=' + y).done(allowNav).fail(allowNav);
//	});
//}
//function allowNav()
//{
//	allowNavigation = true;
//}
//function PopupMessage(msg)
//{
//	var pm = $(""#popupMessage"");
//	if(pm.length < 1)
//		$(""#cameraImageDiv"").after('<div id=""popupFrame""><div id=""popupMessage"">' + msg + '</div><center><input type=""button"" value=""Close Message"" onclick=""CloseMessage()""/></center></div>');
//	else
//		pm.append(msg);
//}
//function CloseMessage()
//{
//	$(""#popupFrame"").remove();
//}
//function GetNewImage()
//{
//	$(""#cameraImageImg"").attr('src', 'image?nocache=' + new Date().getTime());
//}
//function disableRefresh()
//{
//	refreshDisabled = true;
//}
//if(" + (useAutoResize ? "true" : "false") + @")
//{
//	$(window).load(resize);
//	$(window).resize(resize);
//}
//function resize(width, height)
//{
//	var newHeight;
//	var newWidth;
//	var currentImage = document.getElementById(""cameraImageImg"");
//	if (currentImage == null)
//		return;
//	if (typeof (width) == 'undefined' || typeof (height) == 'undefined')
//	{
//		var imgOff = FindOffsets(currentImage);
//		var screenOff = GetViewportDims();
//		// Calculate available dimensions
//		var availableHeight = (screenOff.height - imgOff.top) - 7;
//		var availableWidth = (screenOff.width - imgOff.left) - 21;
//		// Take into consideration the original width and height for the image
//		newHeight = originheight < availableHeight ? originheight : availableHeight;
//		newWidth = originwidth < availableWidth ? originwidth : availableWidth;
//		// Calculate ratios
//		var originRatio = originwidth / originheight;
//		var newRatio = newWidth / newHeight;
//		if (newRatio < originRatio)
//			newHeight = newWidth / originRatio;
//		else
//			newWidth = newHeight * originRatio;
////		currentImage.onclick = function ()
////		{
////			resize(originwidth, originheight);
////		}
//	}
//	else
//	{
//		var newHeight = height;
//		var newWidth = width;
////		currentImage.onclick = function ()
////		{
////			resize();
////		}
//	}
//	$(currentImage).height(newHeight);
//	$(currentImage).width(newWidth);
//	
//	$(""#cameraImageDiv"").width(newWidth);
//	$(""#cameraImageDiv"").height(newHeight);
//}
//function GetViewportDims()
//{
//	var w;
//	var h;
//	if (typeof (window.innerWidth) != 'undefined')
//	{
//		w = window.innerWidth;
//		h = window.innerHeight;
//	}
//	else if (typeof (document.documentElement) != 'undefined' && typeof (document.documentElement.clientWidth) != 'undefined' && document.documentElement.clientWidth != 0)
//	{
//		w = document.documentElement.clientWidth;
//		h = document.documentElement.clientHeight;
//	}
//	else
//	{
//		w = document.getElementsByTagName('body')[0].clientWidth;
//		h = document.getElementsByTagName('body')[0].clientHeight;
//	}
//	return { width: w, height: h };
//}
//function FindOffsets(node)
//{
//	var oLeft = 0;
//	var oTop = 0;
//	var oWidth = node.offsetWidth;
//	var oHeight = node.offsetHeight;
//	if (oWidth == 0)
//		oWidth += node.scrollWidth;
//	oLeft = node.offsetLeft;
//	oTop = node.offsetTop;
//	if (oHeight == 0)
//	{
//		if (node.childNodes.length > 0)
//		{
//			oHeight += node.childNodes[0].offsetHeight;
//			oTop = node.childNodes[0].offsetTop;
//		}
//		if (oHeight == 0)
//			oHeight += node.scrollHeight;
//	}
//	node = node.offsetParent;
//	while (node)
//	{
//		oLeft += node.offsetLeft;
//		oTop += node.offsetTop;
//		node = node.offsetParent;
//	}
//	return { left: oLeft, top: oTop, width: oWidth, height: oHeight };
//}
//</script>
//<style type=""text/css"">
//#cameraImageDiv
//{
//	float: left;
//	border: 1px solid gray;
//	width: 1280px;
//	height: 800px;
//	margin-right: 4px;
//	background-color: black;
//}
//#cameraNavGridDiv
//{
//	float: left;
//	border: 1px dotted gray;
//}
//</style>
//</head>
//<body>
//
//<div id=""status""></div>
//<div>
//
//<div id=""cameraImageDiv"">
//	<img id=""cameraImageImg"" alt=""Live View"" />
//	<!--<div style=""position:absolute; top:403px; left: 643px; width:10px;height:10px;background-color:blue;""></div>
//	<div style=""position:absolute; top:405px; left: 645px; width:6px;height:6px;background-color:red;""></div>
//	<div style=""position:absolute; top:407px; left: 647px; width:2px;height:2px;background-color:green;""></div>-->
//</div>
//
//" + @"
//
//</div>
//</body>
//</html>");
//            return sb.ToString();
//        }

//        internal static string GetHtml(string camId, IPCameraBase cam)
//        {
//            StringBuilder sb = new StringBuilder();
//            #region HTML
//            sb.Append("<td id=\"pthCell\" class=\"nounderline\"><div id=\"pthDiv\"><script type=\"text/javascript\">");
//            sb.Append(Javascript.Longclick());
//            sb.Append("</script><script type=\"text/javascript\">");
//            sb.Append(Javascript.JqueryMousewheel());
//            sb.Append("</script>");
//            sb.Append(@"<style type=""text/css"">
//.presettbl
//{
//	border-collapse: collapse;
//}
//.presettbl td
//{
//	width: 66px;
//	height: 50px;
//	padding: 1px;
//}
//.presettbl img
//{
//	width: 64px;
//	height: 48px;
//	border: 1px Solid #666666;
//	margin-bottom: -7px;
//}
//#pthDiv
//{
//	background-color: #5C5C5C !important;
//}
//.ptztable
//{
//	background-color: #5C5C5C;
//}
//.ptzButtonCell
//{
//	width: 118px;
//}
//.ptzbuttons, .zfi
//{
//	width: 111px;
//	height: 110px;
//	padding: 5px 2px 2px 5px;
//}
//.zfi
//{
//	padding: 5px 5px 2px 5px;
//	color: White;
//	font-size: 10pt;
//	font-family: Verdana, Arial, Sans-Serif;
//}
//.ptzarrow
//{
//	background: url(""../Images/ptzarrows.png"") no-repeat scroll 0 0 transparent;
//}
//.ptzbutton
//{
//	margin: 0 3px 3px 0;
//	height: 34px;
//	width: 34px;
//	display: block;
//	float: left;
//}
//#yt5
//{
//	background: url(""../Images/ptzclick.png"") no-repeat scroll 0 0 transparent;
//}
//#yt1 { background-position: 0 0; }
//#yt2 { background-position: 0 -31px; }
//#yt3 { background-position: 0 -62px; }
//#yt4 { background-position: 0 -93px; }
//#yt5 { background-position: 0 0; }
//#yt6 { background-position: 0 -124px; }
//#yt7 { background-position: 0 -155px; }
//#yt8 { background-position: 0 -186px; }
//#yt9 { background-position: 0 -217px; }
//#yt1:hover { background-position: -34px 0; }
//#yt2:hover { background-position: -34px -31px; }
//#yt3:hover { background-position: -34px -62px; }
//#yt4:hover { background-position: -34px -93px; }
//#yt5:hover, #yt5.enabled { background-position: -34px 0; }
//#yt6:hover { background-position: -34px -124px; }
//#yt7:hover { background-position: -34px -155px; }
//#yt8:hover { background-position: -34px -186px; }
//#yt9:hover { background-position: -34px -217px; }
//
//.label
//{
//	float: left;
//	height: 28px;
//	line-height: 28px;
//	margin: 3px 0;
//	text-align: center;
//	text-decoration: none;
//	width: 55px;
//}
//.increase, .decrease
//{
//	background: url(""../Images/increase.png"") no-repeat scroll 0 0 transparent;
//	margin: 3px 0;
//	height: 28px;
//	width: 28px;
//	display: block;
//	float: left;
//}
//.decrease
//{
//	background-position: 0 -28px;
//}
//.increase:hover { background-position: -28px 0; }
//.decrease:hover { background-position: -28px -28px; }
//
//#joystickDiv
//{
//	width: 100px;
//	height: 100px;
//	cursor: crosshair;
//}
//.joystickBox50
//{
//	width: 48px;
//	height: 48px;
//	border: 1px solid black;
//	float: left;
//}
//#gridDiv
//{
//	width: 672px;
//	height: 216px;
//	line-height: 0px;
//}
//#gridTbl
//{
//	border-collapse: collapse;
//}
//.gridcell
//{
//	padding: 0px;
//	width: 96px;
//	height: 54px;
//	line-height: 0px;
//}
//.gridImg
//{
//	width: 96px;
//	height: 54px;
//}
//#thumbnailBox
//{
//	background-color: transparent;
//	border: 1px solid Red;
//	position: relative;
//	margin-top: -216px;
//}
//#thumbnailBoxInner
//{
//	background-color: transparent;
//	border: 1px solid Blue;
//	position: relative;
//	display: none;
//}
//</style>
//<script type=""text/javascript"">
//$(function()
//{
//	$('#imgFrame').click(handleTargetClick);
//	$('#zoomControls').mousewheel(function(e, delta, deltaX, deltaY)
//	{
////		if(ThumbMouseDown)
////		{
//			e.preventDefault();
//			if(deltaY < 0)
//				ThumbZoom--;
//			else if(deltaY > 0)
//				ThumbZoom++;
////			if(ThumbZoom < 0)
////				ThumbZoom = 0;
////			else if(ThumbZoom > 16)
////				ThumbZoom = 16;
//			//UpdateThumbnailBox(e);
//			UpdateZoom();
////		}
//	});
//	InitializeGrid();
////	for(var i = 1; i <= 8; i++)
////	{
////		$('#preset' + i).longAndShortClick(
////		function(ele)
////		{
////			var mynum = parseInt(ele.getAttribute('mynum'));
////			if(confirm('Are you sure you want to assign preset ' + mynum + '?'))
////				$.get('PTZ?id=" + camId + @"&cmd=' + (28 + (mynum * 2))).done(function()
////				{
////					$('#preset' + mynum).attr('src', 'PTZPRESETIMG?id=" + camId + @"&index=' + mynum + '&nocache=' + new Date().getTime());
////				});
////		},
////		function(ele)
////		{
////			var mynum = parseInt(ele.getAttribute('mynum'));
////			$.get('PTZ?id=" + camId + @"&cmd=' + (29 + (mynum * 2)));
////		});
////	}
////	// Set up virtual joystick
////	var leftButtonDown = false;
////	var isMovingSpeed = false;
////	var joystickCommandDelayMs = 100;
////	var nextJoystickCommand = 0;
////	$(document).mousedown(function(e)
////	{
////		if(isMovingSpeed)
////		{
////			isMovingSpeed = false;
////			StopPTZ();
////		}
////		// Left mouse button was pressed, set flag
////		if(e.which === 1)
////			leftButtonDown = true;
////		return true;
////	});
////	$(document).mouseup(function(e)
////	{
////		if(isMovingSpeed)
////		{
////			isMovingSpeed = false;
////			StopPTZ();
////		}
////		// Left mouse button was released, clear flag
////		if(e.which === 1)
////			leftButtonDown = false;
////		return true;
////	});
////	$('#joystickDiv').mousemove(function(e)
////	{
////		if(leftButtonDown && nextJoystickCommand < new Date().getTime())
////		{
////			nextJoystickCommand = new Date().getTime() + joystickCommandDelayMs;
////			var offsets = $('#joystickDiv').offset();
////			var clickX = e.pageX - offsets.left;
////			var clickY = e.pageY - offsets.top;
////			var percentX = clickX / $('#joystickDiv').width();
////			var percentY = clickY / $('#joystickDiv').height();
////			isMovingSpeed = true;
////			MoveSpeed(percentX, percentY);
////		}
////	});
//});
//var ThumbX = -10000;
//var ThumbY = -10000;
//var ThumbZoom = 0;
//var ThumbMouseDown = false;
//var lastThumbX = -10000;
//var lastThumbY = -10000;
//var lastThumbZ = 0;
//var ThumbBoxW = 96;
//var ThumbBoxH = 54;
//var ThumbBoxInnerMaxW = 94;
//var ThumbBoxInnerMaxH = 52;
//var ThumbBoxInnerMinW = 4;
//var ThumbBoxInnerMinH = 2.25;
//var ThumbBoxInnerStepW = ((ThumbBoxInnerMaxW - ThumbBoxInnerMinW) / 16.0);
//var ThumbBoxInnerStepH = ((ThumbBoxInnerMaxH - ThumbBoxInnerMinH) / 16.0);
//function InitializeGrid()
//{
//	$('#gridDiv').mousedown(function(e)
//	{
//		if(e.which == 1)
//		{
//			if(ThumbX == -10000)
//				CreateThumbnailBox();
//			e.preventDefault();
//			ThumbMouseDown = true;
//			UpdateThumbnailBox(e);
//		}
//	});
//	$('#gridDiv').mousemove(function(e)
//	{
//		UpdateThumbnailBox(e);
//	});
//	$(document).mouseup(function(e)
//	{
//		ThumbMouseDown = false;
//		
//		clearTimeout(moveThumbnailBoxTimeout);
//		moveThumbnailBoxTimeout = setTimeout(function()
//		{
//			ExecuteThumbnailMove();
//		}, delayBetweenThumbnailBoxMove + 100);
//	});
//}
//function UpdateThumbnailBox(e)
//{
//	if(!ThumbMouseDown)
//		return;
//	var gridDiv = $('#gridDiv');
//	var offsets = gridDiv.offset();
//	var clickX = e.pageX - offsets.left;
//	var clickY = e.pageY - offsets.top;
//
//	var thumbnailBox = $('#thumbnailBox');
//	var tw = thumbnailBox.width();
//	var th = thumbnailBox.height();
//	ThumbX = (clickX - (tw / 2)) / gridDiv.width();
//	ThumbY = (clickY - (th / 2)) / (gridDiv.height() - th);
//
//	thumbnailBox.css('left', (clickX - (tw / 2)) + 'px');
//	thumbnailBox.css('top', (clickY - (th / 2)) + 'px');
//
//	var thumbnailBoxInner = $('#thumbnailBoxInner');
//	var tiw = (ThumbBoxInnerStepW * (16 - ThumbZoom)) + ThumbBoxInnerMinW;
//	var tih = (ThumbBoxInnerStepH * (16 - ThumbZoom)) + ThumbBoxInnerMinH;
//	thumbnailBoxInner.css('width', tiw + 'px');
//	thumbnailBoxInner.css('height', tih + 'px');
//	thumbnailBoxInner.css('left', ((ThumbBoxInnerMaxW - tiw) / 2) + 'px');
//	thumbnailBoxInner.css('top', ((ThumbBoxInnerMaxH - tih) / 2) + 'px');
//
//	MoveCameraToThumbnailBoxPosition();
//}
//var nextThumbnailBoxMove = 0;
//var delayBetweenThumbnailBoxMove = 500;
//var nextZoomAdjust = 0;
//var zoomChangeDelay = 5000;
//var moveThumbnailBoxTimeout = null;
//function MoveCameraToThumbnailBoxPosition()
//{
//	var time = new Date().getTime();
//	if(time < nextThumbnailBoxMove)
//	{
//		clearTimeout(moveThumbnailBoxTimeout);
//		moveThumbnailBoxTimeout = setTimeout(function()
//		{
//			ExecuteThumbnailMove();
//		}, delayBetweenThumbnailBoxMove + 100);
//		return;
//	}
//	clearTimeout(moveThumbnailBoxTimeout);
//	nextThumbnailBoxMove = time + delayBetweenThumbnailBoxMove;
//	ExecuteThumbnailMove();
//}
//function ExecuteThumbnailMove()
//{
////	var useZoom = lastThumbZ;
////	if(new Date().getTime() > nextZoomAdjust && ThumbZoom != lastThumbZ)
////	{
////		useZoom = ThumbZoom;
////		nextZoomAdjust = new Date().getTime() + zoomChangeDelay;
////	}
//	if(ThumbX != lastThumbX || ThumbY != lastThumbY /*|| useZoom != lastThumbZ*/)
//	{
//		lastThumbX = ThumbX;
//		lastThumbY = ThumbY;
//		//lastThumbZ = useZoom;
//		ExecuteCommand('percent/' + ThumbX + '/' + ThumbY + '/' + 0);
//	}
//}
//function CreateThumbnailBox()
//{
//	$('#gridDiv').append('<div id=""thumbnailBox"" style=""width:" + cam.dahuaPtz.thumbnailBoxWidth + @"px;height:" + cam.dahuaPtz.thumbnailBoxHeight + @"px;""><div id=""thumbnailBoxInner"" style=""width:" + (cam.dahuaPtz.thumbnailBoxWidth - 2) + @"px;height:" + (cam.dahuaPtz.thumbnailBoxHeight - 2) + @"px;""></div></div>');
//	$('#thumbnailBox').css('margin-top', '-' + $('#gridDiv').height() + 'px');
//	$('#thumbnailBox').mousemove(function(e)
//	{
//		UpdateThumbnailBox(e);
//	});
//	$('#thumbnailBox').mousedown(function(e)
//	{
//		if(e.which == 1)
//		{
//			ThumbMouseDown = true;
//			e.preventDefault();
//			UpdateThumbnailBox(e);
//		}
//	});
//}
//var lastZoom = 0;
//var nextZoomTime = 0;
//var zoomTimeDelay = 250;
//function UpdateZoom()
//{
//	if(lastZoom != ThumbZoom && new Date().getTime() > nextZoomTime)
//	{
//		nextZoomTime = new Date().getTime() + zoomTimeDelay;
//		var zoomAmount = (ThumbZoom - lastZoom) * 4;
//		lastZoom = ThumbZoom;
//		Zoom(zoomAmount);
//	}
//}
//function PTZDirection(buttonIndex)
//{
//	ExecuteCommand('move_simple/' + buttonIndex);
//}
//function Zoom(amount)
//{
//	ExecuteCommand('zoom/' + amount);
//}
//function Focus(amount)
//{
//	//ExecuteCommand('focus/' + amount);
//}
//function Iris(amount)
//{
//	ExecuteCommand('iris/' + amount);
//}
//function MoveSpeed(xPercent, yPercent)
//{
//	ExecuteCommand('m/' + xPercent + '/' + yPercent);
//}
//function StopPTZ()
//{
//	ExecuteCommand('stopall');
//}
//function ExecuteCommand(cmd)
//{
//	if(!refreshDisabled)
//		$.get('PTZ?id=" + camId + @"&cmd=' + cmd);
//}
//// Click to center //
//var clickToCenterEnabled = false;
//function ToggleClickToCenter()
//{
//	clickToCenterEnabled = !clickToCenterEnabled;
//	clickCausesResize = !clickToCenterEnabled;
//	if(clickToCenterEnabled)
//		$('#yt5').addClass('enabled');
//	else
//		$('#yt5').removeClass('enabled');
//}
//function handleTargetClick(e)
//{
//	if(clickToCenterEnabled)
//	{
//		var offsets = $('#imgFrame').offset();
//		var clickX = e.pageX - offsets.left;
//		var clickY = e.pageY - offsets.top;
//		var percentX = clickX / $('#imgFrame').width();
//		var percentY = clickY / $('#imgFrame').height();
//		ExecuteCommand('frame/' + percentX + '/' + percentY);
//	}
//}
//function fullPanoramaImageLoaded()
//{
//	var fullPanoramaImage = document.getElementById('fullPanoramaImage');
//	var w = parseInt(fullPanoramaImage.naturalWidth / 2);
//	var h = parseInt(fullPanoramaImage.naturalHeight / 2);
//	$('#gridDiv').css('width', w + 'px');
//	$('#gridDiv').css('height', h + 'px');
//	$(fullPanoramaImage).css('width', w + 'px');
//	$(fullPanoramaImage).css('height', h + 'px');
//	$('#thumbnailBox').css('margin-top', '-' + h + 'px');
//}
///////////////////////
//</script>");
//            #endregion
//            sb.Append("<table style=\"width: 100%;\" class=\"ptztable\"><tbody><tr>");

//            // Write PTZ arrows and "click-to-move" button
//            sb.Append("<td class=\"ptzButtonCell\"><div class=\"ptzbuttons\">");
//            for (int i = 1; i <= 9; i++)
//            {
//                sb.Append("<a href=\"javascript:");
//                if (i != 5)
//                    sb.Append("PTZDirection(" + i + ")");
//                else
//                    sb.Append("ToggleClickToCenter()");
//                sb.Append("\" id=\"yt" + i + "\" class=\"ptzbutton");
//                if (i != 5)
//                    sb.Append(" ptzarrow");
//                sb.Append("\"></a>");
//            }
//            sb.Append("</div></td>");

//            // Write Zoom, Focus, and Iris buttons
//            sb.Append("<td class=\"ptzButtonCell\"><div class=\"zfi\">");
//            sb.Append("<div id=\"zoomControls\"><a href=\"javascript:Zoom(10)\" class=\"increase\"></a><span class=\"label\"> Zoom </span><a href=\"javascript:Zoom(-10)\" class=\"decrease\"></a></div>");
//            sb.Append("<a href=\"javascript:Focus(1000)\" class=\"increase\"></a><span class=\"label\"> Focus </span><a href=\"javascript:Focus(-1000)\" class=\"decrease\"></a>");
//            sb.Append("<a href=\"javascript:Iris(5)\" class=\"increase\"></a><span class=\"label\"> Iris </span><a href=\"javascript:Iris(-5)\" class=\"decrease\"></a>");
//            sb.Append("</div></td>");

//            //sb.Append("<td class=\"joystickCell\"><div id=\"joystickDiv\"><div class=\"joystickBox50\"></div><div class=\"joystickBox50\"></div>");
//            //sb.Append("<div class=\"joystickBox50\"></div><div class=\"joystickBox50\"></div></div></td>");

//            if (cam.cameraSpec.ptz_panorama_simple)
//            {
//                // Write pseudo-panorama image grid
//                sb.Append("<td><div id=\"gridDiv\"><table id=\"gridTbl\"><tbody>");
//                for (int j = 0; j < 4; j++)
//                {
//                    sb.Append("<tr>");
//                    for (int i = 0; i < 7; i++)
//                    {
//                        sb.Append("<td class=\"gridcell\">");
//                        sb.Append(GetPresetControl(camId, i + (j * 7)));
//                        sb.Append("</td>");
//                    }
//                    sb.Append("</tr>");
//                }
//                sb.Append("</tbody></table></div></td>");
//            }
//            else
//                sb.Append("<td><div id=\"gridDiv\"><img id=\"fullPanoramaImage\" src=\"PTZPRESETIMG?id=" + camId + "&index=99999&nocache=" + DateTime.Now.Ticks + "\" onload=\"fullPanoramaImageLoaded(); return false;\"></div></td>");


//            sb.Append("</tr></tbody></table>");

//            // Close wrapping container
//            sb.Append("</div></td></tr><tr>");
//            return sb.ToString();
//        }

//        internal static void RunCommand(IPCameraBase cam, string cameraId, string cmd)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

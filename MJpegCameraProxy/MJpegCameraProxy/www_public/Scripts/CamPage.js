var refreshDisabled = false;
var resizeTimeout = null;
var zoomHintTimeout = null;
var digitalZoom = 0;
var zoomTable = [0, 1, 1.2, 1.4, 1.6, 1.8, 2, 2.5, 3, 3.5, 4, 4.5, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20, 23, 26, 30, 35, 40, 45, 50];
var imageIsDragging = false;
var menuDraggingHorizontal = false;
var menuDraggingVertical = false;
var imageIsLargerThanAvailableSpace = false;
var mouseX = 0;
var mouseY = 0;
var imgDigitalZoomOffsetX = 0;
var imgDigitalZoomOffsetY = 0;
var previousImageDraw = new Object();
previousImageDraw.x = -1;
previousImageDraw.y = -1;
previousImageDraw.w = -1;
previousImageDraw.h = -1;
previousImageDraw.z = 10;

var panoramaOriginWidth = 16;
var panoramaOriginHeight = 16;

var rightMenuDesiredWidth = 42;
var bottomMenuHeightPercent = 0.3333333;
var menuResizeOffsetX = 0;
var menuResizeOffsetY = 0;

var sliderHandleApparentHeight = 16;
var zoomSliderHandle;

var panoDragging = false;
var panoPercentX = 0.5;
var panoPercentY = 0.5;

var serverPanoPercentX = 0.5;
var serverPanoPercentY = 0.5;
var serverPanoPercentZ = 0.5;

var enabled3dPositioning = false;
var pos3dX = 0;
var pos3dY = 0;
var pos3dDragging = false;

var pos3dNew = null;

var is_pinching = false;
var pinchTimeout = null;
var pinchScale = 1;

$(function ()
{
	zoomSliderHandle = $("#zoomslider .sliderhandle").get(0);
	zoomSliderHandle.percent = 0;

	if (disableRefreshAfter > -1)
		setTimeout("disableRefresh()", disableRefreshAfter);

	$("#imgFrame").load(function ()
	{
		/*if (typeof this.naturalWidth == "undefined" || this.naturalWidth == 0)
		{
		alert('Bad image data was received.  A data transmission error may have occurred, or this camera may be offline.  Please reload this page to try again.');
		}
		else */
		if (!refreshDisabled && refreshDelay >= 0)
		{
			setTimeout("GetNewImage();", refreshDelay);
		}
	});
	$("#imgFrame").error(function ()
	{
		setTimeout("GetNewImage();", refreshDelay);
	});

	$("#panoramaimg").load(function ()
	{
		//		if (typeof this.naturalWidth != "undefined" && this.naturalWidth != 0)
		//		{
		panoramaOriginWidth = this.naturalWidth;
		panoramaOriginHeight = this.naturalHeight;
		resize(false);
		//		}
	});
	$("#panoramaimg").error(function ()
	{
		$("#panoramaimg").hide();
	});
	$("#panoramaimg").attr("src", "image/PTZPRESETIMG?id=" + cameraId + "&index=99999&nocache=" + new Date().getTime());
});
$(window).load(function ()
{
	GetNewImage();
});
function PopupMessage(msg)
{
	var pm = $("#popupMessage");
	if (pm.length < 1)
		$("#outerFrame").after('<div id="popupFrame"><div id="popupMessage">' + msg + '</div><center><input type="button" value="Close Message" onclick="CloseMessage()"/></center></div>');
	else
		pm.append("<br/>" + msg);
}
function CloseMessage()
{
	$("#popupFrame").remove();
}
function GetNewImage()
{
	if (is_pinching)
		setTimeout(GetNewImage, 50);
	else
		$("#imgFrame").attr('src', 'image/' + cameraId + '.' + (refreshDelay == -1 ? 'm' : '') + 'jpg?patience=5000&nocache=' + new Date().getTime());
}
function disableRefresh()
{
	refreshDisabled = true;
	refreshDelay = 250;
	GetNewImage();
	CloseSocket();
	PopupMessage('This page has been open for ' + disableRefreshAfterMinutes + ' minutes.  To save resources, the image will no longer refresh automatically.  <a href="javascript:top.location.reload();">Click here</a> to reload this page.');
}
$(window).load(doResize);
$(window).resize(doResize);
function doResize()
{
	resize(false);
}
function resize(wasCausedByTrigger)
{
	var windowW = $(window).width(), windowH = $(window).height();
	$("#outerFrame").css("width", windowW + "px");
	$("#outerFrame").css("height", windowH + "px");

	if (bottomMenuHeightPercent <= 0) bottomMenuHeightPercent = 0;
	if (bottomMenuHeightPercent > 0.8) bottomMenuHeightPercent = 0.8;

	var panoAvailableW = windowW;
	var panoAvailableH = windowH * bottomMenuHeightPercent;
	var panoW = Math.min(panoramaOriginWidth, panoAvailableW);
	var panoH = Math.min(panoramaOriginHeight, panoAvailableH);

	var originRatio = panoramaOriginWidth / panoramaOriginHeight;
	var newRatio = panoW / panoH;
	if (newRatio < originRatio)
		panoH = panoW / originRatio;
	else
		panoW = panoH * originRatio;

	var panoImg = $("#panoramaimg");
	panoImg.css("left", ((panoAvailableW - panoW) / 2) + "px");
	panoImg.css("width", panoW + "px");
	panoImg.css("height", panoH + "px");
	$("#bottomMenu").css("height", panoH + "px");

	if (rightMenuDesiredWidth > windowW - 100) rightMenuDesiredWidth = windowW - 100;
	if (rightMenuDesiredWidth < 0) rightMenuDesiredWidth = 0;

	$("#rightMenu").css("width", rightMenuDesiredWidth + "px");

	var rightMenuWidth = $("#rightMenu").outerWidth(true);
	var bottomMenuHeight = $("#bottomMenu").outerHeight(true);
	var camCellWidth = (windowW - rightMenuWidth);
	var camCellHeight = (windowH - bottomMenuHeight);

	$("#camCell").css("width", camCellWidth + "px");
	$("#camCell").css("height", camCellHeight + "px");

	$("#rightMenu").css("left", camCellWidth + "px");
	$("#rightMenu").css("height", camCellHeight + "px");
	$("#bottomMenu").css("top", camCellHeight + "px");

	var zoomSlider = $("#zoomslider");
	var zoomSliderTrack = $("#zoomslider .slidertrack");
	zoomSliderTrack.css("height", Math.max(1, Math.min(200, camCellHeight - (zoomSlider.outerHeight(true) - zoomSliderTrack.height()) - $("#zoomlabel").height() - $("#btn3dPos").height())) + "px");

	ImgResized();

	PanoThumbDraw();

	//	if (!wasCausedByTrigger)
	//	{
	//		if (resizeTimeout != null)
	//			clearTimeout(resizeTimeout);
	//		resizeTimeout = setTimeout(function () { resize(true) }, 100);
	//	}
}
function ImgResized()
{
	var imgAvailableWidth = $("#camCell").width();
	var imgAvailableHeight = $("#camCell").height();

	// Calculate new size based on zoom levels
	var imgDrawWidth = originwidth * (zoomTable[digitalZoom]);
	var imgDrawHeight = originheight * (zoomTable[digitalZoom]);
	if (imgDrawWidth == 0)
	{
		imgDrawWidth = imgAvailableWidth;
		imgDrawHeight = imgAvailableHeight;

		var originRatio = originwidth / originheight;
		var newRatio = imgDrawWidth / imgDrawHeight;
		if (newRatio < originRatio)
			imgDrawHeight = imgDrawWidth / originRatio;
		else
			imgDrawWidth = imgDrawHeight * originRatio;
	}
	$("#imgFrame").css("width", imgDrawWidth + "px");
	$("#imgFrame").css("height", imgDrawHeight + "px");

	imageIsLargerThanAvailableSpace = imgDrawWidth > imgAvailableWidth || imgDrawHeight > imgAvailableHeight;

	if (previousImageDraw.z > -1 && previousImageDraw.z != digitalZoom)
	{
		// We just experienced a zoom change
		// Find the mouse position percentage relative to the center of the image at its old size
		var imgPos = $("#imgFrame").position();
		var mouseRelX = -0.5 + (parseFloat(mouseX - imgPos.left) / previousImageDraw.w);
		var mouseRelY = -0.5 + (parseFloat(mouseY - imgPos.top) / previousImageDraw.h);
		// Get the difference in image size
		var imgSizeDiffX = imgDrawWidth - previousImageDraw.w;
		var imgSizeDiffY = imgDrawHeight - previousImageDraw.h;
		// Modify the zoom offsets by % of difference
		imgDigitalZoomOffsetX -= mouseRelX * imgSizeDiffX;
		imgDigitalZoomOffsetY -= mouseRelY * imgSizeDiffY;
	}

	// Enforce digital panning limits
	var maxOffsetX = (imgDrawWidth - imgAvailableWidth) / 2;
	if (maxOffsetX < 0)
		imgDigitalZoomOffsetX = 0;
	else if (imgDigitalZoomOffsetX > maxOffsetX)
		imgDigitalZoomOffsetX = maxOffsetX;
	else if (imgDigitalZoomOffsetX < -maxOffsetX)
		imgDigitalZoomOffsetX = -maxOffsetX;

	var maxOffsetY = (imgDrawHeight - imgAvailableHeight) / 2;
	if (maxOffsetY < 0)
		imgDigitalZoomOffsetY = 0;
	else if (imgDigitalZoomOffsetY > maxOffsetY)
		imgDigitalZoomOffsetY = maxOffsetY;
	else if (imgDigitalZoomOffsetY < -maxOffsetY)
		imgDigitalZoomOffsetY = -maxOffsetY;

	// Calculate new image position
	var proposedX = (((imgAvailableWidth - imgDrawWidth) / 2) + imgDigitalZoomOffsetX);
	var proposedY = (((imgAvailableHeight - imgDrawHeight) / 2) + imgDigitalZoomOffsetY);

	$("#imgFrame").css("left", proposedX + "px");
	$("#imgFrame").css("top", proposedY + "px");

	// Store new image position for future calculations
	previousImageDraw.x = proposedX;
	previousImageDraw.x = proposedY;
	previousImageDraw.w = imgDrawWidth;
	previousImageDraw.h = imgDrawHeight;
	previousImageDraw.z = digitalZoom;
}
function PanoThumbDraw()
{
	_PanoThumbDraw("#thumbnailbox", "#thumbnailboxinner", panoPercentX, panoPercentY, zoomSliderHandle.percent);
	_PanoThumbDraw("#thumbnailboxServer", "#thumbnailboxinnerServer", serverPanoPercentX, serverPanoPercentY, serverPanoPercentZ);
}
function _PanoThumbDraw(tbSelector, tbInnerSelector, percentX, percentY, percentZ)
{
	var pano = $("#panoramaimg");
	var panoOffset = pano.offset();
	var panoW = pano.width();
	var panoH = pano.height();

	var thumbPercentX = percentX - (thumbnailBoxPercentWidth / 2);
	var thumbPercentY = percentY - (thumbnailBoxPercentHeight / 2);
	var thumbX = (thumbPercentX * panoW) + panoOffset.left;
	var thumbY = (thumbPercentY * panoH) + panoOffset.top;
	var thumbW = thumbnailBoxPercentWidth * panoW;
	var thumbH = thumbnailBoxPercentHeight * panoH;

	var thumbnailbox = $(tbSelector);

	thumbnailbox.css("left", thumbX + "px");
	thumbnailbox.css("top", thumbY + "px");
	thumbnailbox.css("width", thumbW + "px");
	thumbnailbox.css("height", thumbH + "px");

	var thumbInnerMaxW = thumbW - 2;
	var thumbInnerMaxH = thumbH - 2;
	var thumbInnerMinW = thumbW * (1 / zoomMagnification);
	var thumbInnerMinH = thumbH * (1 / zoomMagnification);
	var thumbInnerRangeW = thumbInnerMaxW - thumbInnerMinW;
	var thumbInnerRangeH = thumbInnerMaxH - thumbInnerMinH;

	var inverseZoom = 1.0 - Math.sqrt(percentZ);

	var thumbInnerW = (inverseZoom * thumbInnerRangeW) + thumbInnerMinW;
	var thumbInnerH = (inverseZoom * thumbInnerRangeH) + thumbInnerMinH;
	var thumbInnerX = ((thumbInnerMaxW - thumbInnerW) / 2);
	var thumbInnerY = ((thumbInnerMaxH - thumbInnerH) / 2);

	var thumbnailboxinner = $(tbInnerSelector);
	thumbnailboxinner.css("left", thumbInnerX + "px");
	thumbnailboxinner.css("top", thumbInnerY + "px");
	thumbnailboxinner.css("width", thumbInnerW + "px");
	thumbnailboxinner.css("height", thumbInnerH + "px");
}
function showClientside3dPositioningBox(x, y, w, h)
{
	var box = $("#client3dposbox");

	var blueBox = false;
	if (w < 0)
	{
		x += w;
		w *= -1;
		blueBox = true;
	}
	if (h < 0)
	{
		y += h;
		h *= -1;
		blueBox = true;
	}
	box.css("border-color", blueBox ? "Blue" : "Red");
	box.css("left", (x - 3) + "px");
	box.css("top", (y - 3) + "px");
	box.css("width", (w) + "px");
	box.css("height", (h) + "px");
	box.show();
}
function hideClientside3dPositioningBox()
{
	$("#client3dposbox").hide();
}
function showServerside3dPositioningBox(x, y, w, h, zoomIn)
{
	var imgFrame = $("#imgFrame");
	var imgFrameOffset = imgFrame.offset();
	var imgFrameW = imgFrame.width();
	var imgFrameH = imgFrame.height();

	var boxCenterX = imgFrameW * x;
	var boxCenterY = imgFrameH * y;
	var boxW = imgFrameW * w;
	var boxH = imgFrameH * h;
	var boxX = boxCenterX - (boxW / 2) + imgFrameOffset.left;
	var boxY = boxCenterY - (boxH / 2) + imgFrameOffset.top;

	var box = $("#server3dposbox");

	box.stop(true, true);
	box.css("border-color", zoomIn ? "#FF0000" : "#0000FF");
	box.css("left", (boxX - 3) + "px");
	box.css("top", (boxY - 3) + "px");
	box.css("width", (boxW) + "px");
	box.css("height", (boxH) + "px");
	box.show();
	box.fadeOut(1000);
}
function EndPos3dDragging(mx, my)
{
	if (pos3dDragging)
	{
		if (pinchScale > 1)
		{
			pos3dDragging = false;
			if (!refreshDisabled)
				PopupMessage("Un-zoom the image area first");
			return;
		}
		var box = $("#client3dposbox");
		var imgFrame = $("#imgFrame");
		var imgFrameOffset = imgFrame.offset();
		var imgFrameW = imgFrame.width();
		var imgFrameH = imgFrame.height();

		var x = pos3dX - imgFrameOffset.left;
		var y = pos3dY - imgFrameOffset.top;
		var w = mx - pos3dX;
		var h = my - pos3dY;

		var zoomIn = true;
		if (w < 0)
		{
			x += w;
			w *= -1;
			zoomIn = false;
		}
		if (h < 0)
		{
			y += h;
			h *= -1;
			zoomIn = false;
		}

		pos3dNew = new Object();
		pos3dNew.x = (x + (w / 2)) / imgFrameW;
		pos3dNew.y = (y + (h / 2)) / imgFrameH;
		pos3dNew.w = w / imgFrameW;
		pos3dNew.h = h / imgFrameH;
		pos3dNew.zoomIn = zoomIn;

		pos3dDragging = false;
		hideClientside3dPositioningBox();
	}
}
function SetCamCellCursor()
{
	var innerObjs = $('#imgFrame,#zoomhint,#client3dposbox,#server3dposbox');
	var outerObjs = $('#camCell,#imgFrame,#zoomhint,#client3dposbox,#server3dposbox');
	if (imageIsLargerThanAvailableSpace)
	{
		if (imageIsDragging)
		{
			outerObjs.removeClass("grabcursor");
			outerObjs.addClass("grabbingcursor");
		}
		else
		{
			outerObjs.removeClass("grabbingcursor");
			outerObjs.addClass("grabcursor");
		}
	}
	else if (enabled3dPositioning)
	{
		outerObjs.removeClass("grabcursor");
		outerObjs.removeClass("grabbingcursor");
		innerObjs.css("cursor", "crosshair");
	}
	else
	{
		outerObjs.removeClass("grabcursor");
		outerObjs.removeClass("grabbingcursor");
		innerObjs.css("cursor", "default");
	}
}
$(function ()
{
	hammerIt(document.getElementById("camCell"));

	$('#camCell').mousewheel(function (e, delta, deltaX, deltaY)
	{
		e.preventDefault();
		if (pos3dDragging)
			return;
		if (deltaY < 0)
			digitalZoom -= 1;
		else if (deltaY > 0)
			digitalZoom += 1;
		if (digitalZoom < 0)
			digitalZoom = 0;
		else if (digitalZoom >= zoomTable.length)
			digitalZoom = zoomTable.length - 1;

		$("#zoomhint").stop(true, true);
		$("#zoomhint").show();
		$("#zoomhint").html(digitalZoom == 0 ? "Fit" : (zoomTable[digitalZoom] + "x"))
		RepositionZoomHint();
		if (zoomHintTimeout != null)
			clearTimeout(zoomHintTimeout);
		zoomHintTimeout = setTimeout(function () { $("#zoomhint").fadeOut() }, 200);

		ImgResized();

		SetCamCellCursor();
	});
	$('#camCell,#zoomhint,#client3dposbox,#server3dposbox').mousedown(function (e)
	{
		mouseX = e.pageX;
		mouseY = e.pageY;
		if (enabled3dPositioning && !imageIsLargerThanAvailableSpace)
		{
			pos3dX = mouseX;
			pos3dY = mouseY;
			pos3dDragging = true;
		}
		imageIsDragging = true;
		SetCamCellCursor();
		e.preventDefault();
	});
	$(document).mouseup(function (e)
	{
		menuDraggingHorizontal = menuDraggingVertical = zoomSliderHandle.isDragging = panoDragging = false;
		$(zoomSliderHandle).attr("src", GetButtonSrc(4, 0, MouseInsideObject($(zoomSliderHandle)) ? 2 : 1));

		imageIsDragging = false;
		EndPos3dDragging(e.pageX, e.pageY);
		SetCamCellCursor();

		mouseX = e.pageX;
		mouseY = e.pageY;
	});
	$('#camCell').mouseleave(function (e)
	{
		var ofst = $("#camCell").offset();
		if (e.pageX < ofst.left || e.pageY < ofst.top || e.pageX >= ofst.left + $("#camCell").width() || e.pageY >= ofst.top + $("#camCell").height())
		{
			imageIsDragging = false;
			EndPos3dDragging(e.pageX, e.pageY);
			SetCamCellCursor();
		}
		mouseX = e.pageX;
		mouseY = e.pageY;
	});
	$(document).mouseleave(function (e)
	{
		EndPos3dDragging(e.pageX, e.pageY);
		menuDraggingHorizontal = menuDraggingVertical = zoomSliderHandle.isDragging = panoDragging = imageIsDragging = false;
		SetCamCellCursor();
		$(zoomSliderHandle).attr("src", GetButtonSrc(4, 0, MouseInsideObject($(zoomSliderHandle)) ? 2 : 1));
	});
	$(document).mousemove(function (e)
	{
		var requiresImgResize = false;
		var requiresPanoThumbRedraw = false;
		if (imageIsDragging && imageIsLargerThanAvailableSpace && !pos3dDragging)
		{
			imgDigitalZoomOffsetX += (e.pageX - mouseX);
			imgDigitalZoomOffsetY += (e.pageY - mouseY);
			requiresImgResize = true;
		}
		if (pos3dDragging)
			showClientside3dPositioningBox(pos3dX, pos3dY, e.pageX - pos3dX, e.pageY - pos3dY);
		if (panoDragging)
		{
			var ofst = $("#panoramaimg").offset();
			panoPercentX = (e.pageX - ofst.left) / $("#panoramaimg").width();
			panoPercentY = (e.pageY - ofst.top) / $("#panoramaimg").height();
			requiresPanoThumbRedraw = true;
		}
		CornerMenuResize(e);

		mouseX = e.pageX;
		mouseY = e.pageY;

		HandleSliderMouseMove(zoomSliderHandle, e.pageY);

		if (requiresImgResize)
			ImgResized();
		if (requiresPanoThumbRedraw)
			PanoThumbDraw();

		if ($("#zoomhint").is(":visible"))
			RepositionZoomHint();
	});

	//////////////////// Corner Menu Resize Button //////////////////////////
	$("#cornerMenuResizeButton").mousemove(function (e)
	{
		if (menuDraggingVertical || menuDraggingHorizontal)
			return;
		var ofst = $("#cornerMenuResizeButton").offset();
		var h = false, v = false;
		if (e.pageX >= ofst.left + ($("#cornerMenuResizeButton").width() / 2) && e.pageX < ofst.left + $("#cornerMenuResizeButton").width())
			v = true;
		if (e.pageY >= ofst.top + ($("#cornerMenuResizeButton").height() / 2) && e.pageY < ofst.top + $("#cornerMenuResizeButton").height())
			h = true;
		if (h && v)
			$("#outerFrame").css("cursor", "move");
		else if (h)
			$("#outerFrame").css("cursor", "ew-resize");
		else if (v)
			$("#outerFrame").css("cursor", "ns-resize");
		else
			$("#outerFrame").css("cursor", "default");
	});
	$("#cornerMenuResizeButton").mousedown(function (e)
	{
		menuDraggingVertical = menuDraggingHorizontal = false;
		var ofst = $("#cornerMenuResizeButton").offset();
		if (e.pageX >= ofst.left + ($("#cornerMenuResizeButton").width() / 2) && e.pageX < ofst.left + $("#cornerMenuResizeButton").width())
		{
			menuResizeOffsetY = (ofst.top + $("#cornerMenuResizeButton").height()) - e.pageY + 2;
			menuDraggingVertical = true;
		}
		if (e.pageY >= ofst.top + ($("#cornerMenuResizeButton").height() / 2) && e.pageY < ofst.top + $("#cornerMenuResizeButton").height())
		{
			menuResizeOffsetX = (ofst.left + $("#cornerMenuResizeButton").width()) - e.pageX + 2;
			menuDraggingHorizontal = true;
		}
		e.preventDefault();
		e.stopPropagation();
	});
	$("#cornerMenuResizeButton").on("touchstart", function (e)
	{
		if (typeof (e.pageX) == "undefined")
			e.pageX = e.originalEvent.touches[0].pageX;
		if (typeof (e.pageY) == "undefined")
			e.pageY = e.originalEvent.touches[0].pageY;
		var ofst = $("#cornerMenuResizeButton").offset();
		menuResizeOffsetX = (ofst.left + $("#cornerMenuResizeButton").width()) - e.pageX + 2;
		menuResizeOffsetY = (ofst.top + $("#cornerMenuResizeButton").height()) - e.pageY + 2;
		menuDraggingVertical = true;
		menuDraggingHorizontal = true;
	});
	$("#cornerMenuResizeButton").on("mouseup touchcancel touchend", function (e)
	{
		menuDraggingHorizontal = menuDraggingVertical = false;
	});
	$("#cornerMenuResizeButton").mouseenter(function (e)
	{
		$(this).stop(true);
		$(this).fadeTo(200, 1);
	});
	$("#cornerMenuResizeButton").mouseleave(function (e)
	{
		$(this).stop(true);
		$(this).fadeTo(600, 0);
		if (menuDraggingVertical || menuDraggingHorizontal)
			return;
		$("#outerFrame").css("cursor", "default");
	});
	$("#cornerMenuResizeButton").on("touchmove", CornerMenuResize);
	function CornerMenuResize(e)
	{
		if (typeof (e.pageX) == "undefined")
			e.pageX = e.originalEvent.touches[0].pageX;
		if (typeof (e.pageY) == "undefined")
			e.pageY = e.originalEvent.touches[0].pageY;
		var requiresFullResize = false;
		if (menuDraggingHorizontal)
		{
			rightMenuDesiredWidth = $(window).width() - (e.pageX + menuResizeOffsetX);
			requiresFullResize = true;
		}
		if (menuDraggingVertical)
		{
			bottomMenuHeightPercent = 1 - ((e.pageY + menuResizeOffsetY) / $(window).height());
			requiresFullResize = true;
		}
		if (requiresFullResize)
			resize(false);
	}
	//////////////////// Slider Up //////////////////////////
	$(".sliderup").mouseenter(function (e)
	{
		$(this).attr("src", GetButtonSrc(0, 0, 2));
	});
	$(".sliderup").mouseleave(function (e)
	{
		if (this.btnInterval)
			clearInterval(this.btnInterval);
		$(this).attr("src", GetButtonSrc(0, 0, 1));
	});
	$(".sliderup").on("mousedown touchstart", function (e)
	{
		e.preventDefault();
		var btn = this;
		$(this).attr("src", GetButtonSrc(0, 0, 3));
		if (this.btnInterval)
			clearInterval(this.btnInterval);
		this.btnInterval = setInterval(function ()
		{
			var handle = $(btn).closest(".sliderwrapper").find(".sliderhandle").get(0);
			var wrapper = $(handle).parent();
			handle.percent -= 0.02;
			if (handle.percent < 0.0)
				handle.percent = 0.0;
			$(handle).css("top", handle.percent * (wrapper.height() - sliderHandleApparentHeight) + "px");
		}, 41);
	});
	$(".sliderup").on("mouseup touchcancel touchend", function (e)
	{
		if (this.btnInterval)
		{
			clearInterval(this.btnInterval);
			$(this).attr("src", GetButtonSrc(0, 0, 2));
		}
	});
	//////////////////// Slider Down //////////////////////////
	$(".sliderdn").mouseenter(function (e)
	{
		$(this).attr("src", GetButtonSrc(1, 0, 2));
	});
	$(".sliderdn").mouseleave(function (e)
	{
		if (this.btnInterval)
			clearInterval(this.btnInterval);
		$(this).attr("src", GetButtonSrc(1, 0, 1));
	});
	$(".sliderdn").on("mousedown touchstart", function (e)
	{
		e.preventDefault();
		var btn = this;
		$(this).attr("src", GetButtonSrc(1, 0, 3));
		if (this.btnInterval)
			clearInterval(this.btnInterval);
		this.btnInterval = setInterval(function ()
		{
			var handle = $(btn).closest(".sliderwrapper").find(".sliderhandle").get(0);
			var wrapper = $(handle).parent();
			handle.percent += 0.02;
			if (handle.percent > 1.0)
				handle.percent = 1.0;
			$(handle).css("top", handle.percent * (wrapper.height() - sliderHandleApparentHeight) + "px");
		}, 41);
	});
	$(".sliderdn").on("mouseup touchcancel touchend", function (e)
	{
		if (this.btnInterval)
		{
			clearInterval(this.btnInterval);
			$(this).attr("src", GetButtonSrc(1, 0, 2));
		}
	});
	//////////////////// Slider Handle //////////////////////////
	$(".sliderhandle").mouseenter(function (e)
	{
		if (!zoomSliderHandle.isDragging)
			$(zoomSliderHandle).attr("src", GetButtonSrc(4, 0, 2));
	});
	$(".sliderhandle").mouseleave(function (e)
	{
		if (!zoomSliderHandle.isDragging)
			$(zoomSliderHandle).attr("src", GetButtonSrc(4, 0, 1));
	});
	$(".sliderhandle").mousedown(function (e)
	{
		e.preventDefault();
		$(this).attr("src", GetButtonSrc(4, 0, 3));
		zoomSliderHandle.mouseOffsetY = e.pageY - $(this).offset().top;
		zoomSliderHandle.isDragging = true;
	});
	$(".slidertrackwrapper").on("touchstart", function (e)
	{
		e.preventDefault();
		if (typeof (e.pageY) == "undefined")
			e.pageY = e.originalEvent.touches[0].pageY;
		zoomSliderHandle.mouseOffsetY = $(zoomSliderHandle).height() / 2;
		zoomSliderHandle.isDragging = true;
		HandleSliderMouseMove(zoomSliderHandle, e.pageY);
	});
	$(".slidertrackwrapper").on("touchmove", function (e)
	{
		if (typeof (e.pageY) == "undefined")
			e.pageY = e.originalEvent.touches[0].pageY;
		HandleSliderMouseMove(zoomSliderHandle, e.pageY);
	});
	$(".slidertrackwrapper").on("touchcancel touchend", function (e)
	{
		zoomSliderHandle.isDragging = false;
	});
	//////////////////// 3dpos Button //////////////////////////
	$(".togglebtn").click(function (e)
	{
		this.isToggledOn = (this.isToggledOn ? false : true);
		SetToggleBtnImage(this);
		if (this.id == "btn3dPos")
		{
			enabled3dPositioning = this.isToggledOn;
			SetCamCellCursor();
		}
	});
	$(".togglebtn").mouseenter(function (e)
	{
		this.isBeingHovered = true;
		SetToggleBtnImage(this);
	});
	$(".togglebtn").mouseleave(function (e)
	{
		this.isBeingHovered = false;
		this.isMouseDown = false;
		SetToggleBtnImage(this);
	});
	$(".togglebtn").mousedown(function (e)
	{
		this.isMouseDown = true;
		SetToggleBtnImage(this);
	});
	$(".togglebtn").mouseup(function (e)
	{
		this.isMouseDown = false;
		SetToggleBtnImage(this);
	});

	//////////////////// Panorama //////////////////////////
	$(document).dblclick(function (e)
	{
		e.preventDefault();
	});
	$("#panoramaimg, #thumbnailbox, #thumbnailboxServer").mousedown(function (e)
	{
		e.preventDefault();

		panoDragging = true;

		var ofst = $("#panoramaimg").offset();
		panoPercentX = (e.pageX - ofst.left) / $("#panoramaimg").width();
		panoPercentY = (e.pageY - ofst.top) / $("#panoramaimg").height();

		PanoThumbDraw();
	});
	$("#panoramaimg, #thumbnailbox, #thumbnailboxServer").mousewheel(function (e, delta, deltaX, deltaY)
	{
		var zoomed = false
		if (deltaY < 0)
		{
			e.preventDefault();
			zoomSliderHandle.percent -= 0.04;
			if (zoomSliderHandle.percent < 0.0)
				zoomSliderHandle.percent = 0.0;
			zoomed = true;
		}
		else if (deltaY > 0)
		{
			e.preventDefault();
			zoomSliderHandle.percent += 0.04;
			if (zoomSliderHandle.percent > 1.0)
				zoomSliderHandle.percent = 1.0;
			zoomed = true;
		}
		if (zoomed)
		{
			PanoThumbDraw();
			var handle = $("#zoomslider .sliderhandle").get(0);
			$(handle).css("top", handle.percent * ($(handle).parent().height() - sliderHandleApparentHeight) + "px");
		}
	});
	$("#bottomMenu, #rightMenu").mousedown(function (e)
	{
		e.preventDefault();
	});
});
function HandleSliderMouseMove(handle, y)
{
	if (handle.isDragging)
	{
		var wrapper = $(handle).parent();
		var adjustedMousePosY = y - handle.mouseOffsetY - wrapper.offset().top;
		var sliderBarUsableHeight = wrapper.height() - sliderHandleApparentHeight;

		if (adjustedMousePosY < 0)
		{
			adjustedMousePosY = 0;
			handle.percent = 0;
		}
		if (adjustedMousePosY > sliderBarUsableHeight - 1)
		{
			adjustedMousePosY = sliderBarUsableHeight - 1;
			handle.percent = 1;
		}
		else
			handle.percent = adjustedMousePosY / sliderBarUsableHeight;

		$(handle).css("top", adjustedMousePosY + "px");

		PanoThumbDraw();
	}
}
function MouseInsideObject(obj)
{
	return PointInsideObject(obj, mouseX, mouseY);
}
function PointInsideObject(obj, x, y)
{
	var ofst = obj.offset();
	return x >= ofst.left && y >= ofst.top && x < ofst.left + obj.width() && y < ofst.top + obj.height();
}
function RepositionZoomHint()
{
	$("#zoomhint").css("left", (mouseX - $("#zoomhint").outerWidth(true)) + "px").css("top", (mouseY - $("#zoomhint").outerHeight(true)) + "px");
}
function GetButtonSrc(direction, color, state)
{
	if (direction == 4)
		return "Images/Slider_" + state + ".png";
	return "Images/Btn_" + (direction == 0 ? "up" : "dn") + "_" + (color == 0 ? "blue" : "red") + "_" + state + ".png";
}
function SetToggleBtnImage(btn)
{
	if (btn.isToggledOn)
		$(btn).attr("src", "Images/" + $(btn).attr("imgname") + "_4.png");
	else if (btn.isMouseDown)
		$(btn).attr("src", "Images/" + $(btn).attr("imgname") + "_3.png");
	else if (btn.isBeingHovered)
		$(btn).attr("src", "Images/" + $(btn).attr("imgname") + "_2.png");
	else
		$(btn).attr("src", "Images/" + $(btn).attr("imgname") + "_1.png");
}

///////////////
function hammerIt(elm)
{
	hammertime = new Hammer(elm, {});
	hammertime.get('pinch').set({
		enable: true
	});
	var posX = 0,
		posY = 0,
		scale = 1,
		last_scale = 1,
		last_posX = 0,
		last_posY = 0,
		max_pos_x = 0,
		max_pos_y = 0,
		transform = "",
		el = elm;

	hammertime.on('doubletap pan pinch panend pinchend', function (ev)
	{
		if (ev.type == "doubletap")
		{
			transform =
				"translate3d(0, 0, 0) " +
				"scale3d(2, 2, 1) ";
			scale = 2;
			last_scale = 2;
			try
			{
				if (window.getComputedStyle(el, null).getPropertyValue('-webkit-transform').toString() != "matrix(1, 0, 0, 1, 0, 0)")
				{
					transform =
						"translate3d(0, 0, 0) " +
						"scale3d(1, 1, 1) ";
					scale = 1;
					last_scale = 1;
				}
			} catch (err) { }
			el.style.webkitTransform = transform;
			transform = "";
		}

		//pan    
		if (scale > 1)
		{
			posX = last_posX + ev.deltaX;
			posY = last_posY + ev.deltaY;
			max_pos_x = Math.ceil((scale - 1) * el.clientWidth / 2);
			max_pos_y = Math.ceil((scale - 1) * el.clientHeight / 2);
			if (posX > max_pos_x)
			{
				posX = max_pos_x;
			}
			if (posX < -max_pos_x)
			{
				posX = -max_pos_x;
			}
			if (posY > max_pos_y)
			{
				posY = max_pos_y;
			}
			if (posY < -max_pos_y)
			{
				posY = -max_pos_y;
			}
			is_pinching = true;
			if (pinchTimeout != null)
				clearTimeout(pinchTimeout);
			pinchTimeout = setTimeout(function () { is_pinching = false }, 250);
		}


		//pinch
		if (ev.type == "pinch")
		{
			scale = Math.max(.999, Math.min(last_scale * (ev.scale), 8));
		}
		if (ev.type == "pinchend")
		{
			last_scale = scale;
		}

		//panend
		if (ev.type == "panend")
		{
			last_posX = posX < max_pos_x ? posX : max_pos_x;
			last_posY = posY < max_pos_y ? posY : max_pos_y;
		}

		if (scale != 1)
		{
			transform =
				"translate3d(" + posX + "px," + posY + "px, 0) " +
				"scale3d(" + scale + ", " + scale + ", 1)";
		}

		if (transform)
		{
			el.style.webkitTransform = transform;
		}
		pinchScale = scale;
	});
}
///////////////
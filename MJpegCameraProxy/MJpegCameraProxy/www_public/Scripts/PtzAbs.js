var ptzSocket;
var WebSocket_Connecting = 0;
var WebSocket_Open = 1;
var WebSocket_Closing = 2;
var WebSocket_Closed = 3;
var ws_is_ready = false;
var ptzInterval = null;

var previousPtzState = new Object();
previousPtzState.x = 0;
previousPtzState.y = 0;
previousPtzState.z = 1;
previousPtzState.focus = 0;
previousPtzState.iris = 0;

$(window).load(function ()
{
	previousPtzState.x = panoPercentX;
	previousPtzState.y = panoPercentY;
	previousPtzState.z = zoomSliderHandle.percent;

	if (typeof WebSocket != "function")
	{
		PopupMessage("Your browser does not support web sockets.  Web sockets are required to use PTZ control.");
	}
	else
	{
		ptzSocket = new WebSocket(webSocketUrl);
		ptzSocket.onopen = function (event)
		{
			console.log("WebSocket Open");
			ptzSocket.send("session " + $.cookie("cps"));
		};
		ptzSocket.onclose = function (event)
		{
			var errmsg = "WebSocket Closed. (Code " + event.code + (event.reason ? (" " + event.reason) : "") + ")";
			PopupMessage(errmsg);
			console.log(errmsg);
		};
		ptzSocket.onerror = function (event)
		{
			console.log("WebSocket Error");
		};
		ptzSocket.onmessage = function (event)
		{
			console.log(event.data);
			HandleWSMessage(event.data);
		};
		ptzInterval = setInterval(ptzLoop, 100);
	}
});

function CloseSocket()
{
	if (ptzSocket)
		ptzSocket.close();
}

function PtzSend(message)
{
	var wsState = ptzSocket.readyState;
	if (wsState == 1)
	{
		if (ws_is_ready)
			ptzSocket.send(message);
		else
			PopupMessage("PTZ command failed because we are still negotiating with the server.  Please try again in a moment.");
	}
	else if (wsState == 0)
		PopupMessage("WebSocket is still connecting.");
	else if (wsState == 2)
		PopupMessage("WebSocket is closing.");
	else if (wsState == 3)
		PopupMessage("WebSocket is closed.");
}

function HandleWSMessage(msg)
{
	if (msg == "sessioninvalid")
	{
		PopupMessage("The WebSocket Server did not accept your session.");
	}
	else if (msg == "sessionaccepted")
	{
		ptzSocket.send("register " + cameraId);
	}
	else if (msg.indexOf("registrationfailed ") == 0)
	{
		var parts = msg.split(" ");
		var reason = "";
		if (parts.length == 2)
		{
			if (parts[1] == "alreadyregistered") reason = "This socket has already registered a camera.";
			if (parts[1] == "cameraunavailable") reason = "This camera is not available.  Please try again later.";
			if (parts[1] == "insufficientpermission") reason = "You do not have sufficient permission to control this camera.";
		}
		PopupMessage("Unable to register camera " + cameraId + " with the server. " + reason);
	}
	else if (msg == "registrationaccepted")
	{
		ws_is_ready = true;
	}
	else if (msg.indexOf("sup " + cameraId + " newpos ") == 0)
	{
		var parts = msg.split(" ");
		if (parts.length == 6)
		{
			serverPanoPercentX = parseFloat(parts[3]);
			serverPanoPercentY = parseFloat(parts[4]);
			serverPanoPercentZ = parseFloat(parts[5]);

			serverPanoPercentY = (serverPanoPercentY * (1 - thumbnailBoxPercentHeight)) + (thumbnailBoxPercentHeight / 2);

			_PanoThumbDraw("#thumbnailboxServer", "#thumbnailboxinnerServer", serverPanoPercentX, serverPanoPercentY, serverPanoPercentZ);
		}
	}
	else if (msg.indexOf("sup " + cameraId + " 3dpos ") == 0)
	{
		var parts = msg.split(" ");
		if (parts.length == 8)
		{
			showServerside3dPositioningBox(parseFloat(parts[3]), parseFloat(parts[4]), parseFloat(parts[5]), parseFloat(parts[6]), parts[7] == "1");
		}
	}
}

function EndPtzLoop()
{
	if (ptzInterval != null)
		clearInterval(ptzInterval);
}

// The ptzLoop function is called every 100 ms.  Here, we check for changes in client-side PTZ state and send them to the server.
function ptzLoop()
{
	if (previousPtzState.x != panoPercentX || previousPtzState.y != panoPercentY)
	{
		if (!isWebSocketReady())
			return;
		var reportableY = (panoPercentY - thumbnailBoxPercentHeight / 2) / (1 - thumbnailBoxPercentHeight);
		PtzSend("abs " + panoPercentX + " " + reportableY + " " + zoomSliderHandle.percent);
		previousPtzState.x = panoPercentX;
		previousPtzState.y = panoPercentY;
		previousPtzState.z = zoomSliderHandle.percent;
	}
	if (previousPtzState.z != zoomSliderHandle.percent)
	{
		if (!isWebSocketReady())
			return;
		PtzSend("zoom " + zoomSliderHandle.percent);
		previousPtzState.z = zoomSliderHandle.percent;
	}
	if (pos3dNew != null)
	{
		if (!isWebSocketReady())
			return;
		var w = pos3dNew.w > 0.01 ? pos3dNew.w : 0;
		var h = pos3dNew.h > 0.01 ? pos3dNew.h : 0;
		PtzSend("3dpos " + pos3dNew.x + " " + pos3dNew.y + " " + w + " " + h + " " + (pos3dNew.zoomIn ? "1" : "0"));
		pos3dNew = null;
	}
}

function isWebSocketReady()
{
	return ptzSocket.readyState == 1 && ws_is_ready;
}
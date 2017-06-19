using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Threading;
using System.Web;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy
{
	class CamPage2
	{
		public string Html;
		public string cameraId = "";
		public string cameraName = "";
		public int originHeight = 640;
		public int originWidth = 360;
		public int disableRefreshAfter = 600000;
		public int refreshDelay = 250;
		public string webSocketUrl = "";
		public double thumbnailBoxPercentWidth = 0.1;
		public double thumbnailBoxPercentHeight = 0.1;
		public double zoomMagnification = 1;

		public CamPage2(string html, HttpProcessor p)
		{
			// Set the parameters so the eval statements later can access them.
			cameraId = p.GetParam("cam");
			IPCameraBase cam = MJpegServer.cm.GetCameraAndGetItRunning(cameraId);
			if (cam == null)
			{
				Html = "The specified camera is not available.";
				return;
			}
			disableRefreshAfter = p.GetIntParam("override", 600000);
			string userAgent = p.GetHeaderValue("User-Agent", "");
			bool isMobile = userAgent.Contains("iPad") || userAgent.Contains("iPhone") || userAgent.Contains("Android") || userAgent.Contains("BlackBerry");
			bool isLanConnection = p == null ? false : p.IsLanConnection;
			int defaultRefresh = isLanConnection && !isMobile ? 0 : 250;
			refreshDelay = p.GetIntParam("refresh", defaultRefresh);
			cameraName = cam.cameraSpec.name;
			if (cam.cameraSpec.type == CameraType.h264_rtsp_proxy)
			{
				bool sizeOverridden = cam.cameraSpec.h264_video_width > 0 && cam.cameraSpec.h264_video_height > 0;
				originWidth = sizeOverridden ? cam.cameraSpec.h264_video_width : 640;
				originHeight = sizeOverridden ? cam.cameraSpec.h264_video_height : 360;
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
				{
					Html = @"<!DOCTYPE HTML>
						<html>
						<head>
							<title>" + HttpUtility.HtmlEncode(cam.cameraSpec.name) + @"</title>
						</head>
						<body>
						This camera is starting up.<br/>
						Please <a href=""javascript:top.location.reload();"">try again in a few moments</a>.
						</body>
						</html>";
					return;
				}
				originWidth = cam.ImageSize.Width;
				originHeight = cam.ImageSize.Height;
			}
			webSocketUrl = "ws" + (p.secure_https ? "s" : "") + "://\" + location.hostname + \":" + (p.secure_https ? MJpegWrapper.cfg.webSocketPort_secure : MJpegWrapper.cfg.webSocketPort);

			thumbnailBoxPercentWidth = cam.cameraSpec.ptz_panorama_selection_rectangle_width_percent;
			thumbnailBoxPercentHeight = cam.cameraSpec.ptz_panorama_selection_rectangle_height_percent;
			zoomMagnification = cam.cameraSpec.ptz_magnification;

			// Evaluate the <%...%> expressions present in the html markup
			CSE.CsEval.EvalEnvironment = this;
			StringBuilder sb = new StringBuilder();
			int idxCopyFrom = 0;
			int idxStart = 0, idxEnd = 0;
			idxStart = html.IndexOf("<%");
			while (idxStart != -1)
			{
				sb.Append(html.Substring(idxCopyFrom, idxStart - idxCopyFrom));

				idxEnd = html.IndexOf("%>", idxStart + 2);
				if (idxEnd == -1)
				{
					idxCopyFrom = idxStart;
					break;
				}
				else
				{
					string expression = html.Substring(idxStart + 2, idxEnd - (idxStart + 2)).Trim();
					try
					{
						sb.Append(CSE.CsEval.Eval(expression));
					}
					catch (Exception)
					{
						Html = "<h1>Internal Server Errror</h1><p>The page you requested has errors and is unable to be produced.</p>";
						return;
					}
					idxCopyFrom = idxEnd + 2;
					idxStart = html.IndexOf("<%", idxEnd + 2);
				}
			}
			if (idxCopyFrom < html.Length)
				sb.Append(html.Substring(idxCopyFrom));


			this.Html = sb.ToString();
		}
	}
}

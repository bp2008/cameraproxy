using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Threading;
using System.Net;
using System.IO;

namespace MJpegCameraProxy.PanTiltZoom
{
	public class HuisunBullet : IPTZSimple, IPTZHtml, IPTZPresets
	{
		CameraSpec camSpec;
		IPCameraBase cam;
		string url;
		const int panDelay = 1000;
		const int tiltDelay = 1000;
		const int zoomShortDelay = 200;
		const int zoomMediumDelay = 800;
		const int zoomLongDelay = 1600;
		public HuisunBullet(CameraSpec camSpec, IPCameraBase cam)
		{
			this.camSpec = camSpec;
			this.cam = cam;
			url = "http://" + camSpec.ptz_hostName + "/ipnc/";
		}

		#region IPTZHtml Members

		public string GetHtml(string camId, IPCameraBase cam)
		{
			HtmlOptions options = new HtmlOptions();
			options.showPtzArrows = true;
			options.showPtzDiagonals = true;

			options.panDelay = panDelay;
			options.tiltDelay = tiltDelay;

			options.showZoomButtons = true;

			options.showZoomInLong = options.showZoomInMedium = options.showZoomInShort = true;
			options.showZoomOutLong = options.showZoomOutMedium = options.showZoomOutShort = true;

			options.zoomShortDelay = zoomShortDelay;
			options.zoomMediumDelay = zoomMediumDelay;
			options.zoomLongDelay = zoomLongDelay;

			options.showPresets = true;
			int i = 0;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			options.gettablePresets[i++] = true;
			i = 0;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;
			options.settablePresets[i++] = true;

			options.showZoomLevels = false;

			return PTZHtml.GetHtml(camId, cam, options);
		}

		#endregion

		#region IPTZSimple Members

		public void MoveSimple(PTZDirection direction)
		{
			int x = 500;
			int y = 500;
			if (direction == PTZDirection.Up || direction == PTZDirection.UpLeft || direction == PTZDirection.UpRight)
				y = 250;
			if (direction == PTZDirection.Down || direction == PTZDirection.DownLeft || direction == PTZDirection.DownRight)
				y = 750;
			if (direction == PTZDirection.Left || direction == PTZDirection.UpLeft || direction == PTZDirection.DownLeft)
				x = 250;
			if (direction == PTZDirection.Right || direction == PTZDirection.UpRight || direction == PTZDirection.DownRight)
				x = 750;

			HttpPost(url, GetPTZJSONString("34323638666239302D396463362D3131", 497, 569, 0, 0, false));
		}

		public bool SupportsZoom()
		{
			return true;
		}

		public void Zoom(ZoomDirection direction, ZoomAmount amount)
		{
			int boxSize = 200;

			if (amount == ZoomAmount.Short)
				boxSize = 750;
			else if (amount == ZoomAmount.Medium)
				boxSize = 250;
			else if (amount == ZoomAmount.Long)
				boxSize = 50;

			HttpPost(url, GetPTZJSONString("34323638666239302D396463362D3131", 500, 500, boxSize, boxSize, direction == ZoomDirection.In));
		}

		#endregion

		#region IPTZPresets Members

		public void LoadPreset(int presetNum)
		{
			Preset(presetNum, false);
		}

		public void SavePreset(int presetNum)
		{
			Preset(presetNum, true);
		}

		#endregion

		private void Preset(int presetNum, bool save)
		{
			if(presetNum >= 1 && presetNum <= 32)
				HttpPost(url, GetPresetJsonString("34323638666239302D396463362D3131", presetNum, save));
		}

		private string GetPTZJSONString(string session, int x, int y, int width, int height, bool isZoomIn)
		{
			return "{\"header\":{\"version\":101,\"seq\":0,\"peer_type\":4001,\"local_version\":0,\"peer_id\":\"ffffffffffffffff0000000000000001\",\"session_id\":\"0\",\"tt\":\"0\",\"cc\":\"c49234233daa678fb362ed8536ec5d3b6ebd4540\"},\"body\":{\"cmd\":1030,\"channel\":0,\"ox\":" + x + ",\"oy\":" + y + ",\"width\":" + width + ",\"height\":" + height + ",\"speed\":50,\"direction\":" + (isZoomIn ? 1 : 0) + "}}";
		}

		private string GetPresetJsonString(string session, int presetNum, bool setPreset)
		{
			return "{\"header\":{\"version\":101,\"seq\":0,\"peer_type\":4001,\"local_version\":0,\"peer_id\":\"ffffffffffffffff0000000000000001\",\"session_id\":\"" + session + "\",\"tt\":\"" + DateTime.Now.ToString("yyyyMMddHHmmss") + "\",\"cc\":\"94114f60b7ca73e2b08587af5d8e14b15eb3fbd9\"},\"body\":{\"cmd\":1029,\"channel\":0,\"control\":" + (setPreset ? 1 : 2) + ",\"number\":" + presetNum + ",\"title\":\"\"}}";
		}

		protected string HttpPost(string URI, string data)
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				//req.Proxy = null;
				//req.PreAuthenticate = true;
				req.Method = "POST";
				req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:42.0) Gecko/20100101 Firefox/42.0";
				req.Accept = "*/*";
				req.Headers["Accept-Language"] = "en-US,en;q=0.5";
				req.Headers["Accept-Encoding"] = "gzip, deflate";
				req.Headers["X-Requested-With"] = "XMLHttpRequest";
				req.Referer = "http://192.168.0.122/preview.html";
				req.Headers["Pragma"] = "no-cache";
				req.Headers["Cache-Control"] = "no-cache";
				req.CookieContainer = new CookieContainer();
				req.CookieContainer.Add(new Cookie("yummy_magical_cookie", "/ipnc/", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("language", "en", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("session_id", "34323638666239302D396463362D3131", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("userName", camSpec.ptz_username,"/",req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("password", camSpec.ptz_password, "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("user_level", "0", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("rtsp_port", "554", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("deviceInfo", "%7B%22dechannel_cnt%22%3A1%2C%22model%22%3A%22%22%2C%22serial%22%3A%22104559564C58768279%22%2C%22firmware_version%22%3A%22V1.0.2%20Build%20201509160901%22%2C%22software_version%22%3A%22MiniPtz_V1.0.2_build201509241507%22%2C%22alarm_input%22%3A0%2C%22alarm_output%22%3A0%2C%22audio%22%3A0%2C%22harddisk_cnt%22%3A0%2C%22focusType%22%3A0%2C%22rs232%22%3A0%2C%22rs485%22%3A0%2C%22track%22%3A0%2C%22product_type%22%3A1%7D", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("def_ip", "192.168.0.99", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("showPlugin", "no", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("checkPluginVersion", "no", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("configPage", "local", "/", req.RequestUri.Host));
				req.CookieContainer.Add(new Cookie("page", "preview.html", "/", req.RequestUri.Host));
				//req.Credentials = new NetworkCredential(camSpec.ptz_username, camSpec.ptz_password);

				if (data != null)
				{
					byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
					req.ContentLength = bytes.Length;

					using (Stream os = req.GetRequestStream())
					{
						os.Write(bytes, 0, bytes.Length);
					}
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					string responseText = sr.ReadToEnd();
					return responseText;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
				return ex.ToString();
			}
		}
	}
}

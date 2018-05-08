using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BPUtil;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy
{
	public class HtmlResponse
	{
		public Html.ErrorType errorType = Html.ErrorType.NoError;
		public string html;

		public HtmlResponse(Html.ErrorType errorType)
		{
			this.errorType = errorType;
		}

		public HtmlResponse(string html)
		{
			this.html = html;
		}
	}
	public class Html
	{
		public enum ErrorType
		{
			NoError,
			NoExist,
			BadFile,
			NoAuth
		}
		public static HtmlResponse GetHtml(string pageName, Session s, HttpProcessor p)
		{
			if (pageName.Contains('\\') || pageName.Contains('/') || pageName.Contains("..") || pageName.Contains(':'))
				return new HtmlResponse(Html.ErrorType.NoExist); // Invalid character in page name.
			string fileName = CameraProxyGlobals.HtmlDirectoryBase + pageName;
			if (!File.Exists(fileName))
				return new HtmlResponse(Html.ErrorType.NoExist);
			string file = File.ReadAllText(fileName);

			int idxFirstLineBreak = file.IndexOfAny(new char[] { '\r', '\n' });
			if (idxFirstLineBreak == -1)
				return new HtmlResponse(Html.ErrorType.BadFile);

			int permissionRequired;
			if (!int.TryParse(file.Substring(0, idxFirstLineBreak), out permissionRequired))
				return new HtmlResponse(Html.ErrorType.BadFile);

			if (permissionRequired < 0 || permissionRequired > 100)
				return new HtmlResponse(Html.ErrorType.BadFile);

			if ((s == null && permissionRequired > 0) || (s.permission < permissionRequired))
				return new HtmlResponse(Html.ErrorType.NoAuth);

			string html = file.Substring(3);
			html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
			html = html.Replace("%ALLCAMS_IDS_NAMES_JS_ARRAY%", MJpegServer.cm.GenerateAllCameraIdNameList(s == null ? 0 : s.permission));
			try
			{
				html = html.Replace("%REMOTEIP%", p.RemoteIPAddressStr);
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return new HtmlResponse(html);
		}
	}
}

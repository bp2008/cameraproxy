using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimpleHttp;

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
			string fileName = Globals.HtmlDirectoryBase + pageName;
			if (!File.Exists(fileName))
				return new HtmlResponse(Html.ErrorType.NoExist);
			string file = File.ReadAllText(fileName);

			bool fileIsPrivate = file.StartsWith("1\r\n");
			bool fileIsPublic = file.StartsWith("0\r\n");
			if (!fileIsPrivate && !fileIsPublic)
				return new HtmlResponse(Html.ErrorType.BadFile);

			if (fileIsPrivate && s == null)
				return new HtmlResponse(Html.ErrorType.NoAuth);

			string html = file.Substring(3);
			html = html.Replace("%ALLCAMS%", string.Join(",", MJpegServer.cm.GenerateAllCameraIdList()));
			try
			{
				html = html.Replace("%REMOTEIP%", p.RemoteIPAddress);
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return new HtmlResponse(html);
		}
	}
}

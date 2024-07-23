using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Pages.Admin
{
	class Login : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			p.Response.Cookies.Add("cps", "", TimeSpan.Zero);
			p.Response.Cookies.Add("auth", "", TimeSpan.Zero);
			return MJpegCameraProxy.Login.GetLoginScripts("main") + "<div style=\"margin-bottom: 10px;\">Please log in to continue:</div>" + MJpegCameraProxy.Login.GetLoginBody();
		}
	}
}

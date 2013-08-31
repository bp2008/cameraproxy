using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MJpegCameraProxy
{
	public class Cookie
	{
		public string name;
		public string value;
		public TimeSpan expire;

		public Cookie(string name, string value, TimeSpan expire)
		{
			this.name = name;
			this.value = value;
			this.expire = expire;
		}
	}
	public class Cookies
	{
		SortedList<string, Cookie> cookieCollection = new SortedList<string, Cookie>();
		public void Add(string name, string value)
		{
			Add(name, value, TimeSpan.Zero);
		}
		public void Add(string name, string value, TimeSpan expireTime)
		{
			if (name == null)
				return;
			name = name.ToLower();
			cookieCollection[name] = new Cookie(name, value, expireTime);
		}
		public Cookie Get(string name)
		{
			Cookie cookie;
			if (!cookieCollection.TryGetValue(name, out cookie))
				cookie = null;
			return cookie;
		}
		public string GetValue(string name)
		{
			Cookie cookie = Get(name);
			if (cookie == null)
				return "";
			return cookie.value;
		}
		public override string ToString()
		{
			List<string> cookiesStr = new List<string>();
			foreach (Cookie cookie in cookieCollection.Values)
				cookiesStr.Add("Set-Cookie: " + cookie.name + "=" + cookie.value + (cookie.expire == TimeSpan.Zero ? "" : "; Expires=" + DateTime.UtcNow.Add(cookie.expire).ToCookieTime()));
			return string.Join(Environment.NewLine, cookiesStr);
		}
		public static Cookies FromString(string str)
		{
			Cookies cookies = new Cookies();
			if (str == null)
				return cookies;
			str = HttpUtility.UrlDecode(str);
			string[] parts = str.Split(';');
			for (int i = 0; i < parts.Length; i++)
			{
				int idxEquals = parts[i].IndexOf('=');
				if (idxEquals < 1)
					continue;
				string name = parts[i].Substring(0, idxEquals).Trim();
				string value = parts[i].Substring(idxEquals + 1).Trim();
				cookies.Add(name, value);
			}
			return cookies;
		}
	}
}

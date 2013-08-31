using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class Session
	{
		public string sid;
		private string user;
		private string pass;
		public DateTime expire;

		public Session(string user, string pass)
		{
			this.user = user;
			this.pass = pass;
			expire = DateTime.Now.AddMinutes(20);
			sid = GenerateSid();
		}

		private string GenerateSid()
		{
			StringBuilder sb = new StringBuilder(16);
			while (sb.Length < 16)
				sb.Append(Util.GetRandomAlphaNumericChar());
			return sb.ToString();
		}

	}
}

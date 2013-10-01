using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class Session
	{
		public string sid;
		private string username;
		public int permission;
		public DateTime expire;
		public int sessionLengthMinutes;

		public Session(string username, int permission, int sessionLengthMinutes)
		{
			this.username = username;
			this.permission = permission;
			this.sessionLengthMinutes = sessionLengthMinutes;
			expire = DateTime.Now.AddMinutes(sessionLengthMinutes);
			sid = GenerateSid();
		}

		public static string GenerateSid()
		{
			StringBuilder sb = new StringBuilder(16);
			while (sb.Length < 16)
				sb.Append(Util.GetRandomAlphaNumericChar());
			return sb.ToString();
		}

	}
}

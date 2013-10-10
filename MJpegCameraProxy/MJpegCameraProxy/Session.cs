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

		private SortedList<string, ImageSignature> lastJpgFrame = new SortedList<string, ImageSignature>();

		public Session(string username, int permission, int sessionLengthMinutes)
		{
			if (sessionLengthMinutes < 0)
				sessionLengthMinutes = 0;
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

		/// <summary>
		/// Returns true if the specified id and image has already been sent to this function.
		/// </summary>
		/// <param name="id">The ID string of the camera.</param>
		/// <param name="imgData">The image bytes.</param>
		/// <returns></returns>
		public bool DuplicateImageSendCheck(string id, byte[] imgData)
		{
			if (imgData == null)
				return false;
			ImageSignature img = new ImageSignature(imgData);
			ImageSignature old;
			lock(lastJpgFrame)
			{
				if (!lastJpgFrame.TryGetValue(id, out old))
					old = null;
				lastJpgFrame[id] = img;
			}
			if (old == null)
				return false;
			return img.Equals(old);
		}
	}
}

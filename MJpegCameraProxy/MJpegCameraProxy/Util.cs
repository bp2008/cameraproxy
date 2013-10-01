using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MJpegCameraProxy
{
	public static class Util
	{
		private static Random rand = new Random();
		public static char GetRandomAlphaNumericChar()
		{
			int i;
			lock (rand)
			{
				i = rand.Next(62);
			}
			if (i < 10)
				return (char)(48 + i);
			if (i < 36)
				return (char)(65 + (i - 10));
			return (char)(97 + (i - 36));
		}
		public static bool ParseBool(string str, bool defaultValueIfUnspecified = false)
		{
			if (string.IsNullOrEmpty(str))
				return defaultValueIfUnspecified;
			if (str == "1")
				return true;
			string strLower = str.ToLower();
			if (strLower == "true" || strLower.StartsWith("y"))
				return true;
			return false;
		}
		public static string ToCookieTime(this DateTime time)
		{
			return time.ToString("dd MMM yyyy hh:mm:ss GMT");
		}
		public static bool EnsureDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
				return Directory.CreateDirectory(path).Exists;
			return true;
		}
		public static void WriteImageThumbnailToFile(byte[] imageData, string path, int width = 128, int height = 96)
		{
			try
			{
				Image img = ImageFromBytes(imageData);
				if (img == null)
					return;
				FileInfo file = new FileInfo(path);
				Util.EnsureDirectoryExists(file.Directory.FullName);
				img = img.GetThumbnailImage(width, height, null, System.IntPtr.Zero);
				File.WriteAllBytes(file.FullName, ImageConverter.GetJpegBytes(img));
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}
		public static Image ImageFromBytes(byte[] imageData)
		{
			try
			{
				using (MemoryStream ms = new MemoryStream(imageData))
				{
					return Image.FromStream(ms);
				}
			}
			catch (Exception) { }
			return null;
		}
		public static bool IsAlphaNumeric(string str, bool spacesAndTabsIncluded = false)
		{
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (spacesAndTabsIncluded && (c == ' ' || c == '\t'))))
					return false;
			}
			return true;
		}
	}
}

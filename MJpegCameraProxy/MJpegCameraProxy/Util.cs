using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.Drawing.Imaging;
using MJpegCameraProxy.Configuration;
using BPUtil;

namespace MJpegCameraProxy
{
	public static class Util
	{
		public static char GetRandomAlphaNumericChar()
		{
			int i = StaticRandom.Next(62);
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
			WrappedImage wi = null;
			try
			{
				if (imageData == null || imageData.Length == 0)
					return;
				FileInfo file = new FileInfo(path);
				Util.EnsureDirectoryExists(file.Directory.FullName);
				wi = new WrappedImage(imageData);
				File.WriteAllBytes(file.FullName, wi.ToByteArray(ImageFormat.Jpeg));

			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			finally
			{
				if (wi != null)
					wi.Dispose();
			}
		}
		[Obsolete]
		/// <summary>
		/// This method is DANGEROUS.  The Image is not valid if the MemoryStream is closed.
		/// </summary>
		/// <param name="imageData"></param>
		/// <returns></returns>
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
		public static byte[] GetJpegBytes(System.Drawing.Image img, long quality = 80)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				SaveJpeg(ms, img, quality);
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Saves the image into the specified stream using the specified jpeg quality level.
		/// </summary>
		/// <param name="stream">The stream to save the image in.</param>
		/// <param name="img">The image to save.</param>
		/// <param name="quality">(Optional) Jpeg compression quality, from 0 (low quality) to 100 (very high quality).</param>
		public static void SaveJpeg(System.IO.Stream stream, System.Drawing.Image img, long quality = 80)
		{
			if (quality < 0)
				quality = 0;
			if (quality > 100)
				quality = 100;
			EncoderParameters encoderParams = new EncoderParameters(1);
			EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
			if (jpegCodec == null)
				return;

			encoderParams.Param[0] = qualityParam;
			img.Save(stream, jpegCodec, encoderParams);
		}
		private static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

			for (int i = 0; i < encoders.Length; i++)
				if (encoders[i].MimeType == mimeType)
					return encoders[i];
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

		public static string HttpPost(string URI, string data, string ContentType = "application/x-www-form-urlencoded; charset=utf-8", CookieContainer cookieContainer = null, NetworkCredential credentials = null)
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				req.Proxy = null;
				if (credentials != null)
					req.Credentials = credentials;
				req.ContentType = ContentType;
				req.Method = "POST";
				req.UserAgent = "CameraProxy " + Globals.Version;
				if (cookieContainer == null)
					cookieContainer = new CookieContainer();
				req.CookieContainer = cookieContainer;

				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
				req.ContentLength = bytes.Length;

				using (Stream os = req.GetRequestStream())
				{
					os.Write(bytes, 0, bytes.Length);
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					return sr.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				return ex.ToString();
			}
		}

		public static string GetMime(ImageFormat imgFormat)
		{
			if (imgFormat == ImageFormat.Png)
				return "image/png";
			else if (imgFormat == ImageFormat.Webp)
				return "image/webp";
			else
				return "image/jpeg";
		}


		public static bool TryGetValue(string requestedPage, List<SimpleWwwFile> fileList, out int permissionRequired)
		{
			int idx = fileList.BinarySearch(new SimpleWwwFile(requestedPage, 0), new ComparisonComparer<SimpleWwwFile>((kvp1, kvp2) =>
			{
				return string.Compare(kvp1.Key, kvp2.Key);
			}));
			if (idx < 0)
			{
				permissionRequired = default(int);
				return false;
			}
			else
			{
				permissionRequired = fileList[idx].Value;
				return true;
			}
		}
		public static float Clamp(float num, float min, float max)
		{
			if (num < min)
				return min;
			if (num > max)
				return max;
			return num;
		}

		public static double Clamp(double num, double min, double max)
		{
			if (num < min)
				return min;
			if (num > max)
				return max;
			return num;
		}
		public static int Clamp(int num, int min, int max)
		{
			if (num < min)
				return min;
			if (num > max)
				return max;
			return num;
		}

		public static double RangeValueToPercentage(double valInRange, double lowerBound, double upperBound)
		{
			double range = upperBound - lowerBound;
			if (range == 0)
			{
				if (valInRange > lowerBound)
					return 1;
				else
					return 0;
			}
			double normalized = valInRange - lowerBound;
			return normalized / range;
		}

		public static double PercentageToRangeValueDouble(double percentage, double lowerBound, double upperBound)
		{
			double range = upperBound - lowerBound;
			if (range == 0)
				return lowerBound;
			return (percentage * range) + lowerBound;
		}
		public static int PercentageToRangeValueInt(double percentage, int lowerBound, int upperBound)
		{
			int range = upperBound - lowerBound;
			if (range == 0)
				return lowerBound;
			return (int)Math.Round(percentage * range) + lowerBound;
		}

		/// <summary>
		/// An alternative implementation of the modulus operation which handles negative numbers differently than the modulus operator does.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static int Modulus(int x, int m)
		{
			return (x % m + m) % m;
		}

		/// <summary>
		/// Given a mangification amount (i.e. 2, 4, 8), calculates a ROUGHLY appropriate dahua zoom field value. Because I am mathematically challenged, this will result in mild to moderate inaccuracies in 3d positioning when zoomed in at all.
		/// </summary>
		/// <param name="magnification"></param>
		/// <param name="maxMagnification"></param>
		/// <returns></returns>
		public static int DahuaZoomCalc(double magnification, double maxMagnification)
		{
			if (maxMagnification <= 1)
				return 1;
			if (magnification <= 1)
				return 1;
			if (magnification > maxMagnification)
				return 128;

			double stupid_ceiling = 1.0 - (1 / maxMagnification);
			double myPercentMagnification = 1 / magnification;
			myPercentMagnification = 1 - myPercentMagnification;
			double myPercentOfStupidRange = myPercentMagnification / stupid_ceiling;
			return PercentageToRangeValueInt(myPercentOfStupidRange, 1, 128);
		}
		/// <summary>
		/// Given a dahua zoom amount, calculates a ROUGHLY appropriate magnification level. Because I am mathematically challenged, this will result in mild to moderate inaccuracies in 3d positioning when zoomed in at all.
		/// </summary>
		/// <param name="magnification"></param>
		/// <param name="maxMagnification"></param>
		/// <returns></returns>
		public static double DahuaMagnificationCalc(double DahuaZoomAmount, double maxMagnification)
		{
			if (maxMagnification <= 1)
				return 1;
			if (DahuaZoomAmount <= 1)
				return 1;
			if (DahuaZoomAmount >= 128)
				return maxMagnification;

			double stupid_ceiling = 1.0 - (1 / maxMagnification);

			double myPercentOfStupidRange = RangeValueToPercentage(DahuaZoomAmount, 1, 128);
			double myPercentMagnification = myPercentOfStupidRange * stupid_ceiling;
			myPercentMagnification = 1 - myPercentMagnification;
			return 1 / myPercentMagnification;
		}
	}
}

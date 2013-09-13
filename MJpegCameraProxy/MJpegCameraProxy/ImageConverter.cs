using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using SimpleHttp;

namespace MJpegCameraProxy
{
	public static class ImageConverter
	{
		public static byte[] HandleRequestedConversionIfAny(byte[] input, HttpProcessor p)
		{
			int size = p.GetIntParam("size", 100);
			int quality = p.GetIntParam("quality", 80);
			string format = p.GetParam("format").ToLower();
			int rotateDegrees = p.GetIntParam("rotate", 0);
			int maxWidth = p.GetIntParam("maxwidth", 0);
			int maxHeight = p.GetIntParam("maxheight", 0);

			RotateFlipType rotateFlipType;
			if (rotateDegrees == -270 || rotateDegrees == 90)
				rotateFlipType = RotateFlipType.Rotate90FlipNone;
			else if (rotateDegrees == 270 || rotateDegrees == -90)
				rotateFlipType = RotateFlipType.Rotate270FlipNone;
			else if (rotateDegrees == 180 || rotateDegrees == -180)
				rotateFlipType = RotateFlipType.Rotate180FlipNone;
			else
				rotateFlipType = RotateFlipType.RotateNoneFlipNone;


			if (size != 100 || (maxWidth > 0 && maxHeight > 0) || quality != 80 || rotateFlipType != RotateFlipType.RotateNoneFlipNone || format == "webp" || format == "png")
				return ConvertImage(input, size, quality, GetImageFormatEnumValue(format), rotateFlipType, maxWidth, maxHeight);
			return input;
		}

		private static ImageFormat GetImageFormatEnumValue(string format)
		{
			if (format == "webp")
				return ImageFormat.Webp;
			if (format == "png")
				return ImageFormat.Png;
			return ImageFormat.Jpeg;
		}
		public static byte[] ConvertImage(byte[] input, int size = 100, int quality = 80, ImageFormat format = ImageFormat.Jpeg, RotateFlipType rotateFlipType = RotateFlipType.RotateNoneFlipNone, int maxWidth = 0, int maxHeight = 0)
		{
			byte[] output = input;
			if (size < 1)
				size = 1;
			if (size > 100)
				size = 100;
			if (input.Length == 0)
				return input;

			try
			{
				using (MemoryStream ms = new MemoryStream(input))
				{
					Bitmap bmp = new Bitmap(ms);

					// Calculate new size
					int w = bmp.Width;
					int h = bmp.Height;
					if (size < 100)
					{
						w = Math.Max(1, (int)(bmp.Width * (size / 100.0)));
						h = Math.Max(1, (int)(bmp.Height * (size / 100.0)));
					}
					if (maxWidth > 0 && maxHeight > 0)
					{
						if (w > maxWidth)
						{
							double diff = w / maxWidth;
							w = maxWidth;
							h = (int)(h / diff);
						}
						if (h > maxHeight)
						{
							double diff = h / maxHeight;
							h = maxHeight;
							w = (int)(w / diff);
						}
					}
					if (w != bmp.Width || h != bmp.Height)
						bmp = (Bitmap)bmp.GetThumbnailImage(w, h, null, System.IntPtr.Zero);
					if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
						bmp.RotateFlip(rotateFlipType);

					if (format == ImageFormat.Jpeg)
					{
						output = GetJpegBytes(bmp, quality);
					}
					else if (format == ImageFormat.Webp)
					{
						output = WebP.ToWebP(bmp, quality);
					}
					else if (format == ImageFormat.Png)
					{
						using (MemoryStream ms2 = new MemoryStream())
						{
							bmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
							output = ms2.ToArray();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return output;
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
	}
}

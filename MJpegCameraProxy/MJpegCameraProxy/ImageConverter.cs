using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using SimpleHttp;

namespace MJpegCameraProxy
{
	public static class ImageConverter
	{
		public static byte[] HandleRequestedConversionIfAny(byte[] input, HttpProcessor p, ref ImageFormat imgFormat, string desiredFormatString = "")
		{
			if (imgFormat == ImageFormat.Webp)
				return input; // Cannot currently read WebP images.

			int size = p.GetIntParam("size", 100);
			int quality = p.GetIntParam("quality", -2);
			string format = p.GetParam("format").ToLower();
			if (string.IsNullOrWhiteSpace(format))
				format = desiredFormatString == null ? "" : desiredFormatString.ToLower();
			int rotateDegrees = p.GetIntParam("rotate", 0);
			int maxWidth = p.GetIntParam("maxwidth", 0);
			int maxHeight = p.GetIntParam("maxheight", 0);
			string special = p.GetParam("special");

			RotateFlipType rotateFlipType;
			if (rotateDegrees == -270 || rotateDegrees == 90)
				rotateFlipType = RotateFlipType.Rotate90FlipNone;
			else if (rotateDegrees == 270 || rotateDegrees == -90)
				rotateFlipType = RotateFlipType.Rotate270FlipNone;
			else if (rotateDegrees == 180 || rotateDegrees == -180)
				rotateFlipType = RotateFlipType.Rotate180FlipNone;
			else
				rotateFlipType = RotateFlipType.RotateNoneFlipNone;

			ImageFormat desiredFormat = GetImageFormatEnumValue(format);
			if (size != 100 || (maxWidth > 0 && maxHeight > 0) || quality != -2 || rotateFlipType != RotateFlipType.RotateNoneFlipNone || desiredFormat != imgFormat || special == "red")
			{
				if (quality == -2)
					quality = 80;
				byte[] output = ConvertImage(input, size, quality, desiredFormat, rotateFlipType, maxWidth, maxHeight, imgFormat, special, p.request_url.OriginalString);
				if (input != output)
					imgFormat = desiredFormat;
				return output;
			}
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
		/// <summary>
		/// Converts the image to the specified format.
		/// </summary>
		/// <param name="img">Source image</param>
		/// <param name="size">Percentage size.  100 does not affect image size and is the most efficient option.  This parameter is overridden if maxWidth and maxHeight are both specified.</param>
		/// <param name="quality">Compression quality.  Acceptable range depends on image format:
		/// JPG: 0 to 100
		/// WebP: 0 to 100 or -1 for lossless encode
		/// PNG: No effect</param>
		/// <param name="format">Jpeg, PNG, or WebP</param>
		/// <param name="rotateFlipType">Image rotation and flipping options.</param>
		/// <param name="maxWidth">Maximum width of the output image.  Original image aspect ratio will be preserved.  Requires maxHeight to be specified also.  Overrides the "size" parameter.</param>
		/// <param name="maxHeight">Maximum height of the output image.  Original image aspect ratio will be preserved.  Requires maxWidth to be specified also.  Overrides the "size" parameter.</param>
		/// <param name="special">A string indicating a special image transformation, if any.</param>
		/// <returns>A byte array containing image data in the specified format.</returns>
		/// <remarks>This is called internally by ConvertImage(...)</remarks>
		public static byte[] EncodeImage(WrappedImage img, int size = 100, int quality = 80, ImageFormat format = ImageFormat.Jpeg, RotateFlipType rotateFlipType = RotateFlipType.RotateNoneFlipNone, int maxWidth = 0, int maxHeight = 0, string special = "")
		{
			byte[] output = new byte[0];
			if (size < 1)
				size = 1;
			if (size > 100)
				size = 100;

			// Calculate new size
			int w = img.Width;
			int h = img.Height;
			if (size < 100)
			{
				w = Math.Max(1, (int)(img.Width * (size / 100.0)));
				h = Math.Max(1, (int)(img.Height * (size / 100.0)));
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
			if (w != img.Width || h != img.Height)
				img.Resize(w, h);
			if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
				img.RotateFlip(rotateFlipType);

			if (special == "red")
				img.TurnRed();

			output = img.ToByteArray(format, quality);
			return output;
		}

		/// <summary>
		/// Converts the image to the specified format.
		/// </summary>
		/// <param name="input">byte array containing the source image data</param>
		/// <param name="size">Percentage size.  100 does not affect image size and is the most efficient option.  This parameter is overridden if maxWidth and maxHeight are both specified.</param>
		/// <param name="quality">Compression quality.  Acceptable range depends on image format:
		/// JPG: 0 to 100
		/// WebP: 0 to 100 or -1 for lossless encode
		/// PNG: No effect</param>
		/// <param name="format">Jpeg, PNG, or WebP</param>
		/// <param name="rotateFlipType">Image rotation and flipping options.</param>
		/// <param name="maxWidth">Maximum width of the output image.  Original image aspect ratio will be preserved.  Requires maxHeight to be specified also.  Overrides the "size" parameter.</param>
		/// <param name="maxHeight">Maximum height of the output image.  Original image aspect ratio will be preserved.  Requires maxWidth to be specified also.  Overrides the "size" parameter.</param>
		/// <param name="srcImageFormat">The image format of the source data.  If this format matches the "format" parameter and no other operations are requested, the image may not be re-encoded at all.</param>
		/// <param name="special">A string indicating a special image transformation, if any.</param>
		/// <returns>A byte array containing image data in the specified format.</returns>
		/// <remarks>Calls EncodeBitmap(...) internally.</remarks>
		public static byte[] ConvertImage(byte[] input, int size = 100, int quality = 80, ImageFormat format = ImageFormat.Jpeg, RotateFlipType rotateFlipType = RotateFlipType.RotateNoneFlipNone, int maxWidth = 0, int maxHeight = 0, ImageFormat srcImageFormat = ImageFormat.Jpeg, string special = "", string errorHelp = "")
		{
			if (srcImageFormat == ImageFormat.Webp)
				return input; // Cannot currently read WebP images

			byte[] output = input;
			if (input.Length == 0)
				return input;

			WrappedImage wi = null;
			try
			{
				wi = new WrappedImage(input);
				byte[] encoded = EncodeImage(wi, size, quality, format, rotateFlipType, maxWidth, maxHeight, special);
				if (encoded.Length > 0)
					output = encoded;
			}
			catch (Exception ex)
			{
				StringBuilder inputstart = new StringBuilder();
				for (int i = 0; i < input.Length && i < 50; i++)
					inputstart.Append(input[i]).Append(',');
				StringBuilder inputend = new StringBuilder();
				for (int i = input.Length - 50; i < input.Length && i > 0; i++) // If i <= 0, inputend would just be the same as inputstart anyway.  This would happen if input.Length was <= 50.
					inputend.Append(input[i]).Append(',');
				Logger.Debug(ex, errorHelp + " - input length: " + input.Length + ", " + inputstart.ToString() + "  " + inputend.ToString());
			}
			finally
			{
				if (wi != null)
					wi.Dispose();
			}
			return output;
		}
	}
}

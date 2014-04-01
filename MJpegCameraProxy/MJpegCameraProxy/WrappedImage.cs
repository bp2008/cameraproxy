using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ImageMagick;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO;

namespace MJpegCameraProxy
{
	public class WrappedImage
	{
		private Bitmap bmp;
		private MagickImage mi;
		public WrappedImage(Bitmap bmp)
		{
			if (bmp == null)
				throw new NullReferenceException();
			this.bmp = bmp;
		}
		public WrappedImage(MagickImage mi)
		{
			if (mi == null)
				throw new NullReferenceException();
			this.mi = mi;
		}

		public WrappedImage(byte[] input, bool useImageMagick)
		{
			if (useImageMagick)
				mi = new MagickImage(input);
			else
			{
				MemoryStream ms = new MemoryStream(input);
				bmp = new Bitmap(ms);
			}
		}
		public int Width
		{
			get
			{
				if (bmp != null)
					return bmp.Width;
				return mi.Width;
			}
		}
		public int Height
		{
			get
			{
				if (bmp != null)
					return bmp.Height;
				return mi.Height;
			}
		}

		public void Resize(int w, int h)
		{
			if (bmp != null)
				bmp = (Bitmap)bmp.GetThumbnailImage(w, h, null, System.IntPtr.Zero);
			else
				mi.Resize(w, h);
		}

		public void RotateFlip(RotateFlipType rotateFlipType)
		{
			if (bmp != null)
				bmp.RotateFlip(rotateFlipType);
		}

		public void TurnRed()
		{
			if (bmp != null)
			{
				if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
					return;

				// Based on: http://www.codeproject.com/Articles/16403/Fast-Pointerless-Image-Processing-in-NET
				int BitsPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat);
				int BytesPerPixel = BitsPerPixel / 8;
				int stride = 4 * ((bmp.Width * BitsPerPixel + 31) / 32);
				byte[] bits = new byte[stride * bmp.Height];
				GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
				try
				{
					IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(bits, 0);
					Bitmap bitmap = new Bitmap(bmp.Width, bmp.Height, stride, bmp.PixelFormat, pointer);

					Graphics g = Graphics.FromImage(bitmap);
					g.DrawImageUnscaledAndClipped(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
					g.Dispose();

					for (int y = 0; y < bmp.Height; y++)
					{
						int start = stride * y;
						int end = start + (bmp.Width * BytesPerPixel);
						for (int i = start; i < end; i += 3)
						{
							bits[i + 2] = (byte)((bits[i] + bits[i + 1] + bits[i + 2]) / 3);
							bits[i] = bits[i + 1] = 0;
						}
					}
					bmp = new Bitmap(bitmap);
				}
				finally
				{
					handle.Free();
				}
			}
			else
			{
				if (mi.ColorSpace == ColorSpace.sRGB && mi.ColorType == ColorType.TrueColor)
				{
					WritablePixelCollection data = mi.GetWritablePixels();
					byte[] vals = data.GetValues();
					int stride = data.Width * 4;
					for (int y = 0; y < data.Height; y++)
					{
						int start = stride * y;
						int end = start + stride;
						for (int i = start; i < end; i += 4)
						{
							vals[i] = ByteAverage(vals[i], vals[i + 1], vals[i + 2]);
							vals[i + 1] = vals[i + 2] = 0;
						}
					}
					data.Set(vals);
					data.Write();
				}
				else
				{
					Console.WriteLine(mi.ColorSpace + " " + mi.ColorType);
				}
			}
		}

		private byte ByteAverage(params byte[] bytes)
		{
			int total = 0;
			foreach (byte b in bytes)
				total += b;
			return (byte)(total / bytes.Length);
		}

		public byte[] ToByteArray(ImageFormat format, int quality = 80)
		{
			if (bmp != null)
			{
				if (format == ImageFormat.Webp)
					return WebP.ToWebP(bmp, quality);
				else if (format == ImageFormat.Png)
				{
					using (MemoryStream ms2 = new MemoryStream())
					{
						bmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
						return ms2.ToArray();
					}
				}
				else
					return Util.GetJpegBytes(bmp, quality);
			}
			else
			{
				mi.Quality = quality;
				if (format == ImageFormat.Webp)
					mi.Format = MagickFormat.WebP;
				else if (format == ImageFormat.Png)
					mi.Format = MagickFormat.Png;
				else
					mi.Format = MagickFormat.Jpeg;
				return mi.ToByteArray();
			}
		}
		private bool disposed = false;
		/// <summary>
		/// Call this when you are finished with the WrappedImage.  While this is not strictly necessary it will help manage memory if the inner image is a .net Bitmap.
		/// </summary>
		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			if (bmp != null)
				bmp.Dispose();
		}
	}
}

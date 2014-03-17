using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace MJpegCameraProxy
{
	public static class WebP
	{
		/// <summary>
		/// Converts the specified image to WebP.  Image must be in 24 BPP RGB format.
		/// </summary>
		/// <param name="input">Image must be in 24 BPP RGB format.</param>
		/// <param name="quality">(Optional) Output quality (0-100).  Pass -1 for lossless quality.</param>
		/// <returns></returns>
		public static byte[] ToWebP(Bitmap input, int quality = 80)
		{
			bool inputGotReplaced = false;
			if (quality < -1)
				quality = -1;
			if (quality > 100)
				quality = 100;
			try
			{
				if (input.PixelFormat != PixelFormat.Format24bppRgb)
				{
					Bitmap bmp = new Bitmap(input.Width, input.Height, PixelFormat.Format16bppRgb555);
					using (var gr = Graphics.FromImage(bmp))
						gr.DrawImage(input, new Rectangle(0, 0, input.Width, input.Height));
					input = bmp;
					inputGotReplaced = true;
				}

				BitmapData data = input.LockBits(
					new Rectangle(0, 0, input.Width, input.Height),
					ImageLockMode.ReadOnly,
					PixelFormat.Format24bppRgb);

				IntPtr unmanagedData = IntPtr.Zero;
				int size;
				try
				{
					size = WebPEncodeBGR(data.Scan0, input.Width, input.Height, data.Stride, quality, out unmanagedData);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					if (unmanagedData != IntPtr.Zero)
						WebPFree(unmanagedData);
					return new byte[0];
				}
				input.UnlockBits(data);
				try
				{
					byte[] managedData = new byte[size];
					Marshal.Copy(unmanagedData, managedData, 0, size);
					return managedData;
				}
				catch (Exception)
				{
					return new byte[0];
				}
				finally
				{
					WebPFree(unmanagedData);
				}
			}
			catch (Exception)
			{
				return new byte[0];
			}
			finally
			{
				if(inputGotReplaced && input != null)
					input.Dispose();
			}
		}

		[DllImport("libwebp_a.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int WebPEncodeBGR(IntPtr rgb, int width, int height, int stride, float quality_factor, out IntPtr output);

		[DllImport("libwebp_a.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern int WebPFree(IntPtr p);
	}
}
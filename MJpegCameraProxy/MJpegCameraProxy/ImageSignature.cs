using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class ImageSignature
	{
		public int length;
		public byte[] signature;
		public ImageSignature(byte[] bytes)
		{
			if (bytes == null)
				length = 0;
			else
				length = bytes.Length;

			signature = new byte[100];

			if (length > 0)
				for (int i = 0; i < signature.Length; i++)
					signature[i] = bytes[GetBytesIndex(i)];
		}
		private int GetBytesIndex(int signatureIndex)
		{
			int idx = (length / 100) * signatureIndex;
			if (idx < 0)
				idx = 0;
			if (idx >= length)
				idx = length - 1;
			return idx;
		}
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType() != typeof(ImageSignature))
				return false;
			ImageSignature other = (ImageSignature)obj;
			if (length != other.length)
				return false;
			for (int i = 0; i < signature.Length; i++)
				if (signature[i] != other.signature[i])
					return false;
			return true;
		}
		public override int GetHashCode()
		{
			return (length << 8) + signature[50];
		}
	}
}

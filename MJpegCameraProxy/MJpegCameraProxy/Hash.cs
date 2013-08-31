using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace MJpegCameraProxy
{
	public class Hash
	{
		public static byte[] GetSHA1Bytes(string s)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			SHA1 sha = new SHA1CryptoServiceProvider();
			byte[] result = sha.ComputeHash(data);
			return result;
		}
		public static string GetSHA1Hex(string s)
		{
			return BitConverter.ToString(GetSHA1Bytes(s)).Replace("-", "").ToLower();
		}
		public static byte[] GetMD5Bytes(string s)
		{
			byte[] data = UTF8Encoding.UTF8.GetBytes(s);
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] result = md5.ComputeHash(data);
			return result;
		}
		public static string GetMD5Hex(string s)
		{
			return BitConverter.ToString(GetMD5Bytes(s)).Replace("-", "").ToLower();
		}
	}
}

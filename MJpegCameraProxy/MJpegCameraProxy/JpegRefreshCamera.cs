using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace MJpegCameraProxy
{
	public class JpegRefreshCamera : IPCameraBase
	{
		public string url, user, pass, ip;
		public JpegRefreshCamera(string url, string user, string pass)
		{
			this.url = url;
			this.user = user;
			this.pass = pass;
			Match m = Regex.Match(url, "://(.*?)/");
			if (m.Success)
				ip = m.Groups[1].Value;
			else
				ip = "";
		}

		protected override void DoBackgroundWork()
		{
			while (!Exit)
			{
				try
				{
					byte[] newFrame = SimpleProxy.GetData(url, user, pass, true);
					if (!ArraysLooselyMatch(newFrame, lastFrame))
						this.lastFrame = newFrame;
				}
				catch (ThreadAbortException)
				{
					return;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					Thread.Sleep(5000);
				}
			}
		}
		private bool ArraysLooselyMatch(byte[] one, byte[] two)
		{
			if (one == null && two == null)
				return true;
			else if (one == null || two == null)
				return false;

			if (one.Length != two.Length)
				return false;

			int incrementAmount = 1000;
			if (one.Length < 100)
				incrementAmount = 1;
			else if (one.Length < 1000)
				incrementAmount = 10;
			else if (one.Length < 10000)
				incrementAmount = 100;

			for (int i = 0; i < one.Length; i += incrementAmount)
			{
				if (one[i] != two[i])
					return false;
			}
			return true;
		}
	}
}

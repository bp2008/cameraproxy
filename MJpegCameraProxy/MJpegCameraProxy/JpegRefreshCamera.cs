using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using BPUtil;

namespace MJpegCameraProxy
{
	public class JpegRefreshCamera : IPCameraBase
	{
		internal JpegRefreshCamera()
		{
		}

		protected override void DoBackgroundWork()
		{
			CookieAwareWebClient wc = new CookieAwareWebClient();
			wc.Proxy = null;
			if (!string.IsNullOrEmpty(cameraSpec.username) || !string.IsNullOrEmpty(cameraSpec.password))
			{
				wc.Credentials = new NetworkCredential(cameraSpec.username, cameraSpec.password);
				//string authInfo = cameraSpec.username + ":" + cameraSpec.password;
				//authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
				//wc.Headers["Authorization"] = "Basic " + authInfo;
			}
			while (!Exit)
			{
				try
				{
					System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
					timer.Start();

					byte[] newFrame = wc.DownloadData(cameraSpec.imageryUrl);//SimpleProxy.GetData(cameraSpec.imageryUrl, cameraSpec.username, cameraSpec.password, true);
					if (!ArraysLooselyMatch(newFrame, lastFrame))
					{
						this.lastFrame = newFrame;
						EventWaitHandle oldWaitHandle = newFrameWaitHandle;
						newFrameWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
						oldWaitHandle.Set();
					}
					int diff = cameraSpec.delayBetweenImageGrabs - (int)timer.ElapsedMilliseconds;
					while (diff > 0 && !Exit)
					{
						int timeToWait = Math.Max(250, diff);
						Thread.Sleep(timeToWait);
						diff = cameraSpec.delayBetweenImageGrabs - (int)timer.ElapsedMilliseconds;
					}
					if (newFrame.Length == 0 && cameraSpec.delayBetweenImageGrabs < 1000)
					{
						// Prevent rapid attempts if image failed to load
						int ctr = 0;
						while (ctr++ < 10)
							Thread.Sleep(100);
					}
					timer.Stop();
				}
				catch (ThreadAbortException)
				{
					newFrameWaitHandle.Set();
					return;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					newFrameWaitHandle.Set();
					if (!Exit)
						Thread.Sleep(5000);
				}
			}
			newFrameWaitHandle.Set();
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

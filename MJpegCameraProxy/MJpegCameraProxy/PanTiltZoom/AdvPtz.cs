using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace MJpegCameraProxy.PanTiltZoom
{
	public class AdvPtz
	{
		private static ConcurrentDictionary<string, AdvPtz> ptzList = new ConcurrentDictionary<string, AdvPtz>();

		public DahuaPTZ dahuaPtz;

		public AdvPtz(DahuaPTZ dahuaPtz)
		{
			this.dahuaPtz = dahuaPtz;
		}

		public static void AssignPtzObj(Configuration.CameraSpec cs)
		{
			SetPtzObj(cs.id, new AdvPtz(new DahuaPTZ(cs)));
		}

		public static AdvPtz GetPtzObj(string cameraId)
		{
			AdvPtz obj;
			if (ptzList.TryGetValue(cameraId, out obj))
				return obj;
			return null;
		}
		private static void SetPtzObj(string cameraId, AdvPtz obj)
		{
			ptzList.AddOrUpdate(cameraId, obj, (s, old) => 
			{
				old.dahuaPtz.Shutdown(false);
				return obj; 
			});
		}

		public static void Stop()
		{
			lock (ptzList)
			{
				foreach (AdvPtz ptzObj in ptzList.Values)
					ptzObj.dahuaPtz.Shutdown(false);
			}
		}
	}
}

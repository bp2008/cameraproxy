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

		public AdvPtzController ptzController;

		public AdvPtz(AdvPtzController ptzController)
		{
			this.ptzController = ptzController;
		}

		public static void AssignPtzObj(Configuration.CameraSpec cs)
		{
			if (cs.ptzType == Configuration.PtzType.Dahua)
				SetPtzObj(cs.id, new AdvPtz(new Dahua.DahuaPTZ(cs)));
			else if (cs.ptzType == Configuration.PtzType.Hikvision)
				SetPtzObj(cs.id, new AdvPtz(new Hikvision.HikvisionPTZ(cs)));
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
				old.ptzController.Shutdown(false);
				return obj;
			});
		}

		public static void Stop()
		{
			lock (ptzList)
			{
				foreach (AdvPtz ptzObj in ptzList.Values)
					ptzObj.ptzController.Shutdown(false);
			}
		}
	}
	public enum Direction
	{
		Up, Down, Left, Right, LeftUp, RightUp, LeftDown, RightDown
	}
	public class DoubleVector3
	{
		public double X = 0;
		public double Y = 0;
		public double Z = 0;
		public DoubleVector3()
		{
		}
		public DoubleVector3(double X, double Y, double Z)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}
		public DoubleVector3 Copy()
		{
			return new DoubleVector3(X, Y, Z);
		}
	}
	public class FloatVector3
	{
		public float X = 0;
		public float Y = 0;
		public float Z = 0;
		public FloatVector3()
		{
		}
		public FloatVector3(float X, float Y, float Z)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}
		public FloatVector3 Copy()
		{
			return new FloatVector3(X, Y, Z);
		}
	}
	public class IntVector3
	{
		public int X = 0;
		public int Y = 0;
		public int Z = 0;
		public IntVector3()
		{
		}
		public IntVector3(int X, int Y, int Z)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}
		public IntVector3 Copy()
		{
			return new IntVector3(X, Y, Z);
		}
	}
	public class Pos3d
	{
		public float X = 0;
		public float Y = 0;
		public float W = 0;
		public float H = 0;
		public bool zoomIn = false;
		public Pos3d()
		{
		}
		public Pos3d(float X, float Y, float W, float H, bool zoomIn)
		{
			this.X = X;
			this.Y = Y;
			this.W = W;
			this.H = H;
			this.zoomIn = zoomIn;
		}
	}
	public interface AdvPtzController
	{
		string GetStatusUpdate();
		void PrepareWorkerThreadIfNecessary();
		void SetAbsolutePTZPosition(FloatVector3 pos);
		void Set3DPTZPosition(Pos3d pos);
		void SetZoomPosition(double z);
		void Shutdown(bool isAboutToStart);
		void GeneratePanorama(bool full);
	}
}

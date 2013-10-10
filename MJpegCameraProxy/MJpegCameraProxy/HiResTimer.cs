using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MJpegCameraProxy
{
	public class HiResTimer
	{
		private bool isPerfCounterSupported = false;
		private Int64 frequency = 0;

		// Windows CE native library with QueryPerformanceCounter().
		private const string lib = "Kernel32.dll";
		[DllImport(lib)]
		private static extern int QueryPerformanceCounter(ref Int64 count);
		[DllImport(lib)]
		private static extern int QueryPerformanceFrequency(ref Int64 frequency);

		public HiResTimer()
		{
			// Query the high-resolution timer only if it is supported.
			// A returned frequency of 1000 typically indicates that it is not
			// supported and is emulated by the OS using the same value that is
			// returned by Environment.TickCount.
			// A return value of 0 indicates that the performance counter is
			// not supported.
			int returnVal = QueryPerformanceFrequency(ref frequency);

			if (returnVal != 0 && frequency != 1000)
			{
				// The performance counter is supported.
				isPerfCounterSupported = true;
			}
			else
			{
				// The performance counter is not supported. Use
				// Environment.TickCount instead.
				frequency = 1000;
			}
		}

		private Int64 Frequency
		{
			get
			{
				return frequency;
			}
		}

		private Int64 Value
		{
			get
			{
				if (isPerfCounterSupported)
				{
					// Get the value here if the counter is supported.
					Int64 tickCount = 0;
					QueryPerformanceCounter(ref tickCount);
					return tickCount;
				}
				else
				{
					// Otherwise, use Environment.TickCount.
					return (Int64)Environment.TickCount;
				}
			}
		}

		private Int64 start;
		private bool isRunning = false;
		private double elapsedMillisecondsAtTimeOfStop = 0;
		public double ElapsedMilliseconds
		{
			get
			{
				if (isRunning)
				{
					Int64 timeElapsedInTicks = Value - start;
					return (timeElapsedInTicks * 1000) / Frequency;
				}
				else
					return elapsedMillisecondsAtTimeOfStop;
			}
		}
		public void Start()
		{
			start = Value;
			isRunning = true;
		}
		public void Stop()
		{
			if (!isRunning)
				return;
			elapsedMillisecondsAtTimeOfStop = ElapsedMilliseconds;
			isRunning = false;
		}
		public void Reset()
		{
			isRunning = false;
			elapsedMillisecondsAtTimeOfStop = 0;
		}
	}
}

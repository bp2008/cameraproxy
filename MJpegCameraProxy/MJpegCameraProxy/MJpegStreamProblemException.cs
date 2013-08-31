using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	class MJpegStreamProblemException : Exception
	{
		public MJpegStreamProblemException(string p) : base(p)
		{
		}
	}
}

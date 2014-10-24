using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	[Serializable()]
	public class SimpleWwwFile
	{
		public string Key;
		public int Value;
		public SimpleWwwFile()
		{
		}
		public SimpleWwwFile(string fileName, int permission)
		{
			this.Key = fileName;
			this.Value = permission;
		}
	}
}

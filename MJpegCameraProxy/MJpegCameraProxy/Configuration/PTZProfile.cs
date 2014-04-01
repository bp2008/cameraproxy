using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace MJpegCameraProxy.Configuration
{
	[XmlInclude(typeof(PTZSpecV1))]
	public class PTZProfile : SerializableObjectBase
	{
		public PTZSpec spec = null;
		public static object lockObj = new object();
		public string name
		{
			get
			{
				if (spec == null)
					return "";
				return spec.name;
			}
			set
			{
				if (spec != null)
					spec.name = value;
			}
		}

		public static List<PTZProfile> GetPtzProfiles()
		{
			List<PTZProfile> profiles = new List<PTZProfile>();
			DirectoryInfo di = new DirectoryInfo(Globals.PTZProfilesDirectoryBase);
			if (di.Exists)
			{
				FileInfo[] files = di.GetFiles("*.xml");
				foreach (FileInfo fi in files)
				{
					PTZProfile p = new PTZProfile();
					p.Load(fi.FullName);
					profiles.Add(p);
				}
				profiles.Sort(new ComparisonComparer<PTZProfile>((p1, p2) => { return p1.name.CompareTo(p2.name); }));
			}
			return profiles;
		}
	}
}

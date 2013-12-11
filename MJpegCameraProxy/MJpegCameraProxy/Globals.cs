using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MJpegCameraProxy
{
	public static class Globals
	{
		private static string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
		/// <summary>
		/// Gets the full path to the current executable file.
		/// </summary>
		public static string ExecutablePath
		{
			get { return executablePath; }
		}
		private static string applicationRoot = new FileInfo(executablePath).Directory.FullName;
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Does not have trailing '\\'.
		/// </summary>
		public static string ApplicationRoot
		{
			get { return applicationRoot; }
		}
		private static char applicationRootDriveLetter = string.IsNullOrEmpty(applicationRoot) ? 'c' : applicationRoot[0];
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.
		/// </summary>
		public static char ApplicationRootDriveLetter
		{
			get { return applicationRootDriveLetter; }
		}
		private static string applicationDirectoryBase = applicationRoot + "\\";
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Includes trailing '\\'.
		/// </summary>
		public static string ApplicationDirectoryBase
		{
			get { return applicationDirectoryBase; }
		}
		//private static string camsDirectoryBase = applicationRoot + "\\Cams\\";
		///// <summary>
		///// Gets the full path to the Cams directory including the trailing '\\'.  Just add ID!
		///// </summary>
		//public static string CamsDirectoryBase
		//{
		//    get { return camsDirectoryBase; }
		//}
		//private static string usersDirectoryBase = applicationRoot + "\\Users\\";
		///// <summary>
		///// Gets the full path to the Users directory including the trailing '\\'.  Just add user name!
		///// </summary>
		//public static string UsersDirectoryBase
		//{
		//    get { return usersDirectoryBase; }
		//}
		private static string htmlDirectoryBase = applicationRoot + "\\Html\\";
		/// <summary>
		/// Gets the full path to the Html directory including the trailing '\\'.  Just add page name!
		/// </summary>
		public static string HtmlDirectoryBase
		{
			get { return htmlDirectoryBase; }
		}
		private static string thumbsDirectoryBase = applicationRoot + "\\Thumbs\\";
		/// <summary>
		/// Gets the full path to the Thumbs directory including the trailing '\\'.  Just add file name!
		/// </summary>
		public static string ThumbsDirectoryBase
		{
			get { return thumbsDirectoryBase; }
		}
		private static string configFilePath = applicationDirectoryBase + "Config.cfg";
		/// <summary>
		/// Gets the full path to the config file.
		/// </summary>
		public static string ConfigFilePath
		{
			get { return configFilePath; }
		}
		public static string Version = "1.3.3";
	}
}

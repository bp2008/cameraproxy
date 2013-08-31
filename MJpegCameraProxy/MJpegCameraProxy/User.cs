using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MJpegCameraProxy
{
	public class User
	{
		public string un;
		public string pw;

		public User(string un, string pw)
		{
			this.un = un;
			this.pw = pw;
		}
		public static User CreateUser(string name)
		{
			string[] lines = File.ReadAllLines(Globals.UsersDirectoryBase + name);
			if (lines.Length < 2)
				return null;
			string un = "", pw = "";

			if (lines.Length >= 2)
			{
				un = lines[0];
				pw = lines[1];
			}
			User user = new User(un, pw);

			return user;
		}
	}
}

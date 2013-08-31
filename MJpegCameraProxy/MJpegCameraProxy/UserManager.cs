using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace MJpegCameraProxy
{
	public class UserManager
	{
		public ConcurrentDictionary<string, User> users = new ConcurrentDictionary<string, User>();

		public UserManager()
		{
		}

		public User GetUserByName(string name)
		{
			if (name == null)
				return null;
			name = name.ToLower();

			User user = null;
			// Try to get the user reference.
			if (!users.TryGetValue(name, out user))
			{
				// User has not been created yet
				if (name.Contains('\\') || name.Contains('/') || name.Contains('.') || name.Contains(':'))
					return null; // Invalid character in user name.
				if (File.Exists(Globals.UsersDirectoryBase + name))
				{
					// We have a definition file for it.
					user = User.CreateUser(name);
					if (users.TryAdd(name, user))
					{
						// User just got added
					}
					else
					{
						// User was added by another thread
						if (!users.TryGetValue(name, out user))
							return null; // And yet it doesn't exist. Fail.
					}
				}
				else
				{
					// User has not been created yet, and we have no definition file for it.
					return null;
				}
			}
			return user;
		}
	}
}

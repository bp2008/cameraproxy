using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class LoginManager
	{
		Queue<DateTime> lastLogins = new Queue<DateTime>();
		public LoginManager()
		{
		}
		public bool AllowLogin()
		{
			DateTime now = DateTime.Now;
			if (lastLogins.Count < 2)
			{
				lastLogins.Enqueue(now);
				return true;
			}
			if (lastLogins.Peek() < now.Subtract(TimeSpan.FromMinutes(1)))
			{
				lastLogins.Dequeue(); // Oldest login in queue was older than a minute ago
				lastLogins.Enqueue(now);
				return true;
			}
			else
				return false; // Oldest login in queue was newer than a minute ago.
		}
	}
}

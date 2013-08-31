using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy
{
	public class SessionManager
	{
		public SortedList<string, Session> sessions = new SortedList<string, Session>();
		UserManager um = new UserManager();
		public Session AddNewSession(string auth)
		{
			int idxColon = auth.IndexOf(':');
			if (idxColon == -1)
				return null;
			return AddNewSession(auth.Substring(0, idxColon), auth.Substring(idxColon + 1));
		}
		public Session AddNewSession(string user, string pass)
		{
			if (ValidLoginInformation(user, pass))
			{
				Session s = new Session(user, pass);
				lock (sessions)
				{
					sessions.Add(s.sid, s);
				}
				return s;
			}
			return null;
		}
		public Session GetSession(string sid)
		{
			Session s;
			lock (sessions)
			{
				SessionCleanup();
				if (sessions.TryGetValue(sid, out s))
				{
					s.expire = DateTime.Now.AddMinutes(20);
					return s;
				}
			}
			return null;
		}

		public Session GetSession(string sid, string auth)
		{
			Session s = GetSession(sid);
			if (s == null)
				s = AddNewSession(auth);
			if (s != null)
				s.expire = DateTime.Now.AddMinutes(20);
			return s;
		}
		private DateTime nextSessionCleanup = DateTime.Now;
		private void SessionCleanup()
		{
			if (nextSessionCleanup < DateTime.Now)
				return;
			nextSessionCleanup = DateTime.Now.AddMinutes(5);
			List<string> keysToDelete = new List<string>();
			lock (sessions)
			{
				foreach (string key in sessions.Keys)
					if (sessions[key].expire > DateTime.Now)
						keysToDelete.Add(key);
				foreach (string key in keysToDelete)
					sessions.Remove(key);
			}
		}
		private bool ValidLoginInformation(string user, string pass)
		{
			User u = um.GetUserByName(user);
			if (u != null && Hash.GetSHA1Hex(u.pw + "justtomakethingsharder") == pass)
				return true;
			return false;
		}
	}
}

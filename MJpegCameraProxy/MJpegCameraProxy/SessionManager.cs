using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy
{
	public class SessionManager
	{
		public SortedList<string, Session> sessions = new SortedList<string, Session>();
		public Session AddNewSession(string auth, bool passwordIsHashed = true)
		{
			int idxColon = auth.IndexOf(':');
			if (idxColon == -1)
				return new Session("anonymous", 0, 1);
			return AddNewSession(auth.Substring(0, idxColon), auth.Substring(idxColon + 1), passwordIsHashed);
		}
		public Session AddNewSession(string user, string pass, bool passwordIsHashed = true)
		{
			User u = passwordIsHashed ? GetUserIfValid(user, pass) : GetUserIfValid_RawPassword(user, pass);
			if (u != null)
			{
				Session s = new Session(u.name, u.permission, u.sessionLengthMinutes);
				if (u.sessionLengthMinutes > 0)
					lock (sessions)
					{
						sessions.Add(s.sid, s);
					}
				return s;
			}
			return new Session("anonymous", 0, 1);
		}
		public Session GetSession(string sid)
		{
			if (sid == null || sid.Length != 16)
				return null;
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

		public Session GetSession(string sid, string auth, string rawAuth = null)
		{
			if (!string.IsNullOrWhiteSpace(rawAuth))
				return AddNewSession(rawAuth, false);
			Session s = GetSession(sid);
			if (s == null)
				s = AddNewSession(auth);
			//if (s != null)
			s.expire = DateTime.Now.AddMinutes(s.sessionLengthMinutes);
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
		internal void RemoveSession(string sid)
		{
			if (sid == null || sid.Length != 16)
				return;
			lock (sessions)
			{
				sessions.Remove(sid);
			}
		}
		private bool ValidLoginInformation(string user, string pass)
		{
			User u = GetUserIfValid(user, pass);
			return u != null;
		}
		private User GetUserIfValid(string user, string pass)
		{
			User u = MJpegWrapper.cfg.GetUser(user);
			if (u != null && Hash.GetSHA1Hex(u.pass + "justtomakethingsharder") == pass)
				return u;
			return null;
		}
		private User GetUserIfValid_RawPassword(string user, string pass)
		{
			User u = MJpegWrapper.cfg.GetUser(user);
			if (u != null && u.pass == pass)
				return u;
			return null;
		}
	}
}

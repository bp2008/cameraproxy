using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BPUtil;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Configuration
{
	public class ProxyConfig : SerializableObjectBase
	{
		public int webSocketPort = 44454;
		public int webSocketPort_secure = -1;
		public int webport = 44456;
		public int webport_https = -1;

		public CertificateMode certificateMode;
		/// <summary>
		/// Ignored if certificateSource == CertificateMode.SelfSigned
		/// </summary>
		public string certificate_pfx_path = "";
		/// <summary>
		/// Ignored if certificateSource == CertificateMode.SelfSigned
		/// </summary>
		public string certificate_pfx_password = "";

		/// <summary>
		/// The email address to provide to LetsEncrypt.  They may send renewal notices here, but CameraProxy is designed to automatically renew the certificate.
		/// </summary>
		public string letsEncrypt_email = "";
		/// <summary>
		/// One or more domains to authorize and include in the certificate.
		/// </summary>
		public string[] letsEncrypt_domains = new string[] { "" };
		/// <summary>
		/// If set, the challenge files will be written to this directory.  If unset, the challenge responses will be hosted directly by the embedded web server.
		/// </summary>
		public string letsEncrypt_www_root = "";

		public List<User> users = new List<User>();
		public List<CameraSpec> cameras = new List<CameraSpec>();
		public List<SimpleWwwFile> wwwFiles = new List<SimpleWwwFile>();

		public void SetWwwFilesList(List<SimpleWwwFile> list)
		{
			wwwFiles = list;
		}
		public List<SimpleWwwFile> GetWwwFilesList()
		{
			return wwwFiles;
		}

		public string SaveItem(HttpProcessor p)
		{
			bool isNew = p.Request.GetBoolParam("new");
			string originalIdNotLowerCase = p.Request.GetPostParam("itemid");
			string originalId = originalIdNotLowerCase.ToLower();
			string itemtype = p.Request.GetPostParam("itemtype");
			if (itemtype == "camera")
			{
				CameraSpec cs = new CameraSpec();
				string result = cs.setFieldValues(p.Request.RawPostParams);
				if (result.StartsWith("0"))
					return result;
				lock (this)
				{
					if (isNew)
					{
						cs.id = originalId;
						if (CameraIdIsUsed(cs.id))
							return "0A camera with this ID already exists.";
						cameras.Add(cs);
					}
					else
					{
						if (originalId != cs.id && CameraIdIsUsed(cs.id))
							return "0A camera with this ID already exists.";
						bool foundCamera = false;
						for (int i = 0; i < cameras.Count; i++)
							if (cameras[i].id == originalId)
							{
								cs.order = cameras[i].order;
								foundCamera = true;
								MJpegServer.cm.KillCamera(originalId);
								cameras[i] = cs;
								break;
							}
						if (!foundCamera)
							cameras.Add(cs);
					}
					MJpegServer.cm.CleanUpCameraOrder();
					Save(CameraProxyGlobals.ConfigFilePath);
				}
				return result;
			}
			else if (itemtype == "user")
			{
				Configuration.User u = new Configuration.User();
				string result = u.setFieldValues(p.Request.RawPostParams);
				if (result.StartsWith("0"))
					return result;
				lock (this)
				{
					if (isNew)
					{
						u.name = originalId;
						if (UserNameIsUsed(u.name))
							return "0A user with this name already exists.";
						users.Add(u);
					}
					else
					{
						if (originalId != u.name && UserNameIsUsed(u.name))
							return "0A user with this name already exists.";
						bool foundUser = false;
						for (int i = 0; i < users.Count; i++)
							if (users[i].name == originalId)
							{
								foundUser = true;
								users[i] = u;
								break;
							}
						if (!foundUser)
							users.Add(u);
					}
					Save(CameraProxyGlobals.ConfigFilePath);
				}
				return result;
			}
			else if (itemtype == "ptzprofile")
			{
				PTZProfile f = new PTZProfile();
				int version = p.Request.GetPostIntParam("version", 1);
				if (version == 1)
					f.spec = new PTZSpecV1();
				string result = f.spec.setFieldValues(p.Request.RawPostParams);

				if (result.StartsWith("0"))
					return result;
				lock (this)
				{
					if (isNew)
					{
						f.name = originalIdNotLowerCase;
						if (ProfileNameIsUsed(f.name))
							return "0A PTZ Profile with this name already exists.";
						f.Save(CameraProxyGlobals.PTZProfilesDirectoryBase + f.name + ".xml");
					}
					else
					{
						if (originalId != f.name.ToLower() && ProfileNameIsUsed(f.name))
							return "0A PTZ Profile with this name already exists.";
						File.Delete(CameraProxyGlobals.PTZProfilesDirectoryBase + originalId + ".xml");
						f.Save(CameraProxyGlobals.PTZProfilesDirectoryBase + f.name + ".xml");
					}
				}
				return result;
			}
			return "0Invalid item type: " + itemtype;
		}

		private bool ProfileNameIsUsed(string profileName)
		{
			return File.Exists(CameraProxyGlobals.PTZProfilesDirectoryBase + profileName + ".xml");
		}

		private bool CameraIdIsUsed(string cameraId)
		{
			lock (this)
			{
				foreach (CameraSpec spec in cameras)
					if (spec.id == cameraId)
						return true;
			}
			return false;
		}
		private bool UserNameIsUsed(string userName)
		{
			lock (this)
			{
				foreach (Configuration.User u in users)
					if (u.name == userName)
						return true;
			}
			return false;
		}

		public CameraSpec GetCameraSpec(string id)
		{
			lock (this)
			{
				foreach (CameraSpec spec in cameras)
					if (spec.id.ToLower() == id)
						return spec;
			}
			return null;
		}

		public User GetUser(string name)
		{
			lock (this)
			{
				foreach (User u in users)
					if (u.name == name)
						return u;
			}
			return null;
		}

		public string DeleteItems(HttpProcessor p)
		{
			string itemtype = p.Request.GetPostParam("itemtype");
			string ids = p.Request.GetPostParam("ids").ToLower();
			if (ids == null || ids.Length < 1)
				return "0No items were specified for deletion";
			string[] parts = ids.Split(',');
			HashSet<string> hsParts = new HashSet<string>(parts);
			if (itemtype == "camera")
			{
				lock (this)
				{
					cameras.RemoveAll(cs =>
					{
						bool remove = hsParts.Contains(cs.id);
						if (remove)
							MJpegServer.cm.KillCamera(cs.id);
						return remove;
					});
					MJpegServer.cm.CleanUpCameraOrder();
					Save(CameraProxyGlobals.ConfigFilePath);
				}
			}
			else if (itemtype == "user")
			{
				lock (this)
				{
					users.RemoveAll(u =>
					{
						return hsParts.Contains(u.name);
					});
					Save(CameraProxyGlobals.ConfigFilePath);
				}
			}
			else if (itemtype == "ptzprofile")
			{
				lock (this)
				{
					foreach (string s in parts)
						if (ProfileNameIsUsed(s))
							File.Delete(CameraProxyGlobals.PTZProfilesDirectoryBase + s + ".xml");
				}
			}
			else if (itemtype == "wwwfile")
			{
				lock (this)
				{
					wwwFiles.RemoveAll(f =>
					{
						return hsParts.Contains(f.Key) && !File.Exists(CameraProxyGlobals.WWWDirectoryBase + f.Key);
					});
					Save(CameraProxyGlobals.ConfigFilePath);
				}
			}
			return "1";
		}

		public string ReorderCam(HttpProcessor p)
		{
			lock (this)
			{
				string id = p.Request.GetPostParam("id").ToLower();
				if (string.IsNullOrEmpty(id))
					return "0Missing id parameter";
				string dir = p.Request.GetPostParam("dir");
				if (string.IsNullOrEmpty(dir))
					return "0Missing dir parameter";

				int diff = (dir == "up" ? -1 : (dir == "down" ? 1 : 0));

				if (diff == 0)
					return "0Invalid dir parameter";

				bool found = false;
				foreach (CameraSpec spec in cameras)
					if (spec.id.ToLower() == id)
					{
						int oldOrder = spec.order;
						int newOrder = oldOrder + diff;
						foreach (CameraSpec swapWith in cameras)
							if (swapWith.order == newOrder)
								swapWith.order = oldOrder;
						spec.order = newOrder;
						found = true;
						break;
					}
				if (!found)
					return "0Invalid id parameter";

				MJpegServer.cm.CleanUpCameraOrder();

				Save(CameraProxyGlobals.ConfigFilePath);
				return "1";
			}
		}
	}
}

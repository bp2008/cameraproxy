using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	public class ProxyConfig : SerializableObjectBase
	{
		public ushort webport = 44456;

		public List<User> users = new List<User>();
		public List<CameraSpec> cameras = new List<CameraSpec>();

		public string SaveItem(SimpleHttp.HttpProcessor p)
		{
			bool isNew = p.GetBoolParam("new");
			string originalId = p.GetPostParam("itemid");
			string itemtype = p.GetPostParam("itemtype");
			if (itemtype == "camera")
			{
				CameraSpec cs = new CameraSpec();
				string result = cs.setFieldValues(p.RawPostParams);
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
								foundCamera = true;
								MJpegServer.cm.KillCamera(originalId);
								cameras[i] = cs;
								break;
							}
						if (!foundCamera)
							cameras.Add(cs);
					}
					Save(Globals.ConfigFilePath);
				}
				return result;
			}
			else if (itemtype == "user")
			{
				Configuration.User u = new Configuration.User();
				string result = u.setFieldValues(p.RawPostParams);
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
					Save(Globals.ConfigFilePath);
				}
				return result;
			}
			return "0Invalid item type: " + itemtype;
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

		public string DeleteItems(SimpleHttp.HttpProcessor p)
		{
			string itemtype = p.GetPostParam("itemtype");
			string ids = p.GetPostParam("ids");
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
					Save(Globals.ConfigFilePath);
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
					Save(Globals.ConfigFilePath);
				}
			}
			return "1";
		}
	}
}

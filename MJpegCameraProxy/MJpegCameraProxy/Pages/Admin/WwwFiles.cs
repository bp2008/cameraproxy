using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using MJpegCameraProxy.Configuration;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Pages.Admin
{
	class WwwFiles : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			SortedList<string, int> wwwFileSortedList = new SortedList<string, int>();
			// Add configured files to list
			lock (MJpegWrapper.cfg)
			{
				foreach (var item in MJpegWrapper.cfg.GetWwwFilesList())
					wwwFileSortedList.Add(item.Key, item.Value);
			}
			// Add filesystem files to list
			HashSet<string> existingFiles = new HashSet<string>();
			DirectoryInfo diWWW = new DirectoryInfo(Globals.WWWDirectoryBase);
			string directoryBase = Globals.WWWDirectoryBase.ToLower().Replace('\\', '/');
			foreach (FileInfo fi in diWWW.GetFiles("*", SearchOption.AllDirectories))
			{
				string fileName = fi.FullName.ToLower().Replace('\\', '/');
				if (!fileName.StartsWith(directoryBase))
					continue;
				fileName = fileName.Substring(directoryBase.Length);
				existingFiles.Add(fileName);
				if (!wwwFileSortedList.ContainsKey(fileName))
					wwwFileSortedList.Add(fileName, 100);
			}
			// Create list of WwwFile so we can easily list them in a table.
			List<WwwFile> wwwFileList = new List<WwwFile>();
			List<SimpleWwwFile> simpleWwwFileList = new List<SimpleWwwFile>();
			foreach (var item in wwwFileSortedList)
			{
				wwwFileList.Add(new WwwFile(item.Key, item.Value, existingFiles.Contains(item.Key)));
				simpleWwwFileList.Add(new SimpleWwwFile(item.Key, item.Value));
			}

			// Save the current files list
			MJpegWrapper.cfg.SetWwwFilesList(simpleWwwFileList);
			MJpegWrapper.cfg.Save(Globals.ConfigFilePath);

			ItemTable<WwwFile> tbl = new ItemTable<WwwFile>("Web Server Files", "wwwfile", "fileName", wwwFileList, wwwFileList, ItemTableMode.Save, new ItemTableColumnDefinition<WwwFile>[]
			{
				new ItemTableColumnDefinition<WwwFile>("File", c => { return (!c.verifiedExisting ? "<span style=\"color:Red\" title=\"Files in red no longer exist, but they are configured with a stored permission\" >" : "") + HttpUtility.HtmlEncode(c.fileName) + (!c.verifiedExisting ? "</span>" : ""); }),
				new ItemTableColumnDefinition<WwwFile>("Permission Required", c => { return "<input saveitemfield=\"permission\" saveitemkey=\"" + HttpUtility.JavaScriptStringEncode(c.fileName) + "\" type=\"number\" min=\"0\" max=\"100\" value=\"" + (c.permission > 100 ? 100 : (c.permission < 0 ? 0 : c.permission)) + "\" />"; })
			});
			return tbl.GetSectionHtml() + "<div>* Note: Only files with a red name can be deleted from this list.  Files have a red name when they no longer exist in the www directory.</div>";
		}
		internal override void HandleSave(HttpProcessor p, Session s, SortedList<string, SortedList<string, string>> items)
		{
			SortedList<string, int> newWWWFiles = new SortedList<string, int>();
			foreach (var item in items)
			{
				int permission;
				if (int.TryParse(item.Value["permission"], out permission))
					newWWWFiles.Add(item.Key.ToLower(), permission);
			}
			List<SimpleWwwFile> wwwFileList = new List<SimpleWwwFile>();
			foreach (var item in newWWWFiles)
				wwwFileList.Add(new SimpleWwwFile(item.Key, item.Value));
			lock (MJpegWrapper.cfg)
			{
				MJpegWrapper.cfg.SetWwwFilesList(wwwFileList);
				MJpegWrapper.cfg.Save(Globals.ConfigFilePath);
			}
		}
	}
	class WwwFile
	{
		public string fileName;
		public int permission;
		public bool verifiedExisting;
		public WwwFile(string fileName, int permission, bool verifiedExisting)
		{
			this.fileName = fileName;
			this.permission = permission;
			this.verifiedExisting = verifiedExisting;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Web;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Pages.Admin
{
	class PTZProfileList : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			ItemTable<PTZProfile> tbl = new ItemTable<PTZProfile>("PTZ Profiles", "ptzprofile", "name", PTZProfile.GetPtzProfiles(), PTZProfile.lockObj, ItemTableMode.Add, new ItemTableColumnDefinition<PTZProfile>[]
			{
				new ItemTableColumnDefinition<PTZProfile>("Name", c => { return "<a href=\"javascript:EditItem('" + HttpUtility.JavaScriptStringEncode(c.spec.name) + "')\">" + HttpUtility.HtmlEncode(c.spec.name) + "</a>"; })
			});
			return tbl.GetSectionHtml();
		}
	}
}

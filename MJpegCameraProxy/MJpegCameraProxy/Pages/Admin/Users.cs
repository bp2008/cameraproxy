using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MJpegCameraProxy.Pages.Admin
{
	class Users : AdminBase
	{
		protected override string GetPageHtml(SimpleHttp.HttpProcessor p, Session s)
		{
			ItemTable<Configuration.User> tbl = new ItemTable<Configuration.User>("Users", "user", "name", MJpegWrapper.cfg.users, MJpegWrapper.cfg, ItemTableMode.Add, new ItemTableColumnDefinition<Configuration.User>[]
			{
				new ItemTableColumnDefinition<Configuration.User>("Name", u => { return "<a href=\"javascript:EditItem('" + u.name + "')\">" + HttpUtility.HtmlEncode(u.name) + "</a>"; }),
				new ItemTableColumnDefinition<Configuration.User>("Permission", u => { return u.permission.ToString(); }),
				new ItemTableColumnDefinition<Configuration.User>("Session Length (Minutes)", u => { return u.sessionLengthMinutes.ToString(); })
			});
			return tbl.GetSectionHtml();
		}
	}
}

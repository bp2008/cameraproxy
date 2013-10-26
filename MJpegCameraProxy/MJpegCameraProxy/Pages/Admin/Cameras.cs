using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using MJpegCameraProxy.Configuration;

namespace MJpegCameraProxy.Pages.Admin
{
	class Cameras : AdminBase
	{
		protected override string GetPageHtml(SimpleHttp.HttpProcessor p, Session s)
		{
			ItemTable<CameraSpec> tbl = new ItemTable<CameraSpec>("Cameras", "camera", "id", MJpegWrapper.cfg.cameras, MJpegWrapper.cfg, new ItemTableColumnDefinition<CameraSpec>[]
			{
				new ItemTableColumnDefinition<CameraSpec>(" ", c => { return "<a href=\"../" + c.id + ".cam\"><img src=\"../" + c.id + ".jpg?maxwidth=40&maxheight=40&nocache=" + DateTime.Now.ToBinary().ToString() + "\" alt=\"[img]\" /></a>"; }),
				new ItemTableColumnDefinition<CameraSpec>("Name", c => { return "<a href=\"javascript:EditItem('" + c.id + "')\">" + HttpUtility.HtmlEncode(c.name) + "</a>"; }),
				new ItemTableColumnDefinition<CameraSpec>("ID", c => { return c.id; }),
				new ItemTableColumnDefinition<CameraSpec>("Enabled", c => { return c.enabled ? ("<span style=\"color:Green;\">Enabled</span>") : "<span style=\"color:Red;\">Disabled</span>"; }),
				new ItemTableColumnDefinition<CameraSpec>("Type", c => { return c.type.ToString() + (c.type == CameraType.h264_rtsp_proxy ? ":" + c.h264_port : ""); }),
				new ItemTableColumnDefinition<CameraSpec>("Ptz", c => { return c.ptzType.ToString(); }),
				new ItemTableColumnDefinition<CameraSpec>("TTL", c => { return c.maxBufferTime.ToString(); }),
				new ItemTableColumnDefinition<CameraSpec>("Permission", c => { return c.minPermissionLevel.ToString(); }),
				new ItemTableColumnDefinition<CameraSpec>("Url", c => { return c.imageryUrl; }),
				new ItemTableColumnDefinition<CameraSpec>("Order", c => { return "<a href=\"javascript:void(0)\" onclick=\"$.post('reordercam', { dir: 'up', id: '" + c.id + "' }).done(function(){location.href=location.href;});\">Up</a><br/><a href=\"javascript:void(0)\" onclick=\"$.post('reordercam', { dir: 'down', id: '" + c.id + "' }).done(function(){location.href=location.href;});\">Down</a>"; })
			});
			return tbl.GetSectionHtml();
		}
	}
}

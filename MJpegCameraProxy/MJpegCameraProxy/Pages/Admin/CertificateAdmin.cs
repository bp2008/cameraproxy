using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Pages.Admin
{
	class CertificateAdmin : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			StringBuilder sb = new StringBuilder();
			WriteInputHeading(sb, "Certificate Mode");
			sb.AppendLine("<div>" + MJpegWrapper.cfg.certificateMode + "</div>");
			WriteInputHeading(sb, "Certificate Path (pfx file)<i> -- Ignored for Self Signed certificate mode.</i>");
			sb.AppendLine("<div>" + MJpegWrapper.cfg.certificate_pfx_path + "</div>");
			WriteInputHeading(sb, "Certificate Password<i> -- Ignored for Self Signed certificate mode.</i>");
			sb.AppendLine("<div>" + MJpegWrapper.cfg.certificate_pfx_password + "</div>");
			return sb.ToString();
		}
		protected void WriteInputHeading(StringBuilder sb, string text)
		{
			sb.AppendLine("<div>" + HttpUtility.HtmlEncode(text) + "</div>");
		}
	}
}

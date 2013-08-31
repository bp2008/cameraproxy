using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MJpegCameraProxy
{
	public class Login
	{
		public static string GetString(string LoginURL)
		{
			return @"<!DOCTYPE HTML>
<html>
<head>
	<title>Log In</title>
	<script type=""text/javascript"" src=""Scripts/jquery.js""></script>
	<script type=""text/javascript"" src=""Scripts/sha1.js""></script>
	<script type=""text/javascript"">
		function keyPressed(e)
		{
			if (e.keyCode == 13)
			{
				tryLogin();
				return false;
			}
			return true;
		}
		function tryLogin()
		{
			$(""#errortext"").css('color', '#000000');
			$(""#errortext"").html('Logging in...');
			var username = $(""#username"").val();
			var password = $(""#password"").val() + ""justtomakethingsharder"";
			setCookie('auth', escape(username) + ':' + escape(HashPW(password)), 1000);
			top.location.href = unescape('" + HttpUtility.JavaScriptStringEncode(HttpUtility.UrlEncode(LoginURL)) + @"');
		}
		function HashPW(pw)
		{
			var shaObj = new jsSHA(pw, ""ASCII"");
			var hash = shaObj.getHash(""HEX"");
			return hash;
		}
		function setCookie(c_name, value, exdays)
		{
			var exdate = new Date();
			exdate.setDate(exdate.getDate() + exdays);
			var c_value = escape(value) + (exdays==null ? '' : '; expires=' + exdate.toUTCString());
			document.cookie = c_name + '=' + c_value;
		}
	</script>
</head>
<body>
<div id=""loginBox"">
		<table style=""border-collapse: collapse;"">
			<tr>
				<td>
					<label for=""username"">User:</label>
				</td>
				<td>
					<input id=""username"" type=""text"" />
				</td>
			</tr>
			<tr>
				<td>
					<label for=""password"">Pass:</label>
				</td>
				<td>
					<input id=""password"" type=""password"" onkeypress=""return keyPressed(event);"" />
				</td>
			</tr>
		</table>
		<br /><input type=""button"" value=""Log In"" style=""margin-left: 40px;"" onclick=""tryLogin(); return false;"" />
		<br />
		<br />
		<div id=""errortext"">&nbsp; &nbsp;</div>
	</div>
</body>
</html>
";
		}
	}
}

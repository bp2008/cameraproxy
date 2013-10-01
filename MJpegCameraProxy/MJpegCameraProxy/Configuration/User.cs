﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MJpegCameraProxy.Configuration
{
	/// <summary>
	/// An object containing information about a user, such as the user name, the password, the permission level, and the session length.
	/// </summary>
	[EditorName("User Configuration")]
	public class User : FieldSettable
	{
		[EditorName("User Name")]
		[EditorHint("Alphanumeric only, no whitespace allowed")]
		public string name = "newuser";
		[IsPasswordField(true)]
		[EditorName("Password")]
		public string pass = "";
		[EditorName("Permission Level")]
		[EditorHint("0 to 100.  A value of 0 means the user has no more rights than an anonymous user.")]
		public int permission = 10;
		[EditorName("Session Length in Minutes")]
		[EditorHint("Must be greater than 0.<br/>This does not really affect functionality.  It is simply an optimization.")]
		public int sessionLengthMinutes = 1440;

		public User()
		{
		}
		public User(string user, string pass, int permission, int sessionLengthMinutes = 1440)
		{
			this.name = user;
			this.pass = pass;
			this.permission = permission;
			this.sessionLengthMinutes = sessionLengthMinutes;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace MJpegCameraProxy.Configuration
{
	public class EditorNameAttribute : Attribute
	{
		public readonly string name;
		public EditorNameAttribute(string name)
		{
			this.name = name;
		}
		public override string ToString()
		{
			return name;
		}
	}
	public class IsPasswordField : Attribute
	{
		public readonly bool isPassword;
		public IsPasswordField(bool isPassword)
		{
			this.isPassword = isPassword;
		}
		public override string ToString()
		{
			return isPassword ? "1" : "0";
		}
	}
	public class EditorCategory : Attribute
	{
		public readonly string name;
		public EditorCategory(string name)
		{
			this.name = name;
		}
		public override string ToString()
		{
			return name;
		}
	}
	public class EditorHint : Attribute
	{
		public readonly string text;
		public EditorHint(string text)
		{
			this.text = text;
		}
		public override string ToString()
		{
			return text;
		}
	}
	public class EditorCondition_FieldMustBe : Attribute
	{
		public readonly string fieldName;
		public readonly object[] acceptableValues;
		/// <summary>
		/// This condition is met if the object has a field with the specified name, and that field's value appears in the acceptableValues array.
		/// </summary>
		/// <param name="fieldName">The name of the field which must appear.</param>
		/// <param name="acceptableValues">All possible values of this field which lead to the condition being met.</param>
		public EditorCondition_FieldMustBe(string fieldName, params object[] acceptableValues)
		{
			this.fieldName = fieldName;
			this.acceptableValues = acceptableValues;
		}
	}
}

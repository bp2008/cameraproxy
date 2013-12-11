using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace MJpegCameraProxy.Configuration
{
	public abstract class SerializableObjectBase
	{
		public bool Save(string filePath = null)
		{
			try
			{
				lock (this)
				{
					if (filePath == null)
						filePath = this.GetType().Name + ".cfg";
					System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
					using (FileStream fs = new FileStream(filePath, FileMode.Create))
						x.Serialize(fs, this);
				}
				return true;
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return false;
		}
		public bool Load(string filePath = null)
		{
			try
			{
				Type thistype = this.GetType();
				if (filePath == null)
					filePath = thistype.Name + ".cfg";
				lock (this)
				{
					if (!File.Exists(filePath))
						return false;
					System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
					object obj;
					using (FileStream fs = new FileStream(filePath, FileMode.Open))
						obj = x.Deserialize(fs);
					foreach (FieldInfo sourceField in obj.GetType().GetFields())
					{
						try
						{
							FieldInfo targetField = thistype.GetField(sourceField.Name);
							if (targetField != null && targetField.MemberType == sourceField.MemberType)
								targetField.SetValue(this, sourceField.GetValue(obj));
						}
						catch (Exception) { }
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return false;
		}
	}
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

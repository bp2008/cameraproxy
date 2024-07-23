using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MJpegCameraProxy.Configuration
{
	public abstract class FieldSettable
	{
		protected string SetFieldValue(string fieldName, string fieldValue)
		{
			if (string.IsNullOrWhiteSpace(fieldName))
				return "0Field name not specified.";
			if(fieldValue == null)
				return "0Field \"" + fieldName + "\" value not specified.";
			FieldInfo fi = this.GetType().GetField(fieldName);
			if (fi == null)
				return "1"; // return "0Field \"" + fieldName + "\" not found.";
			if (fi.FieldType == typeof(string))
				fi.SetValue(this, fieldValue);
			else if (fi.FieldType == typeof(int))
			{
				int intValue;
				if (int.TryParse(fieldValue, out intValue))
					fi.SetValue(this, intValue);
				else
					return "0Invalid value received for field \"" + fieldName + "\". (" + fieldValue + ")";
			}
			else if (fi.FieldType == typeof(float))
			{
				float floatValue;
				if (float.TryParse(fieldValue, out floatValue))
					fi.SetValue(this, floatValue);
				else
					return "0Invalid value received for field \"" + fieldName + "\". (" + fieldValue + ")";
			}
			else if (fi.FieldType == typeof(double))
			{
				double doubleValue;
				if (double.TryParse(fieldValue, out doubleValue))
					fi.SetValue(this, doubleValue);
				else
					return "0Invalid value received for field \"" + fieldName + "\". (" + fieldValue + ")";
			}
			else if (fi.FieldType == typeof(ushort))
			{
				ushort ushortValue;
				if (ushort.TryParse(fieldValue, out ushortValue))
					fi.SetValue(this, ushortValue);
				else
					return "0Invalid value received for field \"" + fieldName + "\". (" + fieldValue + ")";
			}
			else if (fi.FieldType == typeof(bool))
			{
				fi.SetValue(this, fieldValue == "1");
			}
			else if (fi.FieldType.IsSubclassOf(typeof(Enum)))
			{
				try
				{
					var enumValue = Enum.Parse(fi.FieldType, fieldValue);
					fi.SetValue(this, enumValue);
				}
				catch (Exception)
				{
					return "0Can't give \"" + fieldName + "\" the value " + fieldValue + ".";
				}
			}
			else
			{
				return "0Field \"" + fieldName + "\" has unsupported type (" + fi.FieldType.ToString() + ").";
			}
			return "1";
		}
		public string setFieldValues(IDictionary<string, string> args)
		{
			string result;
			foreach (KeyValuePair<string, string> kvp in args)
			{
				result = SetFieldValue(kvp.Key, kvp.Value);
				if (result.StartsWith("0"))
					return result;
			}
			return validateFieldValues();
		}
		protected virtual string validateFieldValues()
		{
			return "1";
		}
	}
}

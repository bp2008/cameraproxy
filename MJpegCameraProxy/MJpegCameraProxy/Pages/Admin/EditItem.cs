using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Reflection;
using System.Web;

namespace MJpegCameraProxy.Pages.Admin
{
	class EditItem : AdminBase
	{
		StringBuilder sb = new StringBuilder();
		protected override string GetPageHtml(SimpleHttp.HttpProcessor p, Session s)
		{
			sb.Clear();
			string itemtype = p.GetParam("itemtype");
			string itemid = p.GetParam("itemid").ToLower();
			sb.Append("<div id=\"itemtype\" itemtype=\"").Append(itemtype).Append("\" style=\"display:none;\"></div>");
			sb.Append("<div id=\"itemid\" itemid=\"").Append(itemid).Append("\" style=\"display:none;\"></div>");
			sb.Append("<div id=\"itemfields\">");
			if (itemtype == "camera")
			{
				sb.AppendLine("<div style=\"display:none;\" id=\"pageToLoadWhenFinished\" page=\"cameras\"></div>");
				bool foundCamera = false;
				lock (MJpegWrapper.cfg)
				{
					foreach (CameraSpec cs in MJpegWrapper.cfg.cameras)
						if (cs.id == itemid)
						{
							foundCamera = true;
							CreateItemEditor(cs);
							break;
						}
				}
				if (!foundCamera)
					sb.Append("Could not find camera");
				return sb.ToString();
			}
			else if (itemtype == "user")
			{
				sb.AppendLine("<div style=\"display:none;\" id=\"pageToLoadWhenFinished\" page=\"users\"></div>");
				bool foundUser = false;
				lock (MJpegWrapper.cfg)
				{
					foreach (Configuration.User u in MJpegWrapper.cfg.users)
						if (u.name == itemid)
						{
							foundUser = true;
							CreateItemEditor(u);
							break;
						}
				}
				if (!foundUser)
					sb.Append("Could not find user");
				return sb.ToString();
			}
			else
			{
				sb.AppendLine("<div style=\"display:none;\" id=\"pageToLoadWhenFinished\" page=\"main\"></div>");
			}
			sb.Append("</div>");
			return sb.ToString();
		}
		private void CreateHiddenTextInputField(string fieldName, string fieldValue)
		{
			sb.Append("<input style=\"display:none;\" type=\"text\" fieldname=\"" + HttpUtility.HtmlAttributeEncode(fieldName) + "\" value=\"" + HttpUtility.HtmlAttributeEncode(fieldValue) + "\" />");
		}
		private void CreateItemEditor(object obj)
		{
			sb.Append(GetItemEditorScripts());
			SectionTitle(sb, GetAttributeValue(obj.GetType(), typeof(Configuration.EditorNameAttribute), "Item Editor"));
			bool inTable = false;

			foreach (FieldInfo fi in obj.GetType().GetFields().OrderBy(field => field.MetadataToken))
			{
				string displayName = GetAttributeValue(fi, typeof(Configuration.EditorNameAttribute));
				if (displayName != null)
				{
					string hint = GetAttributeValue(fi, typeof(Configuration.EditorHint), "");
					if (!string.IsNullOrWhiteSpace(hint))
						hint = " <span style=\"color: #666666; font-style: italic\">" + hint + "</span>";

					string category = GetAttributeValue(fi, typeof(Configuration.EditorCategory));

					if (category != null)
					{
						if (inTable)
							SectionTableEnd(sb);
						sb.Append(GetConditionString(fi));
						SectionTitle(sb, category);
						if (inTable)
							SectionTableStart(sb);
					}

					if (!inTable)
					{
						SectionTableStart(sb);
						inTable = true;
					}

					if (fi.FieldType == typeof(string))
					{
						string inputtype = "text";
						if (GetAttributeValue(fi, typeof(Configuration.IsPasswordField)) == "1")
							inputtype = "password";

						SectionTableEntry(sb, displayName, "<input style=\"width:95%\" type=\"" + inputtype + "\" fieldname=\"" + fi.Name + "\" value=\"" + HttpUtility.HtmlAttributeEncode(ToString(fi.GetValue(obj))) + "\" />" + hint);
					}
					else if (fi.FieldType == typeof(int))
						SectionTableEntry(sb, displayName, "<input style=\"width:100px\" type=\"number\" fieldname=\"" + fi.Name + "\" value=\"" + ToString(fi.GetValue(obj)) + "\" />" + hint);
					else if (fi.FieldType == typeof(ushort))
						SectionTableEntry(sb, displayName, "<input style=\"width:100px\" type=\"number\" min=\"0\" max=\"65535\" fieldname=\"" + fi.Name + "\" value=\"" + ToString(fi.GetValue(obj)) + "\" />" + hint);
					else if (fi.FieldType == typeof(bool))
						SectionTableEntry(sb, "<label for=\"" + fi.Name + "\">" + displayName + "</label>", "<input type=\"checkbox\" id=\"" + fi.Name + "\" fieldname=\"" + fi.Name + "\"" + (((bool)fi.GetValue(obj)) ? " checked=\"checked\"" : "") + " />" + hint);
					else if (fi.FieldType.IsSubclassOf(typeof(Enum)))
					{
						MemberInfo[] memberInfos = fi.FieldType.GetMembers(BindingFlags.Public | BindingFlags.Static);
						StringBuilder sbSelect = new StringBuilder();
						sbSelect.Append("<select fieldname=\"" + fi.Name + "\">");
						for (int i = 0; i < memberInfos.Length; i++)
						{
							sbSelect.Append("<option value=\"").Append(HttpUtility.HtmlAttributeEncode(memberInfos[i].Name)).Append("\"");
							sbSelect.Append(ToString(fi.GetValue(obj)) == memberInfos[i].Name ? " selected=\"selected\"" : "");
							sbSelect.Append(">").Append(HttpUtility.HtmlEncode(memberInfos[i].Name)).Append("</option>");
						}
						sbSelect.Append("</select>" + hint);
						SectionTableEntry(sb, displayName, ToString(sbSelect));
					}
					else
						SectionTableEntry(sb, displayName, "Unsupported Field Type (" + fi.GetType() + ")");
				}
			}
			if (inTable)
			{
				inTable = false;
				SectionTableEnd(sb);
			}

			SectionTitle(sb, "");
			SectionStart_RightAligned(sb);
			sb.Append("<span id=\"errorMessage\" style=\"color: Red;\"></span> &nbsp; ");
			sb.Append("<input id=\"saveBtn\" type=\"button\" value=\"Save\" onclick=\"SaveItem();return false;\" />");
			sb.Append(" &nbsp; ");
			sb.Append("<input id=\"cancelBtn\" type=\"button\" value=\"Cancel\" onclick=\"CancelBtn_Click();return false;\" />");
			SectionEnd(sb);
		}

		private string GetItemEditorScripts()
		{
			return @"<script type=""text/javascript"">
	var itemtype;
	var itemid;
	$(function()
	{
		itemtype = $('#itemtype').attr('itemtype');
		itemid = $('#itemid').attr('itemid');
		ConfigureCollapsibleSections();
		ListenForChangesToConditions();
		EvaluateConditions();
	});
	function SaveItem()
	{
		$('#saveBtn, #cancelBtn').unbind('click');
		$('#saveBtn').val('Saving...');
		var fields = $('#itemfields');
		var inputs = fields.find('input, select');
		var parameters = new Object();
		inputs.each(function(idx, ele)
		{
			var thisInput = $(ele);
			if(thisInput.is(':checkbox'))
			{
				parameters[thisInput.attr('fieldname')] = thisInput.is(':checked') ? '1' : '0';
			}
			else
				parameters[thisInput.attr('fieldname')] = thisInput.val();
		});
		parameters.itemid = itemid;
		parameters.itemtype = itemtype;
		$.post('saveitem', parameters).done(gotSaveItem).fail(failedSaveItem);
	}
	function gotSaveItem(data, textStatus, jqXHR)
	{
		$('#errorMessage').html('');
		if(data[0] == '1')
			location.href = $('#pageToLoadWhenFinished').attr('page');
		else
		{
			$('#saveBtn').val('Save');
			$('#saveBtn').click(SaveItem);
			$('#cancelBtn').click(CancelBtn_Click);
			$('#errorMessage').html(data.substr(1));
		}
	}
	function failedSaveItem()
	{
		$('#saveBtn').val('Save');
		$('#saveBtn').click(SaveItem);
		$('#cancelBtn').click(CancelBtn_Click);
		alert('Failed to save.');
	}
	function CancelBtn_Click()
	{
		$('#saveBtn, #cancelBtn').unbind('click');
		location.href = $('#pageToLoadWhenFinished').attr('page');
	}
	function ConfigureCollapsibleSections()
	{
		$('#content .section-title').click(function(e)
		{
			$(this).next().slideToggle();
		});
	}
	function ListenForChangesToConditions()
	{
		$('div.condition_FieldMustBe').each(function(idx, ele)
		{
			var fieldName = $(ele).attr('requiredFieldName');
			var targetField = $('#itemfields').find('*[fieldname=""' + fieldName + '""]');
			targetField.change(EvaluateConditions);
		});
	}
	function EvaluateConditions()
	{
		$('div.condition_FieldMustBe').each(function(idx, ele)
		{
			var fieldName = $(ele).attr('requiredFieldName');
			var acceptableValues = $(ele).attr('acceptableValues').split('|');
			var targetField = $('#itemfields').find('*[fieldname=""' + fieldName + '""]');
			if(targetField.length > 0)
			{
				var valueToCompareAgainst = targetField.val();
				var targetFieldType = targetField.attr('type');
				if(targetFieldType && targetFieldType.toLowerCase() == 'checkbox')
					valueToCompareAgainst = targetField.is(':checked') ? 'true' : 'false';
				if(acceptableValues.indexOf(valueToCompareAgainst) > -1)
				{
					$(ele).next().show();
					$(ele).next().next().show();
				}
				else
				{
					$(ele).next().hide();
					$(ele).next().next().hide();
				}
			}
		});
	}
</script>";
		}
		private object GetAttribute(MemberInfo sourceObjectType, Type attributeType)
		{
			object[] attributes = sourceObjectType.GetCustomAttributes(attributeType, false);
			if (attributes.Length > 0)
				return attributes[0];
			return null;
		}
		private string GetAttributeValue(MemberInfo sourceObjectType, Type attributeType, string defaultValue = null)
		{
			object attribute = GetAttribute(sourceObjectType, attributeType);
			if (attribute != null)
				return attribute.ToString();
			return defaultValue;
		}
		private string GetConditionString(MemberInfo fi)
		{
			object attr = GetAttribute(fi, typeof(Configuration.EditorCondition_FieldMustBe));
			if (attr == null)
				return "";
			EditorCondition_FieldMustBe condition = (EditorCondition_FieldMustBe)attr;
			return "<div class=\"condition_FieldMustBe\" style=\"display: none\" requiredFieldName=\"" + condition.fieldName + "\" acceptableValues=\"" + string.Join("|", condition.acceptableValues) + "\"></div>";
		}
		private string ToString(object obj)
		{
			if (obj == null)
				return "";
			return obj.ToString();
		}
	}
}

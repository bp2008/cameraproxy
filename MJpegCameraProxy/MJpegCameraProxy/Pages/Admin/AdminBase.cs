using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web;
using BPUtil;
using BPUtil.SimpleHttp;

namespace MJpegCameraProxy.Pages.Admin
{
	abstract class AdminBase
	{
		public string GetHtml(HttpProcessor p, Session s, string pageKey)
		{
			try
			{
				return @"<!DOCTYPE HTML>
<html>
<head>
	<title>CameraProxy Administration</title>
	"
		+ GetScriptCallouts("../" + CameraProxyGlobals.jQueryPath, "../Scripts/sha1.js", "../Scripts/TableSorter.js", "../" + CameraProxyGlobals.jQueryUIJsPath)
		+ GetStyleCallouts("../Styles/TableSorter_Green.css", "../" + CameraProxyGlobals.jQueryUICssPath, "../Styles/Site.css")
		+ GetAdminScript(p, s, pageKey)
	+ @"
</head>
<body>
<table id=""layoutroot"">
	<tbody>
		<tr>
			<td colspan=""2"" id=""header"">
				<div class=""title"">CameraProxy</div>
				<div class=""version"">Version " + CameraProxyGlobals.Version + @"</div>
			</td>
		</tr>
		<tr id=""bodycell"">
			<td id=""menu"">
				"
		+ GetMenuHtml(p, s, pageKey)
	+ @"
			</td>
			<td id=""content"">
				"
		+ GetPageHtml(p, s)
		+ @"
			</td>
		</tr>
		<tr>
			<td colspan=""2"" id=""footer"">
				MJpegCameraProxy " + CameraProxyGlobals.Version + @"
			</td>
		</tr>
	</tbody>
</table>
</body>
</html>";
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			return null;
		}

		private string GetAdminScript(HttpProcessor p, Session s, string pageKey)
		{
			return @"<script type=""text/javascript"">
	var isCreatingItem = false;
	$(function()
	{
		$('#newItemDialog').dialog(
		{
			modal: true,
			autoOpen: false,
			buttons:
			{
				'Cancel': function()
				{
					if(isCreatingItem)
						return;
					$(this).dialog('close');
				},
				'Accept': function()
				{
					if(isCreatingItem)
						return;
					isCreatingItem = true;
					CreateItem();
				}
			}
		});
	});
	function EditItem(itemId)
	{
		top.location.href=""edititem?itemtype="" + $('#newItemDialog').attr('itemtype') + ""&itemid="" + itemId;
	}
	function AddItem()
	{
		$('#newItemDialog').dialog('open');
	}
	function CreateItem()
	{
		$.post(""saveitem?new=1"", { itemid: $('#newId').val(), itemtype: $('#newItemDialog').attr('itemtype') }).done(gotCreateItem).fail(failCreateItem);
	}
	function gotCreateItem(data, textStatus, jqXHR)
	{
		$('#newItemErrorMessage').html('');
		if(data[0] == '1')
			EditItem($('#newId').val());
		else
		{
			isCreatingItem = false;
			$('#newItemErrorMessage').html(data.substr(1));
		}
	}
	function failCreateItem()
	{
		isCreatingItem = false;
		failedContent();
	}
	var isDeleting = false;
	function DeleteSelected(itemtype)
	{
		if(isDeleting)
			return;
		var deletions = new Array();
		$('.listItemSelectionCheckbox_' + itemtype).each(function(idx, ele)
		{
			if($(ele).is(':checked'))
				deletions.push($(ele).attr('itemid'));
		});
		if(deletions.length > 0)
		{
			if(confirm('Please confirm that you wish to delete ' + deletions.join(', ')))
			{
				isDeleting = true;
				$.post('deleteitems', { itemtype: itemtype, ids: deletions.join(',') }).done(gotDeleteItems).fail(failedContent);
			}
		}
		else
			alert('Nothing is selected!');
	}
	function gotDeleteItems(data, textStatus, jqXHR)
	{
		$('#errorMessage').html('');
		if(data[0] == '1')
			location.href = location.href;
		else
		{
			isDeleting = false;
			$('#errorMessage').html(data.substr(1));
		}
	}
	function failedContent()
	{
		alert('Failed to load page!');
	}
	function SaveList()
	{
		var outerStr = new Array();
		var items = new Object();
		$('*[saveitemfield][saveitemkey]').each(function(idx, ele)
		{
			var itemkey = $(ele).attr('saveitemkey');
			if(typeof(items[itemkey]) == 'undefined')
				items[itemkey] = new Object();
			items[itemkey][$(ele).attr('saveitemfield')] = $(ele).val();
		});
		var outerChildren = Object.keys(items);
		for (var i = 0; i < outerChildren.length; i++)
		{
			var child = outerChildren[i];
			var innerStr = new Array();
			var innerChildren = Object.keys(items[child]);
			for (var j = 0; j < innerChildren.length; j++)
			{
				var innerChild = innerChildren[j];
				innerStr.push(innerChild + ':' + items[child][innerChild]);
			}
			outerStr.push(child + '*' + innerStr.join('*'));
		}
		var finalStr = outerStr.join('|');
		console.log(finalStr);
		$.post('savelist', {pagename: '" + pageKey + @"', items: finalStr}).done(function(data)
		{
			if(data.indexOf('0') == 0)
				alert(data.substr(1));
			if(data.indexOf('1') == 0)
				alert('Saved');
		});
	}
</script>";
		}

		protected virtual string GetMenuHtml(HttpProcessor p, Session s, string pageKey)
		{
			if (s?.permission != 100)
				return "";
			StringBuilder sb = new StringBuilder();
			foreach (string key in AdminPage.pageKeys)
				AddLink(sb, pageKey, 1, AdminPage.pageKeyToName[key], key);
			return sb.ToString();
		}
		protected static void AddLink(StringBuilder sb, string currentPage, int indentLevel, string linkText, string linkPath)
		{
			string link = linkPath.ToLower().Replace(" ", "");
			bool selected = link == currentPage;
			sb.Append("<a href=\"").Append(link).Append("\" class=\"indent").Append(indentLevel);
			if (selected)
				sb.Append(" active");
			sb.Append("\">").Append(linkText).Append("</a>").Append(Environment.NewLine);
		}
		private static string GetScriptCallouts(params string[] scripts)
		{
			if (scripts == null)
				return "";
			StringBuilder sb = new StringBuilder();
			foreach (string script in scripts)
				sb.Append(@"<script type=""text/javascript"" src=""").Append(script).Append(@"""></script>
	");
			return sb.ToString();
		}
		private static string GetStyleCallouts(params string[] styles)
		{
			if (styles == null)
				return "";
			StringBuilder sb = new StringBuilder();
			foreach (string style in styles)
				sb.Append(@"<link href=""").Append(style).Append(@""" rel=""stylesheet"" type=""text/css"">
	");
			return sb.ToString();
		}
		protected abstract string GetPageHtml(HttpProcessor p, Session s);
		internal virtual void HandleSave(HttpProcessor p, Session s, SortedList<string, SortedList<string, string>> items)
		{
			throw new Exception("HandleSave is not implemented by this class!");
		}
		#region Helpers

		internal static void SectionTitle(StringBuilder sb, string title)
		{
			sb.Append("<div class=\"section-title\">").Append(title).Append("</div>").Append(Environment.NewLine);
		}

		internal static void SectionStart(StringBuilder sb)
		{
			sb.Append("<div class=\"section\">").Append(Environment.NewLine);
		}
		internal static void SectionStart_RightAligned(StringBuilder sb)
		{
			sb.Append("<div class=\"section\" style=\"text-align:right;\">").Append(Environment.NewLine);
		}

		internal static void SectionEnd(StringBuilder sb)
		{
			sb.Append("</div>").Append(Environment.NewLine);
		}

		internal static void SectionTableStart(StringBuilder sb)
		{
			sb.Append("<div class=\"section\"><table class=\"fields\"><tbody>").Append(Environment.NewLine);
		}

		internal static void SectionTableEnd(StringBuilder sb)
		{
			sb.Append("</tbody></table></div>").Append(Environment.NewLine);
		}

		internal static void SectionTableEntry(StringBuilder sb, string label, string valueStr)
		{
			sb.Append("<tr><td class=\"title indent1\">");
			sb.Append(label);
			sb.Append("</td><td class=\"contentcell\">");
			sb.Append(valueStr);
			sb.Append("</td></tr>").Append(Environment.NewLine);
		}
		internal static void SectionTableStart_N(StringBuilder sb, string id, bool firstCellSmall, params string[] cellHeads)
		{
			sb.Append("<div class=\"section\"><table class=\"fields tablesorter\"><thead>").Append(Environment.NewLine);
			sb.Append("<tr>").Append(Environment.NewLine);
			for (int i = 0; i < cellHeads.Length; i++)
			{
				string s = cellHeads[i];
				sb.Append("<th");
				if (firstCellSmall && i == 0)
					sb.Append(" style=\"width: 20px;\"");
				sb.Append(">").Append(s).Append("</th>").Append(Environment.NewLine);
			}
			sb.Append("</tr>").Append(Environment.NewLine);
			sb.Append("</thead><tbody>").Append(Environment.NewLine);
		}
		internal static void SectionTableEntry_N(StringBuilder sb, bool firstCellSmall, params string[] cellValues)
		{
			sb.Append("<tr>").Append(Environment.NewLine);
			for (int i = 0; i < cellValues.Length; i++)
			{
				string s = cellValues[i];
				sb.Append("<td");
				if (firstCellSmall && i == 0)
					sb.Append(" style=\"width: 20px;\"");
				sb.Append(">").Append(s).Append("</td>").Append(Environment.NewLine);
			}
			sb.Append("</tr>").Append(Environment.NewLine);
		}
		internal static void AddAndDeleteButtons(StringBuilder sb, string itemType, ItemTableMode mode)
		{
			sb.Append("<div class=\"section\">");
			if (mode == ItemTableMode.Add)
				sb.Append("<input type=\"button\" value=\"Add " + itemType + "\" onClick=\"AddItem(); return false;\" />");
			else if (mode == ItemTableMode.Save)
				sb.Append("<input type=\"button\" value=\"Save List\" onClick=\"SaveList(); return false;\" />");
			sb.Append(" &nbsp; <input type=\"button\" value=\"Delete Selected\" onClick=\"DeleteSelected('" + itemType + "'); return false;\" /> &nbsp; &nbsp; &nbsp; ");
			sb.Append("<span id=\"errorMessage\" style=\"color:Red;\"></span></div>");
			sb.Append(@"
<div id=""newItemDialog"" title=""Enter a " + itemType + @" id:"" itemtype=""" + itemType + @""">
	<p>
		<label for=""newId"">Please enter an id for the new " + itemType + @":</label><br/>
		<input type=""text"" id=""newId"" />
		<p id=""newItemErrorMessage"" style=""color:Red""></p>
	</p>
</div>");
		}
		#endregion
	}
	enum ItemTableMode
	{
		Add,
		Save
	}
	class ItemTable<T>
	{
		string tableName, tableKey, idField;
		List<T> myList;
		ItemTableColumnDefinition<T>[] cols;
		object objToLockWhenReadingList = new object();
		ItemTableMode mode;
		public ItemTable(string tableName, string tableKey, string idField, List<T> itemList, object objToLockWhenReadingList, ItemTableMode mode, params ItemTableColumnDefinition<T>[] cols)
		{
			this.tableName = tableName;
			this.tableKey = tableKey;
			this.idField = idField;
			this.myList = itemList;
			this.objToLockWhenReadingList = objToLockWhenReadingList;
			this.cols = cols;
			this.mode = mode;
		}

		public string GetSectionHtml()
		{
			string[] headings = new string[cols.Length + 1];
			headings[0] = "";
			for (int i = 1; i < headings.Length; i++)
				headings[i] = cols[i - 1].columnName;

			StringBuilder sb = new StringBuilder();
			AdminBase.SectionTitle(sb, tableName);
			AdminBase.SectionTableStart_N(sb, tableKey + "tbl", true, headings);
			lock (objToLockWhenReadingList)
			{
				for (int i = 0; i < myList.Count; i++)
				{
					T obj = myList[i];
					string[] values = new string[cols.Length + 1];
					values[0] = "<input type=\"checkbox\" class=\"listItemSelectionCheckbox_" + tableKey + "\" itemid=\"" + HttpUtility.JavaScriptStringEncode((string)GetItemId(obj)) + "\" />";
					for (int j = 0; j < cols.Length; j++)
					{
						ItemTableColumnDefinition<T> cd = cols[j];
						values[j + 1] = cd.valueFunc(myList[i]);
					}
					AdminBase.SectionTableEntry_N(sb, true, values);
				}
			}
			AdminBase.SectionTableEnd(sb);

			AdminBase.AddAndDeleteButtons(sb, tableKey, mode);
			return sb.ToString();
		}

		private object GetItemId(T obj)
		{
			Type t = obj.GetType();
			FieldInfo fi = t.GetField(idField);
			if (fi != null)
				return fi.GetValue(obj);
			PropertyInfo pi = t.GetProperty(idField);
			if (pi != null)
				return pi.GetValue(obj, null);
			return "";
		}
	}
	class ItemTableColumnDefinition<T>
	{
		public string columnName;
		public Func<T, string> valueFunc;
		public ItemTableColumnDefinition(string columnName, Func<T, string> valueFunc)
		{
			this.columnName = columnName;
			this.valueFunc = valueFunc;
		}
	}
	//class ItemEditor<T>
	//{
	//    string editorName, editorKey;
	//    T myObj;
	//    ItemEditorRow<T>[] rows;
	//    object objToLockWhenReadingItem = new object();
	//    public ItemEditor(string editorName, string editorKey, T obj, object objToLockWhenReadingItem, params ItemEditorRow<T>[] rows)
	//    {
	//        this.editorName = editorName;
	//        this.editorKey = editorKey;
	//        this.myObj = obj;
	//        this.objToLockWhenReadingItem = objToLockWhenReadingItem;
	//        this.rows = rows;
	//    }

	//    public string GetSectionHtml()
	//    {
	//        StringBuilder sb = new StringBuilder();
	//        AdminBase.SectionTitle(sb, editorName);
	//        AdminBase.SectionTableStart(sb);
	//        lock (objToLockWhenReadingItem)
	//        {
	//            object.
	//            SectionTableEntry(sb, "Name", "<input style=\"width:95%\" type=\"text\" id=\"txtUserName\" value=\"" + HttpUtility.HtmlAttributeEncode(u.user) + "\" />");
	//            SectionTableEntry(sb, "Password", "<input style=\"width:75%\" type=\"password\" id=\"txtPassword\" value=\"" + HttpUtility.HtmlAttributeEncode(u.pass) + "\" /> <span class=\"hint\">(stored in plain text)</span>");
	//            SectionTableEntry(sb, "Permission", "<input style=\"width:50px\" type=\"number\" id=\"txtPermission\" maxlength=\"3\" value=\"" + (int)u.permission + "\" /> <span class=\"hint\">(0 = guest, 50 = user, 100 = admin) - Currently ignored</span>");
	//            SectionTableEnd(sb);

	//            SectionTitle(sb, "");
	//            SectionStart_RightAligned(sb);
	//            sb.Append("<span id=\"errorMessage\" style=\"color: Red;\"></span> &nbsp; ");
	//            sb.Append("<input id=\"saveBtn\" type=\"button\" value=\"Save\" onclick=\"SaveUser();return false;\" />");
	//            sb.Append(" &nbsp; ");
	//            sb.Append("<input id=\"cancelBtn\" type=\"button\" value=\"Cancel\" onclick=\"CancelBtn_Click();return false;\" />");
	//            SectionEnd(sb);
	//        }
	//        AdminBase.SectionTableEnd(sb);

	//        AdminBase.AddAndDeleteButtons(sb, tableKey);
	//        return sb.ToString();
	//    }
	//}
	//class ItemEditorRow<T>
	//{
	//    public string columnName;
	//    public Func<T, string> valueFunc;
	//    public ItemEditorRow(string columnName, Func<T, string> valueFunc)
	//    {
	//        this.columnName = columnName;
	//        this.valueFunc = valueFunc;
	//    }
	//}
	public static class AdminPage
	{
		internal static SortedList<string, string> pageKeyToName = new SortedList<string, string>();
		internal static SortedList<string, Type> pageKeyToType = new SortedList<string, Type>();
		internal static List<string> pageKeys = new List<string>();

		static AdminPage()
		{
			RegisterPage("Main", typeof(Main));
			RegisterPage("Cameras", typeof(Cameras));
			RegisterPage("PTZProfiles", typeof(PTZProfileList));
			RegisterPage("Users", typeof(Users));
			RegisterPage("WebServerFiles", typeof(WwwFiles));
			RegisterPage("edititem", typeof(EditItem), false);
			RegisterPage("Certificate", typeof(CertificateAdmin));
			RegisterPage("Logout", typeof(Login));
			RegisterPage("Login", typeof(Login), false);
		}

		private static void RegisterPage(string name, Type type, bool showInMenu = true)
		{
			string key = name.ToLower();
			if (showInMenu)
				pageKeys.Add(key);
			pageKeyToName[key] = name;
			pageKeyToType[key] = type;
		}
		public static void HandleRequest(string pageName, HttpProcessor p, Session s)
		{
			string pageKey = pageName.ToLower();

			if (s.permission < 100)
				pageKey = "login";

			Type type;
			if (!pageKeyToType.TryGetValue(pageKey, out type))
			{
				p.writeFailure();
				return;
			}
			ConstructorInfo ctor = type.GetConstructor(new Type[0]);
			object instance = ctor.Invoke(null);
			AdminBase page = (AdminBase)instance;
			string str = page.GetHtml(p, s, pageKey);
			if (str == null)
			{
				p.writeFailure("500 Internal Server Error");
				return;
			}
			HttpCompressionBody response = new HttpCompressionBody(Encoding.UTF8.GetBytes(str), ".html", p.GetHeaderValue("accept-encoding"));
			p.writeSuccess(contentLength: response.body.Length, additionalHeaders: response.additionalHeaders);
			p.outputStream.Flush();
			p.rawOutputStream.Write(response.body, 0, response.body.Length);
		}

		public static string HandleSaveList(HttpProcessor p, Session s)
		{
			string pageKey = p.GetPostParam("pagename").ToLower();

			if (s.permission < 100)
				return "0Insufficient Permission";

			Type type;
			if (!pageKeyToType.TryGetValue(pageKey, out type))
				return "0Invalid pagename";

			ConstructorInfo ctor = type.GetConstructor(new Type[0]);
			object instance = ctor.Invoke(null);
			AdminBase page = (AdminBase)instance;

			SortedList<string, SortedList<string, string>> items = new SortedList<string, SortedList<string, string>>();

			string rawFullItemString = p.GetPostParam("items");
			string[] itemStrings = rawFullItemString.Split('|');
			foreach (string itemString in itemStrings)
			{
				SortedList<string, string> valuesList = new SortedList<string, string>();
				string[] keyValuePairs = itemString.Split('*');
				for (int i = 1; i < keyValuePairs.Length; i++)
				{
					string[] keyAndValue = keyValuePairs[i].Split(':');
					valuesList.Add(keyAndValue[0], keyAndValue[1]);
				}
				items.Add(keyValuePairs[0], valuesList);
			}

			page.HandleSave(p, s, items);

			return "1";
		}
	}
}

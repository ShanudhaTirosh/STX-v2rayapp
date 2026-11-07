using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;

namespace ServiceLib.Handler.Fmt;

public class InnerFmt
{
	private static readonly Lazy<string> SessionSalt = new Lazy<string>(() => Utils.GetGuid(full: false));

	public static List<ProfileItem>? Resolve(string strData, string subid)
	{
		List<ProfileItem> list = new List<ProfileItem>();
		Dictionary<string, string> indexIdMap = new Dictionary<string, string>();
		using (StringReader stringReader = new StringReader(strData))
		{
			while (true)
			{
				string text = stringReader.ReadLine();
				if (text == null)
				{
					break;
				}
				if (text.IsNullOrEmpty())
				{
					continue;
				}
				string text2 = text.Trim();
				if (!text2.StartsWith("stxenterprises://", StringComparison.OrdinalIgnoreCase) && !text2.StartsWith("pxnshield://", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				ProfileItem profileItem = ResolveSingle(text2);
				if (profileItem != null && profileItem.ConfigType != EConfigType.Custom)
				{
					string guid = Utils.GetGuid(full: false);
					if (!profileItem.IndexId.IsNullOrEmpty())
					{
						indexIdMap[profileItem.IndexId] = guid;
					}
					profileItem.IndexId = guid;
					list.Add(profileItem);
				}
			}
		}
		List<ProfileItem> list2 = new List<ProfileItem>();
		foreach (ProfileItem item in list.Where((ProfileItem i) => i.ConfigType.IsGroupType()))
		{
			ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
			protocolExtra = ((!(protocolExtra.SubChildItems == "self")) ? protocolExtra with
			{
				SubChildItems = null
			} : protocolExtra with
			{
				SubChildItems = subid
			});
			List<string> list3 = Utils.String2List(protocolExtra.ChildItems);
			if (list3 != null && list3.Count > 0)
			{
				List<string> lst = (from id in list3
					select indexIdMap.GetValueOrDefault(id, null) into id
					where !id.IsNullOrEmpty()
					select id).ToList();
				protocolExtra = protocolExtra with
				{
					ChildItems = Utils.List2String(lst)
				};
			}
			else
			{
				protocolExtra = protocolExtra with
				{
					ChildItems = null
				};
			}
			item.SetProtocolExtra(protocolExtra);
			if (protocolExtra.SubChildItems.IsNullOrEmpty() && protocolExtra.ChildItems.IsNullOrEmpty())
			{
				list2.Add(item);
			}
		}
		list.RemoveAll(list2.Contains);
		return list;
	}

	public static string? ToUri(List<ProfileItem> items)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ProfileItem item in items)
		{
			if (item.ConfigType == EConfigType.Custom)
			{
				continue;
			}
			ProfileItem profileItem = JsonUtils.DeepCopy(item);
			if (profileItem == null)
			{
				continue;
			}
			string reproducibleExportId = GetReproducibleExportId(profileItem.IndexId);
			profileItem.IndexId = reproducibleExportId;
			if (profileItem.ConfigType.IsGroupType())
			{
				ProtocolExtraItem protocolExtraItem = profileItem.GetProtocolExtra();
				if (!protocolExtraItem.SubChildItems.IsNullOrEmpty())
				{
					protocolExtraItem = protocolExtraItem with
					{
						SubChildItems = "self"
					};
				}
				List<string> list = Utils.String2List(protocolExtraItem.ChildItems);
				if (list != null && list.Count > 0)
				{
					List<string> lst = (from id in list.Select(GetReproducibleExportId)
						where !id.IsNullOrEmpty()
						select id).ToList();
					protocolExtraItem = protocolExtraItem with
					{
						ChildItems = Utils.List2String(lst)
					};
				}
				profileItem.SetProtocolExtra(protocolExtraItem);
			}
			string value = ToUriSingle(profileItem);
			if (!value.IsNullOrEmpty())
			{
				stringBuilder.AppendLine(value);
			}
		}
		if (stringBuilder.Length <= 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	private static ProfileItem? ResolveSingle(string str)
	{
		Uri uri = Utils.TryUri(str);
		if ((object)uri == null)
		{
			return null;
		}
		if (!(JsonUtils.ParseJson(Utils.Base64Decode(uri.AbsolutePath.TrimStart('/'))) is JsonObject jsonObject))
		{
			return null;
		}
		if (jsonObject.TryGetPropertyValue("ProtoExtraObj", out JsonNode jsonNode) && jsonNode is JsonObject obj)
		{
			jsonObject["ProtoExtra"] = JsonUtils.Serialize(obj, indented: false);
			jsonObject.Remove("ProtoExtraObj");
		}
		if (jsonObject.TryGetPropertyValue("TransportExtraObj", out JsonNode jsonNode2) && jsonNode2 is JsonObject obj2)
		{
			jsonObject["TransportExtra"] = JsonUtils.Serialize(obj2, indented: false);
			jsonObject.Remove("TransportExtraObj");
		}
		ProfileItem profileItem = JsonUtils.Deserialize<ProfileItem>(JsonUtils.Serialize(jsonObject, indented: false));
		if (profileItem == null)
		{
			return null;
		}
		if (profileItem.ConfigVersion != 4)
		{
			return null;
		}
		if (!Enum.IsDefined(typeof(EConfigType), profileItem.ConfigType))
		{
			return null;
		}
		bool flag;
		switch (profileItem.CoreType)
		{
		case null:
		case ECoreType.Xray:
		case ECoreType.sing_box:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return null;
		}
		EMultipleLoad? multipleLoad = profileItem.GetProtocolExtra().MultipleLoad;
		if (multipleLoad.HasValue && !Enum.IsDefined(typeof(EMultipleLoad), multipleLoad))
		{
			return null;
		}
		return profileItem;
	}

	private static string? ToUriSingle(ProfileItem item)
	{
		if (!(JsonUtils.ParseJson(JsonUtils.Serialize(item, indented: false)) is JsonObject jsonObject))
		{
			return null;
		}
		if (jsonObject.TryGetPropertyValue("ProtoExtra", out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && !value.IsNullOrEmpty() && JsonUtils.ParseJson(value) is JsonObject value2)
		{
			jsonObject["ProtoExtraObj"] = value2;
			jsonObject.Remove("ProtoExtra");
		}
		if (jsonObject.TryGetPropertyValue("TransportExtra", out JsonNode jsonNode2) && jsonNode2 is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value3) && !value3.IsNullOrEmpty() && JsonUtils.ParseJson(value3) is JsonObject value4)
		{
			jsonObject["TransportExtraObj"] = value4;
			jsonObject.Remove("TransportExtra");
		}
		jsonObject.Remove("Subid");
		jsonObject.Remove("IsSub");
		RemoveEmptyJson(jsonObject);
		string text = Utils.Base64Encode(JsonUtils.Serialize(jsonObject, indented: false)).Replace('+', '-').Replace('/', '_')
			.Replace("=", "");
		return "stxenterprises://" + item.ConfigType.ToString().ToLower() + "/" + text;
	}

	private static string GetReproducibleExportId(string originalIndexId)
	{
		if (originalIndexId.IsNullOrEmpty())
		{
			return originalIndexId;
		}
		return Convert.ToBase64String(BitConverter.GetBytes(HashCode.Combine(SessionSalt.Value, originalIndexId) & 0x7FFFFFFF)).Replace("=", "");
	}

	private static void RemoveEmptyJson(JsonNode? node)
	{
		if (node is JsonObject jsonObject)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, JsonNode> item in jsonObject)
			{
				RemoveEmptyJson(item.Value);
				if (IsEmpty(item.Value))
				{
					list.Add(item.Key);
				}
			}
			{
				foreach (string item2 in list)
				{
					jsonObject.Remove(item2);
				}
				return;
			}
		}
		if (!(node is JsonArray jsonArray))
		{
			return;
		}
		checked
		{
			for (int num = jsonArray.Count - 1; num >= 0; num--)
			{
				RemoveEmptyJson(jsonArray[num]);
				if (IsEmpty(jsonArray[num]))
				{
					jsonArray.RemoveAt(num);
				}
			}
		}
	}

	private static bool IsEmpty(JsonNode? node)
	{
		if (node != null)
		{
			string value;
			if (!(node is JsonValue jsonValue))
			{
				if (node is JsonObject jsonObject)
				{
					return jsonObject.Count == 0;
				}
				if (node is JsonArray jsonArray)
				{
					return jsonArray.Count == 0;
				}
			}
			else if (jsonValue.TryGetValue<string>(out value))
			{
				return string.IsNullOrEmpty(value);
			}
			return false;
		}
		return true;
	}
}

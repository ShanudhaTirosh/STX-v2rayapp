using System.Collections.Generic;
using System.Text.Json.Nodes;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;

namespace ServiceLib.Handler.Fmt;

public class SingboxFmt : BaseFmt
{
	public static List<ProfileItem>? ResolveFullArray(string strData, string? subRemarks)
	{
		object[] array = JsonUtils.Deserialize<object[]>(strData);
		if (array == null || array.Length <= 0)
		{
			return null;
		}
		List<ProfileItem> list = new List<ProfileItem>();
		object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ProfileItem profileItem = ResolveFull(JsonUtils.Serialize(array2[i]), subRemarks);
			if (profileItem != null)
			{
				list.Add(profileItem);
			}
		}
		return list;
	}

	public static ProfileItem? ResolveFull(string strData, string? subRemarks)
	{
		JsonNode jsonNode = JsonUtils.ParseJson(strData);
		if (jsonNode?["inbounds"] == null || jsonNode["outbounds"] == null || jsonNode["route"] == null || jsonNode["dns"] == null)
		{
			return null;
		}
		string address = BaseFmt.WriteAllText(strData);
		return new ProfileItem
		{
			CoreType = ECoreType.sing_box,
			Address = address,
			Remarks = (subRemarks ?? "singbox_custom")
		};
	}
}

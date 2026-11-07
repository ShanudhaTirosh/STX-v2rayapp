using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class TrojanFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.Trojan
		};
		Uri uri = Utils.TryUri(str);
		if (uri == null)
		{
			return null;
		}
		item.Address = uri.IdnHost;
		item.Port = uri.Port;
		item.Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
		item.Password = Utils.UrlDecode(uri.UserInfo);
		NameValueCollection query = Utils.ParseQueryString(uri.Query);
		item.SetProtocolExtra(item.GetProtocolExtra() with
		{
			Flow = BaseFmt.GetQueryValue(query, "flow")
		});
		BaseFmt.ResolveUriQuery(query, ref item);
		return item;
	}

	public static string? ToUri(ProfileItem? item)
	{
		if (item == null)
		{
			return null;
		}
		string remark = string.Empty;
		if (item.Remarks.IsNotEmpty())
		{
			remark = "#" + Utils.UrlEncode(item.Remarks);
		}
		Dictionary<string, string> dicQuery = new Dictionary<string, string>();
		if (!item.GetProtocolExtra().Flow.IsNullOrEmpty())
		{
			dicQuery.Add("flow", item.GetProtocolExtra().Flow);
		}
		BaseFmt.ToUriQuery(item, null, ref dicQuery);
		return BaseFmt.ToUri(EConfigType.Trojan, item.Address, item.Port, item.Password, dicQuery, remark);
	}
}

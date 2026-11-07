using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class VLESSFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.VLESS
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
			VlessEncryption = BaseFmt.GetQueryValue(query, "encryption", "none"),
			Flow = BaseFmt.GetQueryValue(query, "flow")
		});
		item.StreamSecurity = BaseFmt.GetQueryValue(query, "security");
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
		dicQuery.Add("encryption", (!item.GetProtocolExtra().VlessEncryption.IsNullOrEmpty()) ? item.GetProtocolExtra().VlessEncryption : "none");
		if (!item.GetProtocolExtra().Flow.IsNullOrEmpty())
		{
			dicQuery.Add("flow", item.GetProtocolExtra().Flow);
		}
		BaseFmt.ToUriQuery(item, "none", ref dicQuery);
		return BaseFmt.ToUri(EConfigType.VLESS, item.Address, item.Port, item.Password, dicQuery, remark);
	}
}

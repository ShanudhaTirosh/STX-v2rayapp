using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class TuicFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.TUIC
		};
		Uri uri = Utils.TryUri(str);
		if (uri == null)
		{
			return null;
		}
		item.Address = uri.IdnHost;
		item.Port = uri.Port;
		item.Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
		string[] array = Utils.UrlDecode(uri.UserInfo).Split(new char[1] { ':' }, 2);
		if (array.Length == 2)
		{
			item.Username = array.First();
			item.Password = array.Last();
		}
		NameValueCollection query = Utils.ParseQueryString(uri.Query);
		BaseFmt.ResolveUriQuery(query, ref item);
		item.SetProtocolExtra(item.GetProtocolExtra() with
		{
			CongestionControl = BaseFmt.GetQueryValue(query, "congestion_control")
		});
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
		BaseFmt.ToUriQueryLite(item, ref dicQuery);
		if (!item.GetProtocolExtra().CongestionControl.IsNullOrEmpty())
		{
			dicQuery.Add("congestion_control", item.GetProtocolExtra().CongestionControl);
		}
		return BaseFmt.ToUri(EConfigType.TUIC, item.Address, item.Port, item.Username + ":" + item.Password, dicQuery, remark);
	}
}

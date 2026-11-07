using System;
using System.Collections.Generic;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class AnytlsFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		Uri uri = Utils.TryUri(str);
		if (uri == null)
		{
			return null;
		}
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.Anytls,
			Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
			Address = uri.IdnHost,
			Port = uri.Port
		};
		string password = Utils.UrlDecode(uri.UserInfo);
		item.Password = password;
		BaseFmt.ResolveUriQuery(Utils.ParseQueryString(uri.Query), ref item);
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
		string password = item.Password;
		Dictionary<string, string> dicQuery = new Dictionary<string, string>();
		BaseFmt.ToUriQuery(item, "none", ref dicQuery);
		return BaseFmt.ToUri(EConfigType.Anytls, item.Address, item.Port, password, dicQuery, remark);
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class NaiveFmt : BaseFmt
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
			ConfigType = EConfigType.Naive,
			Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
			Address = uri.IdnHost,
			Port = uri.Port
		};
		ProtocolExtraItem protocolExtraItem = item.GetProtocolExtra();
		if (uri.Scheme.Contains("quic"))
		{
			protocolExtraItem = protocolExtraItem with
			{
				NaiveQuic = true
			};
		}
		string text = Utils.UrlDecode(uri.UserInfo);
		if (text.Contains(':'))
		{
			string[] array = text.Split(':', 2);
			item.Username = array[0];
			item.Password = array[1];
		}
		else
		{
			item.Password = text;
		}
		NameValueCollection query = Utils.ParseQueryString(uri.Query);
		BaseFmt.ResolveUriQuery(query, ref item);
		int num = (int.TryParse(BaseFmt.GetQueryValue(query, "insecure-concurrency"), out var result) ? result : 0);
		if (num > 0)
		{
			protocolExtraItem = protocolExtraItem with
			{
				InsecureConcurrency = num
			};
		}
		item.SetProtocolExtra(protocolExtraItem);
		return item;
	}

	public static string? ToUri(ProfileItem? item)
	{
		if (item == null)
		{
			return null;
		}
		string text = string.Empty;
		if (item.Remarks.IsNotEmpty())
		{
			text = "#" + Utils.UrlEncode(item.Remarks);
		}
		string value = (item.Username.IsNotEmpty() ? (Utils.UrlEncode(item.Username) + ":" + Utils.UrlEncode(item.Password)) : Utils.UrlEncode(item.Password));
		Dictionary<string, string> dicQuery = new Dictionary<string, string>();
		BaseFmt.ToUriQuery(item, "none", ref dicQuery);
		ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
		if (protocolExtra.InsecureConcurrency > 0)
		{
			dicQuery.Add("insecure-concurrency", protocolExtra?.InsecureConcurrency.ToString());
		}
		string text2 = ((dicQuery.Count > 0) ? ("?" + string.Join("&", dicQuery.Select((KeyValuePair<string, string> x) => x.Key + "=" + x.Value).ToArray())) : string.Empty);
		string text3 = $"{value}@{BaseFmt.GetIpv6(item.Address)}:{item.Port}";
		if (protocolExtra.NaiveQuic == true)
		{
			return "naive+quic://" + text3 + text2 + text;
		}
		return "naive+https://" + text3 + text2 + text;
	}
}

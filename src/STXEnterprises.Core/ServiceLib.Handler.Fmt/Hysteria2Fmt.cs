using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class Hysteria2Fmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.Hysteria2
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
		BaseFmt.ResolveUriQuery(query, ref item);
		if (item.CertSha.IsNullOrEmpty())
		{
			item.CertSha = BaseFmt.GetQueryDecoded(query, "pinSHA256");
		}
		item.SetProtocolExtra(item.GetProtocolExtra() with
		{
			Ports = BaseFmt.GetQueryDecoded(query, "mport"),
			SalamanderPass = BaseFmt.GetQueryDecoded(query, "obfs-password")
		});
		return item;
	}

	public static string? ToUri(ProfileItem? item)
	{
		if (item == null)
		{
			return null;
		}
		_ = string.Empty;
		string remark = string.Empty;
		if (item.Remarks.IsNotEmpty())
		{
			remark = "#" + Utils.UrlEncode(item.Remarks);
		}
		Dictionary<string, string> dicQuery = new Dictionary<string, string>();
		BaseFmt.ToUriQueryLite(item, ref dicQuery);
		ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
		if (!protocolExtra.SalamanderPass.IsNullOrEmpty())
		{
			dicQuery.Add("obfs", "salamander");
			dicQuery.Add("obfs-password", Utils.UrlEncode(protocolExtra.SalamanderPass));
		}
		if (!protocolExtra.Ports.IsNullOrEmpty())
		{
			dicQuery.Add("mport", Utils.UrlEncode(protocolExtra.Ports.Replace(':', '-')));
		}
		if (!item.CertSha.IsNullOrEmpty())
		{
			string text = item.CertSha;
			int num = text.IndexOf(',');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			dicQuery.Add("pinSHA256", Utils.UrlEncode(text));
		}
		return BaseFmt.ToUri(EConfigType.Hysteria2, item.Address, item.Port, item.Password, dicQuery, remark);
	}

	public static ProfileItem? ResolveFull2(string strData, string? subRemarks)
	{
		if (BaseFmt.Contains(strData, "server", "auth", "up", "down", "listen"))
		{
			string address = BaseFmt.WriteAllText(strData);
			return new ProfileItem
			{
				CoreType = ECoreType.hysteria2,
				Address = address,
				Remarks = (subRemarks ?? "hysteria2_custom")
			};
		}
		return null;
	}
}

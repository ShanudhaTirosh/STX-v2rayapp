using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class WireguardFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem profileItem = new ProfileItem
		{
			ConfigType = EConfigType.WireGuard
		};
		Uri uri = Utils.TryUri(str);
		if (uri == null)
		{
			return null;
		}
		profileItem.Address = uri.IdnHost;
		profileItem.Port = uri.Port;
		profileItem.Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
		profileItem.Password = Utils.UrlDecode(uri.UserInfo);
		NameValueCollection query = Utils.ParseQueryString(uri.Query);
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			WgPublicKey = BaseFmt.GetQueryDecoded(query, "publickey"),
			WgPresharedKey = BaseFmt.GetQueryDecoded(query, "presharedkey"),
			WgReserved = BaseFmt.GetQueryDecoded(query, "reserved"),
			WgInterfaceAddress = BaseFmt.GetQueryDecoded(query, "address"),
			WgMtu = (int.TryParse(BaseFmt.GetQueryDecoded(query, "mtu"), out var result) ? new int?(result) : ((int?)null))
		});
		return profileItem;
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
		ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (!protocolExtra.WgPublicKey.IsNullOrEmpty())
		{
			dictionary.Add("publickey", Utils.UrlEncode(protocolExtra.WgPublicKey));
		}
		if (!protocolExtra.WgPresharedKey.IsNullOrEmpty())
		{
			dictionary.Add("presharedkey", Utils.UrlEncode(protocolExtra.WgPresharedKey));
		}
		if (!protocolExtra.WgReserved.IsNullOrEmpty())
		{
			dictionary.Add("reserved", Utils.UrlEncode(protocolExtra.WgReserved));
		}
		if (!protocolExtra.WgInterfaceAddress.IsNullOrEmpty())
		{
			dictionary.Add("address", Utils.UrlEncode(protocolExtra.WgInterfaceAddress));
		}
		if (protocolExtra.WgMtu > 0)
		{
			dictionary.Add("mtu", protocolExtra.WgMtu.ToString());
		}
		return BaseFmt.ToUri(EConfigType.WireGuard, item.Address, item.Port, item.Password, dictionary, remark);
	}

	public static List<ProfileItem>? ResolveConfig(string strData)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
		Dictionary<string, string> dictionary2 = dictionary;
		checked
		{
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
					if (text2.Equals("[Interface]", StringComparison.OrdinalIgnoreCase))
					{
						dictionary2 = dictionary;
					}
					else if (text2.Equals("[Peer]", StringComparison.OrdinalIgnoreCase))
					{
						Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						list.Add(dictionary3);
						dictionary2 = dictionary3;
					}
					else
					{
						if (text2.StartsWith('[') || text2.StartsWith('#') || text2.StartsWith(';'))
						{
							continue;
						}
						int num = text.IndexOf('=');
						if (num > 0)
						{
							string key = text.Substring(0, num).Trim();
							string text3 = text.Substring(num + 1).Trim();
							int num2 = text3.IndexOfAny(new char[2] { '#', ';' });
							if (num2 >= 0)
							{
								text3 = text3.Substring(0, num2).TrimEnd();
							}
							dictionary2[key] = text3;
						}
					}
				}
			}
			if (!dictionary.TryGetValue("PrivateKey", out var value) || value.IsNullOrEmpty())
			{
				return null;
			}
			int num3 = ((dictionary.TryGetValue("MTU", out var value2) && int.TryParse(value2, out var result)) ? result : 0);
			string wgInterfaceAddress = (dictionary.TryGetValue("Address", out var value3) ? value3 : string.Empty);
			int num4 = 0;
			List<ProfileItem> list2 = new List<ProfileItem>();
			foreach (Dictionary<string, string> item in list)
			{
				if (item.TryGetValue("Endpoint", out var value4) && !value4.IsNullOrEmpty() && TryParseEndpoint(value4, out string address, out int port))
				{
					ProtocolExtraItem protocolExtra = new ProtocolExtraItem
					{
						WgPublicKey = (item.TryGetValue("PublicKey", out var value5) ? value5 : string.Empty).NullIfEmpty(),
						WgPresharedKey = (item.TryGetValue("PresharedKey", out var value6) ? value6 : string.Empty).NullIfEmpty(),
						WgInterfaceAddress = wgInterfaceAddress,
						WgReserved = (item.TryGetValue("Reserved", out var value7) ? value7 : string.Empty).NullIfEmpty(),
						WgMtu = ((num3 > 0) ? new int?(num3) : ((int?)null))
					};
					ProfileItem profileItem = new ProfileItem
					{
						Remarks = $"{"WireGuard"} Peer {num4 + 1}",
						ConfigType = EConfigType.WireGuard,
						Address = address,
						Port = port,
						Password = value
					};
					profileItem.SetProtocolExtra(protocolExtra);
					list2.Add(profileItem);
					num4++;
				}
			}
			return list2;
		}
	}

	private static bool TryParseEndpoint(string endpoint, out string address, out int port)
	{
		address = string.Empty;
		port = 2408;
		string text = endpoint.Trim();
		if (text.IsNullOrEmpty())
		{
			return false;
		}
		checked
		{
			if (text[0] == '[')
			{
				int num = text.IndexOf(']');
				if (num <= 1)
				{
					return false;
				}
				address = text.Substring(1, unchecked(num - 1)).Trim();
				int num2 = num + 1;
				if (num2 < text.Length && text[num2] == ':' && int.TryParse(text.Substring(num2 + 1).Trim(), out var result) && result > 0 && result <= 65535)
				{
					port = result;
				}
				return address.IsNotEmpty();
			}
			int num3 = text.LastIndexOf(':');
			if (num3 <= 0)
			{
				address = text;
				return true;
			}
			address = text.Substring(0, num3).Trim();
			string s = text.Substring(num3 + 1).Trim();
			if (address.IsNullOrEmpty())
			{
				return false;
			}
			if (int.TryParse(s, out var result2) && result2 > 0 && result2 <= 65535)
			{
				port = result2;
				return true;
			}
			address = text;
			return true;
		}
	}
}

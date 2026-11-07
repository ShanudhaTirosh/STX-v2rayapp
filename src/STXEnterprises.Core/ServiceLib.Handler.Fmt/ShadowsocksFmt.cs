using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class ShadowsocksFmt : BaseFmt
{
	private static readonly Regex UrlFinder = new Regex("ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\\S+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex DetailsParser = new Regex("^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\\d+?))$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem profileItem = ResolveSSLegacy(str) ?? ResolveSip002(str);
		if (profileItem == null)
		{
			return null;
		}
		if (profileItem.Address.Length == 0 || profileItem.Port == 0 || profileItem.GetProtocolExtra().SsMethod.IsNullOrEmpty() || profileItem.Password.Length == 0)
		{
			return null;
		}
		profileItem.ConfigType = EConfigType.Shadowsocks;
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
		string userInfo = Utils.Base64Encode(item.GetProtocolExtra().SsMethod + ":" + item.Password, removePadding: true);
		TransportExtraItem transportExtra = item.GetTransportExtra();
		string text = string.Empty;
		string text2 = string.Empty;
		if (item.Network == "raw" && transportExtra.RawHeaderType == "http")
		{
			text = "obfs-local";
			text2 = "obfs=http;obfs-host=" + transportExtra.Host + ";";
		}
		else
		{
			if (item.Network == "ws")
			{
				text2 += "mode=websocket;";
				text2 = text2 + "host=" + transportExtra.Host + ";";
				string text3 = (transportExtra.Path ?? string.Empty).Replace("\\", "\\\\").Replace("=", "\\=").Replace(",", "\\,");
				text2 = text2 + "path=" + text3 + ";";
			}
			if (item.StreamSecurity == "tls")
			{
				text2 += "tls;";
				List<string> list = CertPemManager.ParsePemChain(item.Cert);
				if (list.Count > 0)
				{
					string text4 = list.First().Replace("-----BEGIN CERTIFICATE-----\n", "").Replace("\n-----END CERTIFICATE-----", "")
						.Trim();
					text4 = text4.Replace("=", "\\=");
					text2 = text2 + "certRaw=" + text4 + ";";
				}
			}
			if (text2.Length > 0)
			{
				text = "v2ray-plugin";
				text2 += "mux=0;";
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (text.IsNotEmpty())
		{
			string text5 = text + ";" + text2;
			if (text5.EndsWith(';'))
			{
				string text6 = text5;
				text5 = text6.Substring(0, text6.Length - 1);
			}
			dictionary["plugin"] = Utils.UrlEncode(text5);
		}
		return BaseFmt.ToUri(EConfigType.Shadowsocks, item.Address, item.Port, userInfo, dictionary, remark);
	}

	private static ProfileItem? ResolveSSLegacy(string result)
	{
		Match match = UrlFinder.Match(result);
		if (!match.Success)
		{
			return null;
		}
		ProfileItem profileItem = new ProfileItem();
		string plainText = match.Groups["base64"].Value.TrimEnd('/');
		string value = match.Groups["tag"].Value;
		if (value.IsNotEmpty())
		{
			profileItem.Remarks = Utils.UrlDecode(value);
		}
		Match match2;
		try
		{
			match2 = DetailsParser.Match(Utils.Base64Decode(plainText));
		}
		catch (FormatException)
		{
			return null;
		}
		if (!match2.Success)
		{
			return null;
		}
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			SsMethod = match2.Groups["method"].Value
		});
		profileItem.Password = match2.Groups["password"].Value;
		profileItem.Address = match2.Groups["hostname"].Value;
		profileItem.Port = match2.Groups["port"].Value.ToInt();
		return profileItem;
	}

	private static ProfileItem? ResolveSip002(string result)
	{
		Uri uri = Utils.TryUri(result);
		if (uri == null)
		{
			return null;
		}
		ProfileItem profileItem = new ProfileItem
		{
			Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
			Address = uri.IdnHost,
			Port = uri.Port
		};
		string text = Utils.UrlDecode(uri.UserInfo);
		if (text.Contains(':'))
		{
			string[] array = text.Split(new char[1] { ':' }, 2);
			if (array.Length != 2)
			{
				return null;
			}
			profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
			{
				SsMethod = array.First()
			});
			profileItem.Password = Utils.UrlDecode(array.Last());
		}
		else
		{
			string[] array2 = Utils.Base64Decode(text).Split(new char[1] { ':' }, 2);
			if (array2.Length != 2)
			{
				return null;
			}
			profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
			{
				SsMethod = array2.First()
			});
			profileItem.Password = array2.Last();
		}
		NameValueCollection nameValueCollection = Utils.ParseQueryString(uri.Query);
		if (nameValueCollection["plugin"] != null)
		{
			string[] array3 = nameValueCollection["plugin"].Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (array3.Length == 0)
			{
				return null;
			}
			string text2 = array3[0];
			if (text2 == "simple-obfs")
			{
				text2 = "obfs-local";
			}
			if (text2 == "obfs-local")
			{
				string text3 = array3.FirstOrDefault((string t) => t.StartsWith("obfs="));
				string text4 = array3.FirstOrDefault((string t) => t.StartsWith("obfs-host="));
				if (!text3.IsNullOrEmpty() && text3.Contains("obfs=http") && text4.IsNotEmpty())
				{
					text4 = text4.Replace("obfs-host=", "");
					profileItem.Network = "raw";
					profileItem.SetTransportExtra(profileItem.GetTransportExtra() with
					{
						RawHeaderType = "http",
						Host = text4
					});
				}
			}
			else if (text2 == "v2ray-plugin")
			{
				string text5 = array3.FirstOrDefault((string t) => t.StartsWith("mode="), "websocket");
				string text6 = array3.FirstOrDefault((string t) => t.StartsWith("host="));
				string text7 = array3.FirstOrDefault((string t) => t.StartsWith("path="));
				bool flag = array3.Any((string t) => t == "tls");
				string text8 = array3.FirstOrDefault((string t) => t.StartsWith("certRaw="));
				string text9 = array3.FirstOrDefault((string t) => t.StartsWith("mux="));
				if (text5.Replace("mode=", "") == "websocket")
				{
					profileItem.Network = "ws";
					TransportExtraItem transportExtraItem = profileItem.GetTransportExtra();
					if (!text6.IsNullOrEmpty())
					{
						string text10 = text6.Replace("host=", "");
						transportExtraItem = transportExtraItem with
						{
							Host = text10
						};
						profileItem.Sni = text10;
					}
					if (!text7.IsNullOrEmpty())
					{
						string text11 = text7.Replace("path=", "");
						text11 = text11.Replace("\\=", "=").Replace("\\,", ",").Replace("\\\\", "\\");
						transportExtraItem = transportExtraItem with
						{
							Path = text11
						};
					}
					profileItem.SetTransportExtra(transportExtraItem);
				}
				if (flag)
				{
					profileItem.StreamSecurity = "tls";
					if (!text8.IsNullOrEmpty())
					{
						string text12 = text8.Replace("certRaw=", "");
						text12 = text12.Replace("\\=", "=");
						string cert = "-----BEGIN CERTIFICATE-----\n" + text12 + "\n-----END CERTIFICATE-----";
						profileItem.Cert = cert;
					}
				}
				if (!text9.IsNullOrEmpty() && text9.Replace("mux=", "").ToInt() > 0)
				{
					return null;
				}
			}
		}
		return profileItem;
	}

	public static List<ProfileItem>? ResolveSip008(string result)
	{
		List<SsServer> list = JsonUtils.Deserialize<List<SsServer>>(result);
		if (list != null && list.Count <= 0)
		{
			SsSIP008 ssSIP = JsonUtils.Deserialize<SsSIP008>(result);
			if (ssSIP != null && ssSIP.servers?.Count > 0)
			{
				list = ssSIP.servers;
			}
		}
		if (list != null && list.Count > 0)
		{
			List<ProfileItem> list2 = new List<ProfileItem>();
			{
				foreach (SsServer item in list)
				{
					ProfileItem profileItem = new ProfileItem
					{
						Remarks = item.remarks,
						Password = item.password,
						Address = item.server,
						Port = item.server_port.ToInt()
					};
					profileItem.SetProtocolExtra(new ProtocolExtraItem
					{
						SsMethod = item.method
					});
					list2.Add(profileItem);
				}
				return list2;
			}
		}
		return null;
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;

namespace ServiceLib.Handler.Fmt;

public class BaseFmt
{
	private static readonly string[] _allowInsecureArray = new string[3] { "insecure", "allowInsecure", "allow_insecure" };

	private static string UrlEncodeSafe(string? value)
	{
		return Utils.UrlEncode(value ?? string.Empty);
	}

	protected static string GetIpv6(string address)
	{
		if (Utils.IsIpv6(address))
		{
			if (!address.StartsWith('[') || !address.EndsWith(']'))
			{
				return "[" + address + "]";
			}
			return address;
		}
		return address;
	}

	protected static int ToUriQuery(ProfileItem item, string? securityDef, ref Dictionary<string, string> dicQuery)
	{
		TransportExtraItem transportExtra = item.GetTransportExtra();
		if (item.StreamSecurity.IsNotEmpty())
		{
			dicQuery.Add("security", item.StreamSecurity);
		}
		else if (securityDef != null)
		{
			dicQuery.Add("security", securityDef);
		}
		if (item.Sni.IsNotEmpty())
		{
			dicQuery.Add("sni", Utils.UrlEncode(item.Sni));
		}
		if (item.Fingerprint.IsNotEmpty())
		{
			dicQuery.Add("fp", Utils.UrlEncode(item.Fingerprint));
		}
		if (item.PublicKey.IsNotEmpty())
		{
			dicQuery.Add("pbk", Utils.UrlEncode(item.PublicKey));
		}
		if (item.ShortId.IsNotEmpty())
		{
			dicQuery.Add("sid", Utils.UrlEncode(item.ShortId));
		}
		if (item.SpiderX.IsNotEmpty())
		{
			dicQuery.Add("spx", Utils.UrlEncode(item.SpiderX));
		}
		if (item.Mldsa65Verify.IsNotEmpty())
		{
			dicQuery.Add("pqv", Utils.UrlEncode(item.Mldsa65Verify));
		}
		if (item.StreamSecurity.Equals("tls"))
		{
			if (item.Alpn.IsNotEmpty())
			{
				dicQuery.Add("alpn", Utils.UrlEncode(item.Alpn));
			}
			ToUriQueryAllowInsecure(item, ref dicQuery);
		}
		if (item.EchConfigList.IsNotEmpty())
		{
			dicQuery.Add("ech", Utils.UrlEncode(item.EchConfigList));
		}
		if (item.CertSha.IsNotEmpty())
		{
			dicQuery.Add("pcs", Utils.UrlEncode(item.CertSha));
		}
		if (item.Finalmask.IsNotEmpty())
		{
			JsonNode jsonNode = JsonUtils.ParseJson(item.Finalmask);
			string url = ((jsonNode != null) ? JsonUtils.Serialize(jsonNode, new JsonSerializerOptions
			{
				WriteIndented = false,
				DefaultIgnoreCondition = JsonIgnoreCondition.Never,
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			}) : item.Finalmask);
			dicQuery.Add("fm", Utils.UrlEncode(url));
		}
		string text = item.GetNetwork();
		if (!Global.Networks.Contains(text))
		{
			text = "raw";
		}
		dicQuery.Add("type", (text == "raw") ? "tcp" : text);
		switch (text)
		{
		case "raw":
			dicQuery.Add("headerType", transportExtra.RawHeaderType.IsNotEmpty() ? transportExtra.RawHeaderType : "none");
			if (transportExtra.Host.IsNotEmpty())
			{
				dicQuery.Add("host", UrlEncodeSafe(transportExtra.Host));
			}
			if (transportExtra.Path.IsNotEmpty())
			{
				dicQuery.Add("path", UrlEncodeSafe(transportExtra.Path));
			}
			break;
		case "kcp":
			dicQuery.Add("headerType", transportExtra.KcpHeaderType.IsNotEmpty() ? transportExtra.KcpHeaderType : "none");
			if (transportExtra.KcpSeed.IsNotEmpty())
			{
				dicQuery.Add("seed", UrlEncodeSafe(transportExtra.KcpSeed));
			}
			if (transportExtra.KcpMtu > 0)
			{
				dicQuery.Add("mtu", transportExtra.KcpMtu.ToString());
			}
			break;
		case "ws":
		case "httpupgrade":
			if (transportExtra.Host.IsNotEmpty())
			{
				dicQuery.Add("host", UrlEncodeSafe(transportExtra.Host));
			}
			if (transportExtra.Path.IsNotEmpty())
			{
				dicQuery.Add("path", UrlEncodeSafe(transportExtra.Path));
			}
			break;
		case "xhttp":
			if (transportExtra.Host.IsNotEmpty())
			{
				dicQuery.Add("host", UrlEncodeSafe(transportExtra.Host));
			}
			if (transportExtra.Path.IsNotEmpty())
			{
				dicQuery.Add("path", UrlEncodeSafe(transportExtra.Path));
			}
			if (transportExtra.XhttpMode.IsNotEmpty() && Global.XhttpMode.Contains(transportExtra.XhttpMode))
			{
				dicQuery.Add("mode", UrlEncodeSafe(transportExtra.XhttpMode));
			}
			if (transportExtra.XhttpExtra.IsNotEmpty())
			{
				JsonNode jsonNode2 = JsonUtils.ParseJson(transportExtra.XhttpExtra);
				string value = ((jsonNode2 != null) ? JsonUtils.Serialize(jsonNode2, new JsonSerializerOptions
				{
					WriteIndented = false,
					DefaultIgnoreCondition = JsonIgnoreCondition.Never,
					Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				}) : transportExtra.XhttpExtra);
				dicQuery.Add("extra", UrlEncodeSafe(value));
			}
			break;
		case "grpc":
			if (transportExtra.GrpcServiceName.IsNotEmpty())
			{
				dicQuery.Add("authority", UrlEncodeSafe(transportExtra.GrpcAuthority));
				dicQuery.Add("serviceName", UrlEncodeSafe(transportExtra.GrpcServiceName));
				string grpcMode = transportExtra.GrpcMode;
				if ((grpcMode == "gun" || grpcMode == "multi") ? true : false)
				{
					dicQuery.Add("mode", UrlEncodeSafe(transportExtra.GrpcMode));
				}
			}
			break;
		}
		return 0;
	}

	protected static int ToUriQueryLite(ProfileItem item, ref Dictionary<string, string> dicQuery)
	{
		if (item.Sni.IsNotEmpty())
		{
			dicQuery.Add("sni", Utils.UrlEncode(item.Sni));
		}
		if (item.Alpn.IsNotEmpty())
		{
			dicQuery.Add("alpn", Utils.UrlEncode(item.Alpn));
		}
		ToUriQueryAllowInsecure(item, ref dicQuery);
		return 0;
	}

	private static int ToUriQueryAllowInsecure(ProfileItem item, ref Dictionary<string, string> dicQuery)
	{
		if (item.AllowInsecure.Equals(Global.AllowInsecure.First()))
		{
			dicQuery.Add("insecure", "1");
			dicQuery.Add("allowInsecure", "1");
		}
		else
		{
			dicQuery.Add("insecure", "0");
			dicQuery.Add("allowInsecure", "0");
		}
		return 0;
	}

	protected static int ResolveUriQuery(NameValueCollection query, ref ProfileItem item)
	{
		TransportExtraItem transportExtraItem = item.GetTransportExtra();
		item.StreamSecurity = GetQueryValue(query, "security");
		item.Sni = GetQueryValue(query, "sni");
		item.Alpn = GetQueryDecoded(query, "alpn");
		item.Fingerprint = GetQueryDecoded(query, "fp");
		item.PublicKey = GetQueryDecoded(query, "pbk");
		item.ShortId = GetQueryDecoded(query, "sid");
		item.SpiderX = GetQueryDecoded(query, "spx");
		item.Mldsa65Verify = GetQueryDecoded(query, "pqv");
		item.EchConfigList = GetQueryDecoded(query, "ech");
		item.CertSha = GetQueryDecoded(query, "pcs");
		string queryDecoded = GetQueryDecoded(query, "fm");
		if (queryDecoded.IsNotEmpty())
		{
			JsonNode jsonNode = JsonUtils.ParseJson(queryDecoded);
			item.Finalmask = ((jsonNode != null) ? JsonUtils.Serialize(jsonNode, new JsonSerializerOptions
			{
				WriteIndented = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.Never,
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			}) : queryDecoded);
		}
		else
		{
			item.Finalmask = string.Empty;
		}
		if (_allowInsecureArray.Any((string k) => GetQueryDecoded(query, k) == "1"))
		{
			item.AllowInsecure = Global.AllowInsecure.First();
		}
		else if (_allowInsecureArray.Any((string k) => GetQueryDecoded(query, k) == "0"))
		{
			item.AllowInsecure = Global.AllowInsecure.Skip(1).First();
		}
		else
		{
			item.AllowInsecure = string.Empty;
		}
		string text = GetQueryValue(query, "type", "raw");
		if (text == "tcp")
		{
			text = "raw";
		}
		if (!Global.Networks.Contains(text))
		{
			text = "raw";
		}
		item.Network = text;
		switch (item.Network)
		{
		case "raw":
			transportExtraItem = transportExtraItem with
			{
				RawHeaderType = GetQueryValue(query, "headerType", "none"),
				Host = GetQueryDecoded(query, "host"),
				Path = GetQueryDecoded(query, "path")
			};
			break;
		case "kcp":
		{
			string queryDecoded2 = GetQueryDecoded(query, "seed");
			int num = (int.TryParse(GetQueryValue(query, "mtu"), out var result) ? result : 0);
			transportExtraItem = transportExtraItem with
			{
				KcpHeaderType = GetQueryValue(query, "headerType", "none"),
				KcpSeed = queryDecoded2,
				KcpMtu = ((num > 0) ? new int?(result) : ((int?)null))
			};
			break;
		}
		case "ws":
		case "httpupgrade":
			transportExtraItem = transportExtraItem with
			{
				Host = GetQueryDecoded(query, "host"),
				Path = GetQueryDecoded(query, "path", "/")
			};
			break;
		case "xhttp":
		{
			string text2 = GetQueryDecoded(query, "extra");
			if (text2.IsNotEmpty())
			{
				JsonNode jsonNode2 = JsonUtils.ParseJson(text2);
				if (jsonNode2 != null)
				{
					text2 = JsonUtils.Serialize(jsonNode2, new JsonSerializerOptions
					{
						WriteIndented = true,
						DefaultIgnoreCondition = JsonIgnoreCondition.Never,
						Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
					});
				}
			}
			transportExtraItem = transportExtraItem with
			{
				Host = GetQueryDecoded(query, "host"),
				Path = GetQueryDecoded(query, "path", "/"),
				XhttpMode = GetQueryDecoded(query, "mode"),
				XhttpExtra = text2
			};
			break;
		}
		case "grpc":
			transportExtraItem = transportExtraItem with
			{
				GrpcAuthority = GetQueryDecoded(query, "authority"),
				GrpcServiceName = GetQueryDecoded(query, "serviceName"),
				GrpcMode = GetQueryDecoded(query, "mode", "gun")
			};
			break;
		default:
			item.Network = "raw";
			break;
		}
		item.SetTransportExtra(transportExtraItem);
		return 0;
	}

	protected static bool Contains(string str, params string[] s)
	{
		return s.All((string item) => str.Contains(item, StringComparison.OrdinalIgnoreCase));
	}

	protected static string WriteAllText(string strData, string ext = "json")
	{
		string tempPath = Utils.GetTempPath(Utils.GetGuid(full: false) + "." + ext);
		File.WriteAllText(tempPath, strData);
		return tempPath;
	}

	protected static string ToUri(EConfigType eConfigType, string address, object port, string userInfo, Dictionary<string, string>? dicQuery, string? remark)
	{
		string text = ((dicQuery != null) ? ("?" + string.Join("&", dicQuery.Select((KeyValuePair<string, string> x) => x.Key + "=" + x.Value).ToArray())) : string.Empty);
		string text2 = $"{Utils.UrlEncode(userInfo)}@{GetIpv6(address)}:{port}";
		return Global.ProtocolShares[eConfigType] + text2 + text + remark;
	}

	protected static string GetQueryValue(NameValueCollection query, string key, string defaultValue = "")
	{
		return query[key] ?? defaultValue;
	}

	protected static string GetQueryDecoded(NameValueCollection query, string key, string defaultValue = "")
	{
		return Utils.UrlDecode(GetQueryValue(query, key, defaultValue));
	}
}

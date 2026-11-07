using System;
using System.Collections.Generic;
using System.Linq;
using ServiceLib.Common;
using ServiceLib.Enums;
using SQLite;

namespace ServiceLib.Models.Entities;

[Serializable]
public class ProfileItem
{
	private ProtocolExtraItem? _protocolExtraCache;

	private TransportExtraItem? _transportExtraCache;

	[PrimaryKey]
	public string IndexId { get; set; }

	public EConfigType ConfigType { get; set; }

	public ECoreType? CoreType { get; set; }

	public int ConfigVersion { get; set; }

	public string Subid { get; set; }

	public bool IsSub { get; set; } = true;

	public int? PreSocksPort { get; set; }

	public bool DisplayLog { get; set; } = true;

	public string Remarks { get; set; }

	public string Address { get; set; }

	public int Port { get; set; }

	public string Password { get; set; }

	public string Username { get; set; }

	public string Network { get; set; }

	[Obsolete("Use TransportExtra.RawHeaderType/XhttpMode/GrpcMode/KcpHeaderType instead.")]
	public string HeaderType { get; set; }

	[Obsolete("Use TransportExtra.Host/GrpcAuthority instead.")]
	public string RequestHost { get; set; }

	[Obsolete("Use TransportExtra.Path/GrpcServiceName/KcpSeed instead.")]
	public string Path { get; set; }

	public string StreamSecurity { get; set; }

	public string AllowInsecure { get; set; }

	public string Sni { get; set; }

	public string Alpn { get; set; } = string.Empty;

	public string Fingerprint { get; set; }

	public string PublicKey { get; set; }

	public string ShortId { get; set; }

	public string SpiderX { get; set; }

	public string Mldsa65Verify { get; set; }

	[Obsolete("Use TransportExtra.XhttpExtra instead.")]
	public string Extra { get; set; }

	public bool? MuxEnabled { get; set; }

	public string Cert { get; set; }

	public string CertSha { get; set; }

	public string EchConfigList { get; set; }

	public string Finalmask { get; set; }

	public string ProtoExtra { get; set; }

	public string TransportExtra { get; set; }

	[Obsolete("Use ProtocolExtraItem.Ports instead.")]
	public string Ports { get; set; }

	[Obsolete("Use ProtocolExtraItem.AlterId instead.")]
	public int AlterId { get; set; }

	[Obsolete("Use ProtocolExtraItem.Flow instead.")]
	public string Flow { get; set; }

	[Obsolete("Use ProfileItem.Password instead.")]
	public string Id { get; set; }

	[Obsolete("Use ProtocolExtraItem.xxx instead.")]
	public string Security { get; set; }

	public ProfileItem()
	{
		IndexId = string.Empty;
		ConfigType = EConfigType.VMess;
		ConfigVersion = 4;
		Subid = string.Empty;
		Address = string.Empty;
		Port = 0;
		Password = string.Empty;
		Username = string.Empty;
		Network = string.Empty;
		Remarks = string.Empty;
		StreamSecurity = string.Empty;
		AllowInsecure = string.Empty;
	}

	public string GetSummary()
	{
		string text = "[" + ConfigType.ToString() + "] ";
		if (IsComplex())
		{
			return text + "[" + CoreType.ToString() + "]" + Remarks;
		}
		string[] array = (Address.Contains(':') ? Address.Split(':') : Address.Split('.'));
		int num = array.Length;
		string text2;
		if (num > 2)
		{
			text2 = array.First() + "***" + array.Last();
		}
		else
		{
			text2 = ((num <= 1) ? Address : ("***" + array.Last()));
		}
		string value = text2;
		return text + $"{Remarks}({value}:{Port})";
	}

	public List<string>? GetAlpn()
	{
		if (Alpn.IsNullOrEmpty())
		{
			return null;
		}
		List<string> list = Utils.String2List(Alpn);
		if (list == null || list.Count == 0)
		{
			return null;
		}
		if (GetNetwork() != "ws")
		{
			return list;
		}
		List<string> list2 = list.Where((string a) => !a.Equals("h2", StringComparison.OrdinalIgnoreCase)).ToList();
		if (list2.Count <= 0)
		{
			return null;
		}
		return list2;
	}

	public string GetNetwork()
	{
		if (Network.IsNullOrEmpty() || !Global.Networks.Contains(Network))
		{
			return "raw";
		}
		return Network.TrimEx();
	}

	public bool IsComplex()
	{
		return ConfigType.IsComplexType();
	}

	public bool IsValid()
	{
		if (IsComplex())
		{
			return true;
		}
		bool flag = Address.IsNullOrEmpty();
		if (!flag)
		{
			int port = Port;
			bool flag2 = ((port <= 0 || port >= 65536) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		switch (ConfigType)
		{
		case EConfigType.VMess:
			if (Password.IsNullOrEmpty() || !Utils.IsGuidByParse(Password))
			{
				return false;
			}
			break;
		case EConfigType.VLESS:
			if (Password.IsNullOrEmpty() || (!Utils.IsGuidByParse(Password) && Password.Length > 30))
			{
				return false;
			}
			if (!Global.Flows.Contains(GetProtocolExtra().Flow ?? string.Empty))
			{
				return false;
			}
			break;
		case EConfigType.Shadowsocks:
			if (Password.IsNullOrEmpty())
			{
				return false;
			}
			if (string.IsNullOrEmpty(GetProtocolExtra().SsMethod) || !Global.SsSecuritiesInSingbox.Contains(GetProtocolExtra().SsMethod))
			{
				return false;
			}
			break;
		}
		EConfigType configType = ConfigType;
		flag = (uint)(configType - 5) <= 1u;
		if (flag && StreamSecurity == "reality" && PublicKey.IsNullOrEmpty())
		{
			return false;
		}
		return true;
	}

	public ProtocolExtraItem GetProtocolExtra()
	{
		ProtocolExtraItem protocolExtraItem = _protocolExtraCache;
		if ((object)protocolExtraItem == null)
		{
			ProtocolExtraItem? obj = JsonUtils.Deserialize<ProtocolExtraItem>(ProtoExtra) ?? new ProtocolExtraItem();
			ProtocolExtraItem protocolExtraItem2 = obj;
			_protocolExtraCache = obj;
			protocolExtraItem = protocolExtraItem2;
		}
		return protocolExtraItem;
	}

	public void SetProtocolExtra(ProtocolExtraItem extraItem)
	{
		_protocolExtraCache = extraItem;
		ProtoExtra = JsonUtils.Serialize(extraItem, indented: false);
	}

	public TransportExtraItem GetTransportExtra()
	{
		TransportExtraItem transportExtraItem = _transportExtraCache;
		if ((object)transportExtraItem == null)
		{
			TransportExtraItem? obj = JsonUtils.Deserialize<TransportExtraItem>(TransportExtra) ?? new TransportExtraItem();
			TransportExtraItem transportExtraItem2 = obj;
			_transportExtraCache = obj;
			transportExtraItem = transportExtraItem2;
		}
		return transportExtraItem;
	}

	public void SetTransportExtra(TransportExtraItem transportExtra)
	{
		_transportExtraCache = transportExtra;
		TransportExtra = JsonUtils.Serialize(transportExtra, indented: false);
	}
}

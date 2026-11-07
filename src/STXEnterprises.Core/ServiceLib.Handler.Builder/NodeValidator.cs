using System.Collections.Generic;
using System.Text.Json.Nodes;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Builder;

public class NodeValidator
{
	private class ValidationContext
	{
		public List<string> Errors { get; } = new List<string>();

		public List<string> Warnings { get; } = new List<string>();

		public void Error(string message)
		{
			Errors.Add(message);
		}

		public void Warning(string message)
		{
			Warnings.Add(message);
		}

		public void Assert(bool condition, string errorMsg)
		{
			if (!condition)
			{
				Error(errorMsg);
			}
		}

		public NodeValidatorResult ToResult()
		{
			return new NodeValidatorResult(Errors, Warnings);
		}
	}

	private static readonly HashSet<string> SingboxUnsupportedTransports = new HashSet<string> { "kcp", "xhttp" };

	private static readonly HashSet<EConfigType> SingboxTransportSupportedProtocols = new HashSet<EConfigType>
	{
		EConfigType.VMess,
		EConfigType.VLESS,
		EConfigType.Trojan,
		EConfigType.Shadowsocks
	};

	private static readonly HashSet<string> SingboxShadowsocksAllowedTransports = new HashSet<string> { "raw", "ws" };

	public static NodeValidatorResult Validate(ProfileItem item, ECoreType coreType)
	{
		ValidationContext validationContext = new ValidationContext();
		ValidateNodeAndCoreSupport(item, coreType, validationContext);
		return validationContext.ToResult();
	}

	private static void ValidateNodeAndCoreSupport(ProfileItem item, ECoreType coreType, ValidationContext v)
	{
		if (item.ConfigType == EConfigType.Custom || item.ConfigType.IsGroupType())
		{
			return;
		}
		v.Assert(!item.Address.IsNullOrEmpty(), string.Format(ResUI.MsgInvalidProperty, "Address"));
		int port = item.Port;
		v.Assert(port > 0 && port <= 65535, string.Format(ResUI.MsgInvalidProperty, "Port"));
		string network = item.GetNetwork();
		switch (coreType)
		{
		case ECoreType.sing_box:
		{
			string text = ValidateSingboxTransport(item.ConfigType, network);
			if (text != null)
			{
				v.Error(text);
			}
			if (!Global.SingboxSupportConfigType.Contains(item.ConfigType))
			{
				v.Error(string.Format(ResUI.MsgCoreNotSupportProtocol, "sing_box", item.ConfigType));
			}
			break;
		}
		case ECoreType.Xray:
			if (!Global.XraySupportConfigType.Contains(item.ConfigType))
			{
				v.Error(string.Format(ResUI.MsgCoreNotSupportProtocol, "Xray", item.ConfigType));
			}
			break;
		}
		ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
		switch (item.ConfigType)
		{
		case EConfigType.VMess:
			v.Assert(!item.Password.IsNullOrEmpty() && Utils.IsGuidByParse(item.Password), string.Format(ResUI.MsgInvalidProperty, "Password"));
			break;
		case EConfigType.VLESS:
			v.Assert(!item.Password.IsNullOrEmpty() && (Utils.IsGuidByParse(item.Password) || item.Password.Length <= 30), string.Format(ResUI.MsgInvalidProperty, "Password"));
			v.Assert(Global.Flows.Contains(protocolExtra.Flow ?? string.Empty), string.Format(ResUI.MsgInvalidProperty, "Flow"));
			break;
		case EConfigType.Shadowsocks:
			v.Assert(!item.Password.IsNullOrEmpty(), string.Format(ResUI.MsgInvalidProperty, "Password"));
			v.Assert(!string.IsNullOrEmpty(protocolExtra.SsMethod) && Global.SsSecuritiesInSingbox.Contains(protocolExtra.SsMethod), string.Format(ResUI.MsgInvalidProperty, "SsMethod"));
			break;
		}
		if (item.StreamSecurity == "tls")
		{
			if (!item.Cert.IsNullOrEmpty() && CertPemManager.ParsePemChain(item.Cert).Count == 0 && !item.CertSha.IsNullOrEmpty())
			{
				v.Error(string.Format(ResUI.MsgInvalidProperty, "TLS Certificate"));
			}
			if (item.AllowInsecure == "true")
			{
				v.Warning(ResUI.MsgAllowInsecureDeprecated);
			}
		}
		if (item.StreamSecurity == "reality")
		{
			v.Assert(!item.PublicKey.IsNullOrEmpty(), string.Format(ResUI.MsgInvalidProperty, "PublicKey"));
		}
		TransportExtraItem transportExtra = item.GetTransportExtra();
		if (item.Network == "xhttp" && !transportExtra.XhttpExtra.IsNullOrEmpty() && !(JsonUtils.ParseJson(transportExtra.XhttpExtra) is JsonObject))
		{
			v.Error(string.Format(ResUI.MsgInvalidProperty, "XHTTP Extra"));
		}
		if (!item.Finalmask.IsNullOrEmpty() && !(JsonUtils.ParseJson(item.Finalmask) is JsonObject))
		{
			v.Error(string.Format(ResUI.MsgInvalidProperty, "Finalmask"));
		}
	}

	private static string? ValidateSingboxTransport(EConfigType configType, string net)
	{
		if (SingboxUnsupportedTransports.Contains(net))
		{
			return string.Format(ResUI.MsgCoreNotSupportNetwork, "sing_box", net);
		}
		if (!SingboxTransportSupportedProtocols.Contains(configType) && net != "raw")
		{
			return string.Format(ResUI.MsgCoreNotSupportProtocolTransport, "sing_box", configType.ToString(), net);
		}
		if (configType == EConfigType.Shadowsocks && !SingboxShadowsocksAllowedTransports.Contains(net))
		{
			return string.Format(ResUI.MsgCoreNotSupportProtocolTransport, "sing_box", configType.ToString(), net);
		}
		return null;
	}
}

using System;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class FmtHandler
{
	private static readonly string _tag = "FmtHandler";

	public static string? GetShareUri(ProfileItem item)
	{
		try
		{
			return item.ConfigType switch
			{
				EConfigType.VMess => VmessFmt.ToUri(item), 
				EConfigType.Shadowsocks => ShadowsocksFmt.ToUri(item), 
				EConfigType.SOCKS => SocksFmt.ToUri(item), 
				EConfigType.Trojan => TrojanFmt.ToUri(item), 
				EConfigType.VLESS => VLESSFmt.ToUri(item), 
				EConfigType.Hysteria2 => Hysteria2Fmt.ToUri(item), 
				EConfigType.TUIC => TuicFmt.ToUri(item), 
				EConfigType.WireGuard => WireguardFmt.ToUri(item), 
				EConfigType.Anytls => AnytlsFmt.ToUri(item), 
				EConfigType.Naive => NaiveFmt.ToUri(item), 
				_ => null, 
			};
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return string.Empty;
		}
	}

	public static ProfileItem? ResolveConfig(string config, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		try
		{
			string text = config.TrimEx();
			if (text.IsNullOrEmpty())
			{
				msg = ResUI.FailedReadConfiguration;
				return null;
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.VMess]))
			{
				return VmessFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.Shadowsocks]))
			{
				return ShadowsocksFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.SOCKS]) || text.StartsWith("socks5://") || text.StartsWith("socks4://"))
			{
				return SocksFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.Trojan]))
			{
				return TrojanFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.VLESS]))
			{
				return VLESSFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.Hysteria2]) || text.StartsWith("hy2://"))
			{
				return Hysteria2Fmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.TUIC]))
			{
				return TuicFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.WireGuard]))
			{
				return WireguardFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.Anytls]))
			{
				return AnytlsFmt.Resolve(text, out msg);
			}
			if (text.StartsWith(Global.ProtocolShares[EConfigType.Naive]) || text.StartsWith("naive+https://") || text.StartsWith("naive+quic://"))
			{
				return NaiveFmt.Resolve(text, out msg);
			}
			msg = ResUI.NonvmessOrssProtocol;
			return null;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			msg = ResUI.Incorrectconfiguration;
			return null;
		}
	}
}

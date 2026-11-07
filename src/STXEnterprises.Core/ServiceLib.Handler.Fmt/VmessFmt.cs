using System;
using System.Linq;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class VmessFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		if (str.IndexOf('@') > 0)
		{
			return ResolveStdVmess(str) ?? ResolveVmess(str, out msg);
		}
		return ResolveVmess(str, out msg);
	}

	public static string? ToUri(ProfileItem? item)
	{
		if (item == null)
		{
			return null;
		}
		VmessQRCode vmessQRCode = new VmessQRCode();
		vmessQRCode.v = 2;
		vmessQRCode.ps = item.Remarks.TrimEx();
		vmessQRCode.add = item.Address;
		vmessQRCode.port = item.Port;
		vmessQRCode.id = item.Password;
		vmessQRCode.aid = (int.TryParse(item.GetProtocolExtra()?.AlterId, out var result) ? result : 0);
		vmessQRCode.scy = item.GetProtocolExtra().VmessSecurity ?? "";
		vmessQRCode.net = ((item.GetNetwork() == "raw") ? "tcp" : item.Network);
		VmessQRCode vmessQRCode2 = vmessQRCode;
		vmessQRCode2.type = item.GetNetwork() switch
		{
			"raw" => item.GetTransportExtra().RawHeaderType, 
			"kcp" => item.GetTransportExtra().KcpHeaderType, 
			"xhttp" => item.GetTransportExtra().XhttpMode, 
			"grpc" => item.GetTransportExtra().GrpcMode, 
			_ => "none", 
		};
		VmessQRCode vmessQRCode3 = vmessQRCode;
		vmessQRCode3.host = item.GetNetwork() switch
		{
			"raw" => item.GetTransportExtra().Host, 
			"ws" => item.GetTransportExtra().Host, 
			"httpupgrade" => item.GetTransportExtra().Host, 
			"xhttp" => item.GetTransportExtra().Host, 
			"grpc" => item.GetTransportExtra().GrpcAuthority, 
			_ => null, 
		};
		VmessQRCode vmessQRCode4 = vmessQRCode;
		vmessQRCode4.path = item.GetNetwork() switch
		{
			"raw" => item.GetTransportExtra().Path, 
			"kcp" => item.GetTransportExtra().KcpSeed, 
			"ws" => item.GetTransportExtra().Path, 
			"httpupgrade" => item.GetTransportExtra().Path, 
			"xhttp" => item.GetTransportExtra().Path, 
			"grpc" => item.GetTransportExtra().GrpcServiceName, 
			_ => null, 
		};
		vmessQRCode.tls = item.StreamSecurity;
		vmessQRCode.sni = item.Sni;
		vmessQRCode.alpn = item.Alpn;
		vmessQRCode.fp = item.Fingerprint;
		vmessQRCode.insecure = (item.AllowInsecure.Equals(Global.AllowInsecure.First()) ? "1" : "0");
		string plainText = JsonUtils.Serialize(vmessQRCode);
		plainText = Utils.Base64Encode(plainText);
		return Global.ProtocolShares[EConfigType.VMess] + plainText;
	}

	private static ProfileItem? ResolveVmess(string result, out string msg)
	{
		msg = string.Empty;
		ProfileItem profileItem = new ProfileItem
		{
			ConfigType = EConfigType.VMess
		};
		result = result.Substring(Global.ProtocolShares[EConfigType.VMess].Length);
		result = Utils.Base64Decode(result);
		VmessQRCode vmessQRCode = JsonUtils.Deserialize<VmessQRCode>(result);
		if (vmessQRCode == null)
		{
			msg = ResUI.FailedConversionConfiguration;
			return null;
		}
		profileItem.Network = "raw";
		TransportExtraItem transportExtraItem = new TransportExtraItem
		{
			RawHeaderType = "none"
		};
		profileItem.Remarks = Utils.ToString(vmessQRCode.ps);
		profileItem.Address = Utils.ToString(vmessQRCode.add);
		profileItem.Port = vmessQRCode.port;
		profileItem.Password = Utils.ToString(vmessQRCode.id);
		profileItem.SetProtocolExtra(new ProtocolExtraItem
		{
			AlterId = vmessQRCode.aid.ToString(),
			VmessSecurity = (vmessQRCode.scy.IsNullOrEmpty() ? "auto" : vmessQRCode.scy)
		});
		if (vmessQRCode.net.IsNotEmpty())
		{
			profileItem.Network = ((vmessQRCode.net == "tcp") ? "raw" : vmessQRCode.net);
		}
		if (vmessQRCode.type.IsNotEmpty())
		{
			transportExtraItem = profileItem.GetNetwork() switch
			{
				"raw" => transportExtraItem with
				{
					RawHeaderType = vmessQRCode.type
				}, 
				"kcp" => transportExtraItem with
				{
					KcpHeaderType = vmessQRCode.type
				}, 
				"xhttp" => transportExtraItem with
				{
					XhttpMode = vmessQRCode.type
				}, 
				"grpc" => transportExtraItem with
				{
					GrpcMode = vmessQRCode.type
				}, 
				_ => transportExtraItem, 
			};
		}
		profileItem.SetTransportExtra(profileItem.GetNetwork() switch
		{
			"raw" => transportExtraItem with
			{
				Host = Utils.ToString(vmessQRCode.host),
				Path = Utils.ToString(vmessQRCode.path)
			}, 
			"kcp" => transportExtraItem with
			{
				KcpSeed = Utils.ToString(vmessQRCode.path)
			}, 
			"ws" => transportExtraItem with
			{
				Host = Utils.ToString(vmessQRCode.host),
				Path = Utils.ToString(vmessQRCode.path)
			}, 
			"httpupgrade" => transportExtraItem with
			{
				Host = Utils.ToString(vmessQRCode.host),
				Path = Utils.ToString(vmessQRCode.path)
			}, 
			"xhttp" => transportExtraItem with
			{
				Host = Utils.ToString(vmessQRCode.host),
				Path = Utils.ToString(vmessQRCode.path)
			}, 
			"grpc" => transportExtraItem with
			{
				GrpcAuthority = Utils.ToString(vmessQRCode.host),
				GrpcServiceName = Utils.ToString(vmessQRCode.path)
			}, 
			_ => transportExtraItem, 
		});
		profileItem.StreamSecurity = Utils.ToString(vmessQRCode.tls);
		profileItem.Sni = Utils.ToString(vmessQRCode.sni);
		profileItem.Alpn = Utils.ToString(vmessQRCode.alpn);
		profileItem.Fingerprint = Utils.ToString(vmessQRCode.fp);
		profileItem.AllowInsecure = ((vmessQRCode.insecure == "1") ? Global.AllowInsecure.First() : string.Empty);
		return profileItem;
	}

	public static ProfileItem? ResolveStdVmess(string str)
	{
		ProfileItem item = new ProfileItem
		{
			ConfigType = EConfigType.VMess
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
		item.SetProtocolExtra(new ProtocolExtraItem
		{
			VmessSecurity = "auto"
		});
		BaseFmt.ResolveUriQuery(Utils.ParseQueryString(uri.Query), ref item);
		return item;
	}
}

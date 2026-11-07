using System;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;

namespace ServiceLib.Handler.SysProxy;

public static class SysProxyHandler
{
	private static readonly string _tag = "SysProxyHandler";

	public static async Task<bool> UpdateSysProxy(Config config, bool forceDisable)
	{
		ESysProxyType eSysProxyType = config.SystemProxyItem.SysProxyType;
		if (forceDisable && eSysProxyType != ESysProxyType.Unchanged)
		{
			eSysProxyType = ESysProxyType.ForcedClear;
		}
		try
		{
			int localPort = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
			string exceptions = config.SystemProxyItem.SystemProxyExceptions.Replace(" ", "");
			if (localPort <= 0)
			{
				return false;
			}
			switch (eSysProxyType)
			{
			case ESysProxyType.ForcedChange:
				if (Utils.IsWindows())
				{
					GetWindowsProxyString(config, localPort, out string strProxy, out string strExceptions);
					ProxySettingWindows.SetProxy(strProxy, strExceptions, 2);
				}
				else if (Utils.IsLinux())
				{
					await ProxySettingLinux.SetProxy("127.0.0.1", localPort, exceptions);
				}
				else if (Utils.IsMacOS())
				{
					await ProxySettingOSX.SetProxy("127.0.0.1", localPort, exceptions);
				}
				break;
			case ESysProxyType.ForcedClear:
				if (Utils.IsWindows())
				{
					ProxySettingWindows.UnsetProxy();
				}
				else if (Utils.IsLinux())
				{
					await ProxySettingLinux.UnsetProxy();
				}
				else if (Utils.IsMacOS())
				{
					await ProxySettingOSX.UnsetProxy();
				}
				break;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return true;
	}

	private static void GetWindowsProxyString(Config config, int port, out string strProxy, out string strExceptions)
	{
		strExceptions = config.SystemProxyItem.SystemProxyExceptions?.Replace(" ", "") ?? string.Empty;
		string localBypass = "<local>;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;[::1]";
		
		if (!strExceptions.Contains("127.*"))
		{
			strExceptions = localBypass + (string.IsNullOrEmpty(strExceptions) ? "" : (";" + strExceptions));
		}

		strProxy = string.Empty;
		if (config.SystemProxyItem.SystemProxyAdvancedProtocol.IsNullOrEmpty())
		{
			strProxy = $"{"127.0.0.1"}:{port}";
		}
		else
		{
			strProxy = config.SystemProxyItem.SystemProxyAdvancedProtocol.Replace("{ip}", "127.0.0.1").Replace("{http_port}", port.ToString()).Replace("{socks_port}", port.ToString());
		}
	}
}

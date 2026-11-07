using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Manager;

namespace ServiceLib.Handler.SysProxy;

[SupportedOSPlatform("macos")]
public static class ProxySettingOSX
{
	private static readonly string _proxySetFileName = "ServiceLib.Sample.proxy_set_osx_sh".Replace("ServiceLib.Sample.", "") + ".sh";

	public static async Task SetProxy(string host, int port, string exceptions)
	{
		List<string> list = new List<string>(3)
		{
			"set",
			host,
			port.ToString()
		};
		if (exceptions.IsNotEmpty())
		{
			list.AddRange(exceptions.Split(','));
		}
		await ExecCmd(list);
	}

	public static async Task UnsetProxy()
	{
		await ExecCmd(new List<string>(1) { "clear" });
	}

	private static async Task ExecCmd(List<string> args)
	{
		string text = AppManager.Instance.Config.SystemProxyItem?.CustomSystemProxyScriptPath;
		string text2 = ((!text.IsNotEmpty() || !File.Exists(text)) ? (await FileUtils.CreateLinuxShellFile(_proxySetFileName, EmbedUtils.GetEmbedText("ServiceLib.Sample.proxy_set_osx_sh"), overwrite: false)) : text);
		string text3 = text2;
		NoticeManager.Instance.SendMessage(text3);
		await Utils.GetCliWrapOutput(text3, args);
	}
}

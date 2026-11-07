using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Manager;

namespace ServiceLib.Handler.SysProxy;

[SupportedOSPlatform("linux")]
public static class ProxySettingLinux
{
	private static readonly string _proxySetFileName = "ServiceLib.Sample.proxy_set_linux_sh".Replace("ServiceLib.Sample.", "") + ".sh";

	public static async Task SetProxy(string host, int port, string exceptions)
	{
		await ExecCmd(new List<string>(4)
		{
			"manual",
			host,
			port.ToString(),
			exceptions
		});
	}

	public static async Task UnsetProxy()
	{
		await ExecCmd(new List<string>(1) { "none" });
	}

	private static async Task ExecCmd(List<string> args)
	{
		string text = AppManager.Instance.Config.SystemProxyItem?.CustomSystemProxyScriptPath;
		string text2 = ((!text.IsNotEmpty() || !File.Exists(text)) ? (await FileUtils.CreateLinuxShellFile(_proxySetFileName, EmbedUtils.GetEmbedText("ServiceLib.Sample.proxy_set_linux_sh"), overwrite: false)) : text);
		string text3 = text2;
		NoticeManager.Instance.SendMessage(text3);
		await Utils.GetCliWrapOutput(text3, args);
	}
}

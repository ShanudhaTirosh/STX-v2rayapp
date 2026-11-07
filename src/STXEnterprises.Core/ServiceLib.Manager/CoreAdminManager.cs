using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using ServiceLib.Common;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Resx;
using ServiceLib.Services;

namespace ServiceLib.Manager;

public class CoreAdminManager
{
	private static readonly Lazy<CoreAdminManager> _instance = new Lazy<CoreAdminManager>(() => new CoreAdminManager());

	private Config _config;

	private Func<bool, string, Task>? _updateFunc;

	private int _linuxSudoPid = -1;

	private const string _tag = "CoreAdminHandler";

	public static CoreAdminManager Instance => _instance.Value;

	public async Task Init(Config config, Func<bool, string, Task> updateFunc)
	{
		if (_config == null)
		{
			_config = config;
			_updateFunc = updateFunc;
			await Task.CompletedTask;
		}
	}

	private async Task UpdateFunc(bool notify, string msg)
	{
		await (_updateFunc?.Invoke(notify, msg));
	}

	public async Task<ProcessService?> RunProcessAsLinuxSudo(string fileName, CoreInfo coreInfo, string configPath)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("#!/bin/bash");
		string value = fileName.AppendQuotes() + " " + string.Format(coreInfo.Arguments, Utils.GetBinConfigPath(configPath).AppendQuotes());
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("exec sudo -S -- ");
		handler.AppendFormatted(value);
		stringBuilder2.AppendLine(ref handler);
		ProcessService procService = new ProcessService(await FileUtils.CreateLinuxShellFile("run_as_sudo.sh", stringBuilder.ToString(), overwrite: true), "", Utils.GetBinConfigPath(), displayLog: true, redirectInput: true, null, _updateFunc);
		await procService.StartAsync(AppManager.Instance.LinuxSudoPwd);
		if ((procService == null || procService.HasExited) ? true : false)
		{
			throw new Exception(ResUI.FailedToRunCore);
		}
		_linuxSudoPid = procService.Id;
		return procService;
	}

	public async Task KillProcessAsLinuxSudo()
	{
		if (_linuxSudoPid < 0)
		{
			return;
		}
		try
		{
			string res = (Utils.IsMacOS() ? "ServiceLib.Sample.kill_as_sudo_osx_sh" : "ServiceLib.Sample.kill_as_sudo_linux_sh");
			string text = await FileUtils.CreateLinuxShellFile("kill_as_sudo.sh", EmbedUtils.GetEmbedText(res), overwrite: true);
			if (text.Contains(' '))
			{
				text = text.AppendQuotes();
			}
			List<string> arguments = new List<string>
			{
				"-c",
				$"sudo -S {text} {_linuxSudoPid}"
			};
			await UpdateFunc(notify: false, (await Cli.Wrap("/bin/bash").WithArguments(arguments).WithStandardInputPipe(PipeSource.FromString(AppManager.Instance.LinuxSudoPwd))
				.ExecuteBufferedAsync()).StandardOutput.ToString());
		}
		catch (Exception ex)
		{
			Logging.SaveLog("CoreAdminHandler", ex);
		}
		_linuxSudoPid = -1;
	}
}

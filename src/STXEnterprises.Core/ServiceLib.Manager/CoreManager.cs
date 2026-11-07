using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Handler;
using ServiceLib.Handler.Builder;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;
using ServiceLib.Services;

namespace ServiceLib.Manager;

public class CoreManager
{
	private static readonly Lazy<CoreManager> _instance = new Lazy<CoreManager>(() => new CoreManager());

	private Config _config;

	[SupportedOSPlatform("windows")]
	private WindowsJobService? _processJob;

	private ProcessService? _processService;

	private ProcessService? _processPreService;

	private bool _linuxSudo;

	private Func<bool, string, Task>? _updateFunc;

	private const string _tag = "CoreHandler";

	public static CoreManager Instance => _instance.Value;

	public async Task Init(Config config, Func<bool, string, Task> updateFunc)
	{
		_config = config;
		_updateFunc = updateFunc;
		if (Environment.GetEnvironmentVariable("STXENTERPRISES_LOCAL_APPLICATION_DATA") == "1" || Environment.GetEnvironmentVariable("PXNSHIELD_LOCAL_APPLICATION_DATA") == "1")
		{
			string baseDirectory = Utils.GetBaseDirectory("bin");
			string binPath = Utils.GetBinPath("");
			if (baseDirectory != binPath)
			{
				FileUtils.CopyDirectory(baseDirectory, binPath, recursive: true, overwrite: false);
			}
		}
		if (!Utils.IsNonWindows())
		{
			return;
		}
		List<CoreInfo> coreInfo = CoreInfoManager.Instance.GetCoreInfo();
		foreach (CoreInfo it in coreInfo)
		{
			if (it.CoreType == ECoreType.app)
			{
				if (Utils.UpgradeAppExists(out string upgradeFileName))
				{
					await Utils.SetLinuxChmod(upgradeFileName);
				}
				continue;
			}
			foreach (string coreEx in it.CoreExes)
			{
				string binPath2 = Utils.GetBinPath(Utils.GetExeName(coreEx), it.CoreType.ToString());
				if (File.Exists(binPath2))
				{
					await Utils.SetLinuxChmod(binPath2);
				}
			}
		}
	}

	public async Task LoadCore(CoreConfigContext? mainContext, CoreConfigContext? preContext)
	{
		if (mainContext == null)
		{
			await UpdateFunc(notify: false, ResUI.CheckServerSettings);
			return;
		}
		ProfileItem node = mainContext.Node;
		string binConfigPath = Utils.GetBinConfigPath("config.json");
		RetResult retResult = await CoreConfigHandler.GenerateClientConfig(mainContext, binConfigPath);
		if (!retResult.Success)
		{
			await UpdateFunc(notify: true, retResult.Msg);
			return;
		}
		await UpdateFunc(notify: false, node.GetSummary() ?? "");
		await UpdateFunc(notify: false, Utils.GetRuntimeInfo() ?? "");
		await UpdateFunc(notify: false, string.Format(ResUI.StartService, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
		await CoreStop();
		await Task.Delay(100);
		if (Utils.IsWindows() && _config.TunModeItem.EnableTun)
		{
			await Task.Delay(100);
			await WindowsUtils.RemoveTunDevice();
		}
		await CoreStart(mainContext);
		await CoreStartPreService(preContext);
		AppManager.Instance.RunningCoreType = mainContext.RunCoreType;
		if (_processService != null)
		{
			await UpdateFunc(notify: true, node.GetSummary() ?? "");
		}
	}

	public async Task<ProcessService?> LoadCoreConfigSpeedtest(List<ServerTestItem> selecteds)
	{
		ServerTestItem? serverTestItem = selecteds.FirstOrDefault();
		ECoreType coreType = ((serverTestItem != null && serverTestItem.CoreType == ECoreType.sing_box) ? ECoreType.sing_box : ECoreType.Xray);
		string fileName = $"configTest{Utils.GetGuid(full: false)}.json";
		string configPath = Utils.GetBinConfigPath(fileName);
		RetResult result = await CoreConfigHandler.GenerateClientSpeedtestConfig(_config, configPath, selecteds, coreType);
		await UpdateFunc(notify: false, result.Msg);
		if (!result.Success)
		{
			return null;
		}
		await UpdateFunc(notify: false, string.Format(ResUI.StartService, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
		await UpdateFunc(notify: false, configPath);
		CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(coreType);
		return await RunProcess(coreInfo, fileName, displayLog: true, mayNeedSudo: false);
	}

	public async Task<ProcessService?> LoadCoreConfigSpeedtest(ServerTestItem testItem)
	{
		ProfileItem profileItem = await AppManager.Instance.GetProfileItem(testItem.IndexId);
		if (profileItem == null)
		{
			return null;
		}
		string fileName = $"configTest{Utils.GetGuid(full: false)}.json";
		string configPath = Utils.GetBinConfigPath(fileName);
		var (context, _) = await CoreConfigContextBuilder.Build(_config, profileItem);
		if (!(await CoreConfigHandler.GenerateClientSpeedtestConfig(_config, context, testItem, configPath)).Success)
		{
			return null;
		}
		ECoreType runCoreType = context.RunCoreType;
		CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(runCoreType);
		return await RunProcess(coreInfo, fileName, displayLog: true, mayNeedSudo: false);
	}

	public async Task CoreStop()
	{
		_ = 2;
		try
		{
			if (_linuxSudo)
			{
				await CoreAdminManager.Instance.KillProcessAsLinuxSudo();
				_linuxSudo = false;
			}
			if (_processService != null)
			{
				await _processService.StopAsync();
				_processService.Dispose();
				_processService = null;
			}
			if (_processPreService != null)
			{
				await _processPreService.StopAsync();
				_processPreService.Dispose();
				_processPreService = null;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("CoreHandler", ex);
		}
	}

	private async Task CoreStart(CoreConfigContext context)
	{
		ProfileItem node = context.Node;
		ECoreType coreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
		CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(coreType);
		bool displayLog = node.ConfigType != EConfigType.Custom || node.DisplayLog;
		ProcessService processService = await RunProcess(coreInfo, "config.json", displayLog, mayNeedSudo: true);
		if (processService != null)
		{
			_processService = processService;
		}
	}

	private async Task CoreStartPreService(CoreConfigContext? preContext)
	{
		ProcessService processService = _processService;
		if (processService == null || processService.HasExited || !(preContext != null))
		{
			return;
		}
		ECoreType preCoreType = preContext?.Node?.CoreType ?? ECoreType.sing_box;
		string binConfigPath = Utils.GetBinConfigPath("configPre.json");
		if ((await CoreConfigHandler.GenerateClientConfig(preContext, binConfigPath)).Success)
		{
			CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(preCoreType);
			ProcessService processService2 = await RunProcess(coreInfo, "configPre.json", displayLog: true, mayNeedSudo: true);
			if (processService2 != null)
			{
				_processPreService = processService2;
			}
		}
	}

	private async Task UpdateFunc(bool notify, string msg)
	{
		await (_updateFunc?.Invoke(notify, msg));
	}

	private async Task<ProcessService?> RunProcess(CoreInfo? coreInfo, string configPath, bool displayLog, bool mayNeedSudo)
	{
		string fileName = CoreInfoManager.Instance.GetCoreExecFile(coreInfo, out string msg);
		if (fileName.IsNullOrEmpty())
		{
			await UpdateFunc(notify: false, msg);
			return null;
		}
		try
		{
			bool flag = mayNeedSudo && _config.TunModeItem.EnableTun;
			if (flag)
			{
				ECoreType coreType = coreInfo.CoreType;
				bool flag2 = ((coreType == ECoreType.mihomo || coreType == ECoreType.sing_box) ? true : false);
				flag = flag2;
			}
			if (flag && Utils.IsNonWindows())
			{
				_linuxSudo = true;
				await CoreAdminManager.Instance.Init(_config, _updateFunc);
				return await CoreAdminManager.Instance.RunProcessAsLinuxSudo(fileName, coreInfo, configPath);
			}
			return await RunProcessNormal(fileName, coreInfo, configPath, displayLog);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("CoreHandler", ex);
			await UpdateFunc(mayNeedSudo, ex.Message);
			return null;
		}
	}

	private async Task<ProcessService?> RunProcessNormal(string fileName, CoreInfo? coreInfo, string configPath, bool displayLog)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> item in coreInfo.Environment)
		{
			dictionary[item.Key] = string.Format(item.Value, coreInfo.AbsolutePath ? Utils.GetBinConfigPath(configPath).AppendQuotes() : configPath);
		}
		ProcessService procService = new ProcessService(fileName, string.Format(coreInfo.Arguments, coreInfo.AbsolutePath ? Utils.GetBinConfigPath(configPath).AppendQuotes() : configPath), Utils.GetBinConfigPath(), displayLog, redirectInput: false, dictionary, _updateFunc);
		await procService.StartAsync();
		await Task.Delay(100);
		if ((procService == null || procService.HasExited) ? true : false)
		{
			throw new Exception(ResUI.FailedToRunCore);
		}
		AddProcessJob(procService.Handle);
		return procService;
	}

	private void AddProcessJob(nint processHandle)
	{
		if (Utils.IsWindows())
		{
			if (_processJob == null)
			{
				_processJob = new WindowsJobService();
			}
			try
			{
				_processJob?.AddProcess(processHandle);
			}
			catch
			{
			}
		}
	}
}

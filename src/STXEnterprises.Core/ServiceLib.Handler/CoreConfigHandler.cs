using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Handler.Builder;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;
using ServiceLib.Services.CoreConfig;

namespace ServiceLib.Handler;

public static class CoreConfigHandler
{
	private static readonly string _tag = "CoreConfigHandler";

	public static async Task<RetResult> GenerateClientConfig(CoreConfigContext context, string? fileName)
	{
		_ = AppManager.Instance.Config;
		new RetResult();
		ProfileItem node = context.Node;
		RetResult result;
		if (node.ConfigType != EConfigType.Custom)
		{
			result = ((context.RunCoreType != ECoreType.sing_box) ? new CoreConfigV2rayService(context).GenerateClientConfigContent() : new CoreConfigSingboxService(context).GenerateClientConfigContent());
		}
		else
		{
			_ = node.CoreType;
			result = await GenerateClientCustomConfig(node, fileName);
		}
		if (!result.Success)
		{
			return result;
		}
		if (fileName.IsNotEmpty() && result.Data != null)
		{
			await File.WriteAllTextAsync(fileName, result.Data.ToString());
		}
		return result;
	}

	private static async Task<RetResult> GenerateClientCustomConfig(ProfileItem node, string? fileName)
	{
		RetResult ret = new RetResult();
		try
		{
			if (node == null || fileName == null)
			{
				ret.Msg = ResUI.CheckServerSettings;
				return ret;
			}
			if (File.Exists(fileName))
			{
				File.SetAttributes(fileName, FileAttributes.Normal);
				File.Delete(fileName);
			}
			string text = node.Address;
			if (!File.Exists(text))
			{
				text = Utils.GetConfigPath(text);
			}
			if (!File.Exists(text))
			{
				ret.Msg = ResUI.FailedGenDefaultConfiguration;
				return ret;
			}
			File.Copy(text, fileName);
			File.SetAttributes(fileName, FileAttributes.Normal);
			if (!File.Exists(fileName))
			{
				ret.Msg = ResUI.FailedGenDefaultConfiguration;
				return ret;
			}
			ret.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
			ret.Success = true;
			return await Task.FromResult(ret);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			ret.Msg = ResUI.FailedGenDefaultConfiguration;
			return ret;
		}
	}

	public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, string fileName, List<ServerTestItem> selecteds, ECoreType coreType)
	{
		RetResult result = new RetResult();
		ProfileItem node = new ProfileItem
		{
			CoreType = coreType
		};
		CoreConfigContext context = (await CoreConfigContextBuilder.Build(config, node)).Context;
		foreach (ServerTestItem selected in selecteds)
		{
			ProfileItem node2 = selected.Profile;
			ProfileItem item = (await CoreConfigContextBuilder.ResolveNodeAsync(context, node2)).Item1;
			if (!(node2.IndexId == item.IndexId))
			{
				context.ServerTestItemMap[node2.IndexId] = item.IndexId;
			}
		}
		switch (coreType)
		{
		case ECoreType.sing_box:
			result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(selecteds);
			break;
		case ECoreType.Xray:
			result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(selecteds);
			break;
		}
		if (!result.Success)
		{
			return result;
		}
		await File.WriteAllTextAsync(fileName, result.Data.ToString());
		return result;
	}

	public static async Task<RetResult> GenerateClientSpeedtestConfig(Config config, CoreConfigContext context, ServerTestItem testItem, string fileName)
	{
		new RetResult();
		int port = (testItem.Port = Utils.GetFreePort(checked(AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest) + testItem.QueueNum)));
		RetResult result = ((context.RunCoreType != ECoreType.sing_box) ? new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(port) : new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(port));
		if (!result.Success)
		{
			return result;
		}
		await File.WriteAllTextAsync(fileName, result.Data.ToString());
		return result;
	}
}

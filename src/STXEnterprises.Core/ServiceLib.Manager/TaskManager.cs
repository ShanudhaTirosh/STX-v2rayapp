using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Entities;
using ServiceLib.Services;

namespace ServiceLib.Manager;

public class TaskManager
{
	private static readonly Lazy<TaskManager> _instance = new Lazy<TaskManager>(() => new TaskManager());

	private Config _config;

	private Func<bool, string, Task>? _updateFunc;

	public static TaskManager Instance => _instance.Value;

	public void RegUpdateTask(Config config, Func<bool, string, Task> updateFunc)
	{
		_config = config;
		_updateFunc = updateFunc;
		Task.Run((Func<Task?>)ScheduledTasks);
	}

	private async Task ScheduledTasks()
	{
		Logging.SaveLog("Setup Scheduled Tasks");
		int numOfExecuted = 1;
		while (true)
		{
			await Task.Delay(60000);
			try
			{
				await UpdateTaskRunSubscription();
			}
			catch (Exception ex)
			{
				Logging.SaveLog("ScheduledTasks - UpdateTaskRunSubscription", ex);
			}
			if (numOfExecuted % 20 == 0)
			{
				try
				{
					await ConfigHandler.SaveConfig(_config);
					await ProfileExManager.Instance.SaveTo();
				}
				catch (Exception ex2)
				{
					Logging.SaveLog("ScheduledTasks - SaveConfig", ex2);
				}
			}
			if (numOfExecuted % 60 == 0)
			{
				FileUtils.DeleteExpiredFiles(Utils.GetBinConfigPath(), DateTime.Now.AddHours(-1.0));
				FileUtils.DeleteExpiredFiles(Utils.GetLogPath(), DateTime.Now.AddMonths(-1));
				FileUtils.DeleteExpiredFiles(Utils.GetTempPath(), DateTime.Now.AddMonths(-1));
				try
				{
					await UpdateTaskRunGeo(numOfExecuted / 60);
				}
				catch (Exception ex3)
				{
					Logging.SaveLog("ScheduledTasks - UpdateTaskRunGeo", ex3);
				}
			}
			numOfExecuted = checked(numOfExecuted + 1);
		}
	}

	private async Task UpdateTaskRunSubscription()
	{
		long updateTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
		List<SubItem> list = (await AppManager.Instance.SubItems())?.Where((SubItem t) => t.AutoUpdateInterval > 0).Where((SubItem t) => checked(updateTime - t.UpdateTime >= t.AutoUpdateInterval * 60)).ToList();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		Logging.SaveLog("Execute update subscription");
		foreach (SubItem item in list)
		{
			await SubscriptionHandler.UpdateProcess(_config, item.Id, blProxy: true, async (bool success, string msg) =>
			{
				await (_updateFunc?.Invoke(success, msg));
				if (success)
				{
					Logging.SaveLog("Update subscription end. " + msg);
				}
			});
			item.UpdateTime = updateTime;
			await ConfigHandler.AddSubItem(_config, item);
			await Task.Delay(1000);
		}
	}

	private async Task UpdateTaskRunGeo(int hours)
	{
		if (_config.GuiItem.AutoUpdateInterval > 0 && hours > 0 && hours % _config.GuiItem.AutoUpdateInterval == 0)
		{
			Logging.SaveLog("Execute update geo files");
			await new UpdateService(_config, async (bool success, string msg) =>
			{
				await (_updateFunc?.Invoke(arg1: false, msg));
			}).UpdateGeoFileAll();
		}
	}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using ServiceLib.Common;
using ServiceLib.Models.Configs;

namespace ServiceLib.Handler;

public static class AutoStartupHandler
{
	private static readonly string _tag = "AutoStartupHandler";

	public static async Task<bool> UpdateTask(Config config)
	{
		if (Utils.IsWindows())
		{
			await ClearTaskWindows();
			if (config.GuiItem.AutoRun)
			{
				await SetTaskWindows();
			}
		}
		else if (Utils.IsLinux())
		{
			await ClearTaskLinux();
			if (config.GuiItem.AutoRun)
			{
				await SetTaskLinux();
			}
		}
		else if (Utils.IsMacOS())
		{
			await ClearTaskOSX();
			if (config.GuiItem.AutoRun)
			{
				await SetTaskOSX();
			}
		}
		return true;
	}

	[SupportedOSPlatform("windows")]
	private static async System.Threading.Tasks.Task ClearTaskWindows()
	{
		string autoRunNameWindows = GetAutoRunNameWindows();
		WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Run", autoRunNameWindows, "");
		if (Utils.IsAdministrator())
		{
			AutoStartTaskService(autoRunNameWindows, "", "");
		}
		await System.Threading.Tasks.Task.CompletedTask;
	}

	[SupportedOSPlatform("windows")]
	private static async System.Threading.Tasks.Task SetTaskWindows()
	{
		try
		{
			string autoRunNameWindows = GetAutoRunNameWindows();
			string exePath = Utils.GetExePath();
			if (Utils.IsAdministrator())
			{
				AutoStartTaskService(autoRunNameWindows, exePath, "");
			}
			else
			{
				WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Run", autoRunNameWindows, exePath.AppendQuotes());
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		await System.Threading.Tasks.Task.CompletedTask;
	}

	[SupportedOSPlatform("windows")]
	public static void AutoStartTaskService(string taskName, string fileName, string description)
	{
		if (taskName.IsNullOrEmpty())
		{
			return;
		}
		string name = WindowsIdentity.GetCurrent().Name;
		using TaskService taskService = new TaskService();
		TaskCollection tasks = taskService.RootFolder.GetTasks(new Regex(taskName));
		if (fileName.IsNullOrEmpty())
		{
			foreach (Microsoft.Win32.TaskScheduler.Task item in tasks)
			{
				taskService.RootFolder.DeleteTask(item.Name);
			}
			return;
		}
		TaskDefinition taskDefinition = taskService.NewTask();
		taskDefinition.RegistrationInfo.Description = description;
		taskDefinition.Settings.DisallowStartIfOnBatteries = false;
		taskDefinition.Settings.StopIfGoingOnBatteries = false;
		taskDefinition.Settings.RunOnlyIfIdle = false;
		taskDefinition.Settings.IdleSettings.StopOnIdleEnd = false;
		taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
		taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;
		taskDefinition.Triggers.Add(new LogonTrigger
		{
			UserId = name
		});
		taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
		taskDefinition.Actions.Add(new ExecAction(fileName.AppendQuotes(), null, Path.GetDirectoryName(fileName)));
		taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);
	}

	private static string GetAutoRunNameWindows()
	{
		return "STXEnterprisesAutoRun_" + Utils.GetMd5(Utils.StartupPath());
	}

	[SupportedOSPlatform("linux")]
	private static async System.Threading.Tasks.Task ClearTaskLinux()
	{
		try
		{
			File.Delete(GetHomePathLinux());
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		await System.Threading.Tasks.Task.CompletedTask;
	}

	[SupportedOSPlatform("linux")]
	private static async System.Threading.Tasks.Task SetTaskLinux()
	{
		try
		{
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.linux_autostart_config");
			if (embedText.IsNotEmpty())
			{
				embedText = embedText.Replace("$ExecPath$", Utils.GetExePath());
				Logging.SaveLog(embedText);
				await File.WriteAllTextAsync(GetHomePathLinux(), embedText);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	[SupportedOSPlatform("linux")]
	private static string GetHomePathLinux()
	{
		string text = Path.Combine(Utils.GetHomePath(), ".config", "autostart", "STX Enterprises.desktop");
		Directory.CreateDirectory(Path.GetDirectoryName(text));
		return text;
	}

	[SupportedOSPlatform("macos")]
	private static async System.Threading.Tasks.Task ClearTaskOSX()
	{
		try
		{
			string launchAgentPath = GetLaunchAgentPathMacOS();
			if (File.Exists(launchAgentPath))
			{
				string[] args = new string[2]
				{
					"-c",
					"launchctl unload -w \"" + launchAgentPath + "\""
				};
				await Utils.GetCliWrapOutput("/bin/bash", args);
				File.Delete(launchAgentPath);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	[SupportedOSPlatform("macos")]
	private static async System.Threading.Tasks.Task SetTaskOSX()
	{
		_ = 1;
		try
		{
			string contents = GenerateLaunchAgentPlist();
			string launchAgentPath = GetLaunchAgentPathMacOS();
			await File.WriteAllTextAsync(launchAgentPath, contents);
			string[] args = new string[2]
			{
				"-c",
				"launchctl load -w \"" + launchAgentPath + "\""
			};
			await Utils.GetCliWrapOutput("/bin/bash", args);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	[SupportedOSPlatform("macos")]
	private static string GetLaunchAgentPathMacOS()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", "STX Enterprises-LaunchAgent.plist");
		Directory.CreateDirectory(Path.GetDirectoryName(text));
		return text;
	}

	[SupportedOSPlatform("macos")]
	private static string GenerateLaunchAgentPlist()
	{
		string exePath = Utils.GetExePath();
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(exePath);
		return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n    <key>Label</key>\n    <string>{"STX Enterprises"}-LaunchAgent</string>\n    <key>ProgramArguments</key>\n    <array>\n        <string>/bin/sh</string>\n        <string>-c</string>\n        <string>if ! pgrep -x \"{fileNameWithoutExtension}\" > /dev/null; then \"{exePath}\"; fi</string>\n    </array>\n    <key>RunAtLoad</key>\n    <true/>\n    <key>KeepAlive</key>\n    <false/>\n</dict>\n</plist>";
	}
}

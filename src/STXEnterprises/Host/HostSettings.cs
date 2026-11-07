using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;

namespace STXEnterprises.Host;

public sealed class HostSettings
{
	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public bool AutoRun { get; set; }

	public bool AutoConnect { get; set; }

	public bool MinimiseToTray { get; set; } = true;

	public bool StartMinimised { get; set; }

	public string Language { get; set; } = "en";

	public string Theme { get; set; } = "dark";

	public string? LastConfigId { get; set; }

	public string SplitMode { get; set; } = "all";

	public List<string> SplitApps { get; set; } = new List<string>();

	public bool BlockAds { get; set; }

	public bool TcpFastOpen { get; set; }

	/// <summary>Learn each server's certificate on first connect and refuse impostors afterwards (replaces allowInsecure).</summary>
	public bool PinServerCertificates { get; set; } = true;

	private static readonly object SaveLock = new object();

	private static string Path0 => Path.Combine(Utils.GetConfigPath(), "stx-settings.json");
	private static string LegacyPath0 => Path.Combine(Utils.GetConfigPath(), "pxn-settings.json");

	public static HostSettings Load()
	{
		try
		{
			string filePath = File.Exists(Path0) ? Path0 : (File.Exists(LegacyPath0) ? LegacyPath0 : null);
			if (filePath != null && File.Exists(filePath))
			{
				return JsonSerializer.Deserialize<HostSettings>(File.ReadAllText(filePath), Json) ?? new HostSettings();
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("settings load", ex);
		}
		return new HostSettings();
	}

	public void Save()
	{
		try
		{
			lock (SaveLock)
			{
				AtomicFile.WriteAllText(Path0, JsonSerializer.Serialize(this, Json));
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("settings save", ex);
		}
	}

	public async Task ApplyAutoRunAsync()
	{
		Config config = AppManager.Instance.Config;
		config.GuiItem.AutoRun = AutoRun;
		await ConfigHandler.SaveConfig(config);
		await AutoStartupHandler.UpdateTask(config);
	}
}

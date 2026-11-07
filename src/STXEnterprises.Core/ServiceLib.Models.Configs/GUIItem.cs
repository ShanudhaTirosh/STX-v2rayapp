using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class GUIItem
{
	public bool AutoRun { get; set; }

	public bool EnableStatistics { get; set; }

	public bool DisplayRealTimeSpeed { get; set; }

	public bool KeepOlderDedupl { get; set; }

	public int AutoUpdateInterval { get; set; }

	public int TrayMenuServersLimit { get; set; } = 20;

	public bool EnableHWA { get; set; }

	public bool EnableLog { get; set; } = true;
}

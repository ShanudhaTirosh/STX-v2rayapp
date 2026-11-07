using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class BalancersStrategySettings4Ray
{
	public int? expected { get; set; }

	public string? maxRTT { get; set; }

	public float? tolerance { get; set; }

	public List<string>? baselines { get; set; }

	public List<BalancersStrategySettingsCosts4Ray>? costs { get; set; }
}

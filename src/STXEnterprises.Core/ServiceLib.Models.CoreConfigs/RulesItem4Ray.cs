using System;
using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

[Serializable]
public class RulesItem4Ray
{
	public string? type { get; set; }

	public string? port { get; set; }

	public string? network { get; set; }

	public List<string>? inboundTag { get; set; }

	public string? outboundTag { get; set; }

	public string? balancerTag { get; set; }

	public List<string>? ip { get; set; }

	public List<string>? domain { get; set; }

	public List<string>? protocol { get; set; }

	public List<string>? process { get; set; }
}

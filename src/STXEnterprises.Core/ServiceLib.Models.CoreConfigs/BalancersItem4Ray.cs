using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class BalancersItem4Ray
{
	public List<string>? selector { get; set; }

	public BalancersStrategy4Ray? strategy { get; set; }

	public string? tag { get; set; }

	public string? fallbackTag { get; set; }
}

using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Sniffing4Ray
{
	public bool enabled { get; set; }

	public List<string>? destOverride { get; set; }

	public bool routeOnly { get; set; }
}

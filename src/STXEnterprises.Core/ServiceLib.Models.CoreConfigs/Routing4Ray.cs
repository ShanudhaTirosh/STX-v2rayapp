using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Routing4Ray
{
	public string domainStrategy { get; set; }

	public List<RulesItem4Ray> rules { get; set; }

	public List<BalancersItem4Ray>? balancers { get; set; }
}

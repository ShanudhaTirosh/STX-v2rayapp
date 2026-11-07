using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Route4Sbox
{
	public Rule4Sbox? default_domain_resolver { get; set; }

	public bool? auto_detect_interface { get; set; }

	public string? default_interface { get; set; }

	public List<Rule4Sbox> rules { get; set; }

	public List<Ruleset4Sbox>? rule_set { get; set; }

	public string? final { get; set; }
}

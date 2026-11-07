using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class DnsServer4Ray
{
	public string? address { get; set; }

	public int? port { get; set; }

	public List<string>? domains { get; set; }

	public bool? skipFallback { get; set; }

	public List<string>? expectedIPs { get; set; }

	public string? tag { get; set; }
}

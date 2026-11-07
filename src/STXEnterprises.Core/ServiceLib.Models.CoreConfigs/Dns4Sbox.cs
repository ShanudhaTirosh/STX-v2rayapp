using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Dns4Sbox
{
	public List<Server4Sbox> servers { get; set; }

	public List<Rule4Sbox> rules { get; set; }

	public string? final { get; set; }

	public string? strategy { get; set; }

	public bool? disable_cache { get; set; }

	public bool? disable_expire { get; set; }

	public bool? independent_cache { get; set; }

	public int? cache_capacity { get; set; }

	public bool? reverse_mapping { get; set; }

	public string? client_subnet { get; set; }
}

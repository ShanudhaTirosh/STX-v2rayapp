using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Stats4Sbox
{
	public bool enabled { get; set; }

	public List<string>? inbounds { get; set; }

	public List<string>? outbounds { get; set; }

	public List<string>? users { get; set; }
}

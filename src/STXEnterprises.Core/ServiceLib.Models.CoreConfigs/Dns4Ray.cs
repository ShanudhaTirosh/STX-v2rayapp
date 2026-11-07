using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Dns4Ray
{
	public Dictionary<string, object>? hosts { get; set; }

	public List<object> servers { get; set; }

	public bool? serveStale { get; set; }

	public bool? enableParallelQuery { get; set; }

	public string? tag { get; set; }
}

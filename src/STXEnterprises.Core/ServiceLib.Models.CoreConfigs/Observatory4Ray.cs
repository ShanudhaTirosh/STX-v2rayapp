using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Observatory4Ray
{
	public List<string>? subjectSelector { get; set; }

	public string? probeUrl { get; set; }

	public string? probeInterval { get; set; }

	public bool? enableConcurrency { get; set; }
}

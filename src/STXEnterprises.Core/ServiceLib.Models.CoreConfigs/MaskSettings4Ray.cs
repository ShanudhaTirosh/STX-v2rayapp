using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class MaskSettings4Ray
{
	public string? password { get; set; }

	public string? domain { get; set; }

	public string? packets { get; set; }

	public string? length { get; set; }

	public string? delay { get; set; }

	public int? reset { get; set; }

	public List<NoiseMask4Ray>? noise { get; set; }
}

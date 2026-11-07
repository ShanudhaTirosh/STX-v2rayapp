namespace ServiceLib.Models.CoreConfigs;

public class Inbounds4Ray
{
	public string tag { get; set; }

	public int? port { get; set; }

	public string? listen { get; set; }

	public string protocol { get; set; }

	public Sniffing4Ray sniffing { get; set; }

	public Inboundsettings4Ray settings { get; set; }
}

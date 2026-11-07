namespace ServiceLib.Models.CoreConfigs;

public class Outbounds4Ray
{
	public string tag { get; set; }

	public string protocol { get; set; }

	public string? sendThrough { get; set; }

	public string? targetStrategy { get; set; }

	public Outboundsettings4Ray settings { get; set; }

	public StreamSettings4Ray streamSettings { get; set; }

	public Mux4Ray mux { get; set; }
}

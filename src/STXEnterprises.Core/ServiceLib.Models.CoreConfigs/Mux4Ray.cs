namespace ServiceLib.Models.CoreConfigs;

public class Mux4Ray
{
	public bool enabled { get; set; }

	public int? concurrency { get; set; }

	public int? xudpConcurrency { get; set; }

	public string? xudpProxyUDP443 { get; set; }
}

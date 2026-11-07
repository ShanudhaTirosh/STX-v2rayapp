using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class Mux4RayItem
{
	public int? Concurrency { get; set; }

	public int? XudpConcurrency { get; set; }

	public string? XudpProxyUDP443 { get; set; }
}

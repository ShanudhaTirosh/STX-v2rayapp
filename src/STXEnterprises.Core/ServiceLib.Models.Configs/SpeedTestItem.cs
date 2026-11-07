using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class SpeedTestItem
{
	public int SpeedTestTimeout { get; set; }

	public string SpeedTestUrl { get; set; }

	public string SpeedPingTestUrl { get; set; }

	public int MixedConcurrencyCount { get; set; }

	public string IPAPIUrl { get; set; }

	public string UdpTestTarget { get; set; }

	public int? SpeedTestPageSize { get; set; }

	public int? SpeedTestDelayInterval { get; set; }
}

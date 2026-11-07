using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Outboundsettings4Ray
{
	public List<VnextItem4Ray>? vnext { get; set; }

	public List<ServersItem4Ray>? servers { get; set; }

	public Response4Ray? response { get; set; }

	public string domainStrategy { get; set; }

	public int? userLevel { get; set; }

	public string? secretKey { get; set; }

	public object? address { get; set; }

	public int? port { get; set; }

	public List<WireguardPeer4Ray>? peers { get; set; }

	public bool? noKernelTun { get; set; }

	public int? mtu { get; set; }

	public List<int>? reserved { get; set; }

	public int? workers { get; set; }

	public int? version { get; set; }
}

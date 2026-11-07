using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public abstract class DialFields4Sbox
{
	public string? detour { get; set; }

	public string? bind_interface { get; set; }

	public string? inet4_bind_address { get; set; }

	public string? inet6_bind_address { get; set; }

	public int? routing_mark { get; set; }

	public bool? reuse_addr { get; set; }

	public string? netns { get; set; }

	public string? connect_timeout { get; set; }

	public bool? tcp_fast_open { get; set; }

	public bool? tcp_multi_path { get; set; }

	public bool? udp_fragment { get; set; }

	public Rule4Sbox? domain_resolver { get; set; }

	public string? network_strategy { get; set; }

	public List<string>? network_type { get; set; }

	public List<string>? fallback_network_type { get; set; }

	public string? fallback_delay { get; set; }

	public Tls4Sbox? tls { get; set; }

	public Multiplex4Sbox? multiplex { get; set; }

	public Transport4Sbox? transport { get; set; }

	public HyObfs4Sbox? obfs { get; set; }
}

using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Endpoints4Sbox : BaseServer4Sbox
{
	public bool? system { get; set; }

	public string? name { get; set; }

	public int? mtu { get; set; }

	public List<string> address { get; set; }

	public string private_key { get; set; }

	public int? listen_port { get; set; }

	public string? udp_timeout { get; set; }

	public int? workers { get; set; }

	public List<Peer4Sbox> peers { get; set; }
}

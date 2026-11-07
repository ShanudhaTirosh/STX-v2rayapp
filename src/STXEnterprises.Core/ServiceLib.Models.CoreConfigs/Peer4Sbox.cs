using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Peer4Sbox
{
	public string address { get; set; }

	public int port { get; set; }

	public string public_key { get; set; }

	public string? pre_shared_key { get; set; }

	public List<string> allowed_ips { get; set; }

	public int? persistent_keepalive_interval { get; set; }

	public List<int>? reserved { get; set; }
}

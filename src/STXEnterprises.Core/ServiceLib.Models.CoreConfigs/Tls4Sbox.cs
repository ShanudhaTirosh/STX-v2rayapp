using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Tls4Sbox
{
	public bool enabled { get; set; }

	public string? server_name { get; set; }

	public bool? insecure { get; set; }

	public List<string>? alpn { get; set; }

	public Utls4Sbox? utls { get; set; }

	public Reality4Sbox? reality { get; set; }

	public bool? fragment { get; set; }

	public string? fragment_fallback_delay { get; set; }

	public bool? record_fragment { get; set; }

	public List<string>? certificate { get; set; }

	public Ech4Sbox? ech { get; set; }
}

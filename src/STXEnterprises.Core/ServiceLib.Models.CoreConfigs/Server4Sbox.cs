using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ServiceLib.Models.CoreConfigs;

public class Server4Sbox : BaseServer4Sbox
{
	public string? inet4_range { get; set; }

	public string? inet6_range { get; set; }

	public string? client_subnet { get; set; }

	public string? server { get; set; }

	public new string? domain_resolver { get; set; }

	[JsonPropertyName("interface")]
	public string? Interface { get; set; }

	public int? server_port { get; set; }

	public string? path { get; set; }

	public Headers4Sbox? headers { get; set; }

	public Dictionary<string, List<string>>? predefined { get; set; }
}

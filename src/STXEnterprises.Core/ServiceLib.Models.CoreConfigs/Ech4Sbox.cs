using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Ech4Sbox
{
	public bool enabled { get; set; }

	public List<string>? config { get; set; }

	public string? query_server_name { get; set; }
}

using System;
using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

[Serializable]
public class Inbound4Sbox
{
	public string type { get; set; }

	public string tag { get; set; }

	public string listen { get; set; }

	public int? listen_port { get; set; }

	public string interface_name { get; set; }

	public List<string>? address { get; set; }

	public int? mtu { get; set; }

	public bool? auto_route { get; set; }

	public bool? strict_route { get; set; }

	public List<string>? route_exclude_address { get; set; }

	public bool? endpoint_independent_nat { get; set; }

	public string? stack { get; set; }

	public List<User4Sbox> users { get; set; }
}

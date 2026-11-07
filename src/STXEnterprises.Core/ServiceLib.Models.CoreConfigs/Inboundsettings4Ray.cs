using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Inboundsettings4Ray
{
	public string? auth { get; set; }

	public bool? udp { get; set; }

	public string? ip { get; set; }

	public string? address { get; set; }

	public List<UsersItem4Ray>? clients { get; set; }

	public string? decryption { get; set; }

	public bool? allowTransparent { get; set; }

	public List<AccountsItem4Ray>? accounts { get; set; }

	public string? name { get; set; }

	public int? MTU { get; set; }

	public List<string>? gateway { get; set; }

	public List<string>? autoSystemRoutingTable { get; set; }

	public string? autoOutboundsInterface { get; set; }
}

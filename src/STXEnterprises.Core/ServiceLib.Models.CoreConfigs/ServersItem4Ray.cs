using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class ServersItem4Ray
{
	public string email { get; set; }

	public string address { get; set; }

	public string? method { get; set; }

	public bool? ota { get; set; }

	public string? password { get; set; }

	public int port { get; set; }

	public int? level { get; set; }

	public string flow { get; set; }

	public bool? uot { get; set; }

	public List<SocksUsersItem4Ray> users { get; set; }
}

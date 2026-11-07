using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class VnextItem4Ray
{
	public string address { get; set; }

	public int port { get; set; }

	public List<UsersItem4Ray> users { get; set; }
}

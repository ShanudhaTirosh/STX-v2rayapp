using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ServiceLib.Models.Configs;

[Serializable]
public class InItem
{
	public int LocalPort { get; set; }

	public string Protocol { get; set; }

	public bool UdpEnabled { get; set; }

	public bool SniffingEnabled { get; set; } = true;

	public List<string>? DestOverride { get; set; }

	public bool RouteOnly { get; set; }

	public bool AllowLANConn { get; set; }

	public bool NewPort4LAN { get; set; }

	public string User { get; set; }

	public string Pass { get; set; }

	public bool SecondLocalPortEnabled { get; set; }

	public InItem()
	{
		DestOverride = new List<string> { "http", "tls" };
	}
}

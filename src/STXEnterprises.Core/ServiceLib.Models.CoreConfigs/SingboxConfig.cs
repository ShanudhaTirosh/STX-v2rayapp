using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class SingboxConfig
{
	public Log4Sbox log { get; set; }

	public Dns4Sbox? dns { get; set; }

	public List<Inbound4Sbox> inbounds { get; set; }

	public List<Outbound4Sbox> outbounds { get; set; }

	public List<Endpoints4Sbox>? endpoints { get; set; }

	public Route4Sbox route { get; set; }

	public Experimental4Sbox? experimental { get; set; }
}

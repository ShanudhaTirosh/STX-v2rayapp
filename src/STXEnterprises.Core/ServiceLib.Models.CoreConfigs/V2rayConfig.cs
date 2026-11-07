using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class V2rayConfig
{
	public Log4Ray log { get; set; }

	public object dns { get; set; }

	public List<Inbounds4Ray> inbounds { get; set; }

	public List<Outbounds4Ray> outbounds { get; set; }

	public Routing4Ray routing { get; set; }

	public Metrics4Ray? metrics { get; set; }

	public Policy4Ray? policy { get; set; }

	public Stats4Ray? stats { get; set; }

	public Observatory4Ray? observatory { get; set; }

	public BurstObservatory4Ray? burstObservatory { get; set; }

	public string? remarks { get; set; }
}

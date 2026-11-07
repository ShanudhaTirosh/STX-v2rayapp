using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class BurstObservatory4Ray
{
	public List<string>? subjectSelector { get; set; }

	public BurstObservatoryPingConfig4Ray? pingConfig { get; set; }
}

using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class RoutingBasicItem
{
	public string DomainStrategy { get; set; }

	public string DomainStrategy4Singbox { get; set; }

	public string RoutingIndexId { get; set; }
}

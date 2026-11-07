using System;
using System.Collections.Generic;
using ServiceLib.Enums;

namespace ServiceLib.Models.Configs;

[Serializable]
public class ClashUIItem
{
	public ERuleMode RuleMode { get; set; }

	public bool EnableIPv6 { get; set; }

	public bool EnableMixinContent { get; set; }

	public int ProxiesSorting { get; set; }

	public bool ProxiesAutoRefresh { get; set; }

	public int ProxiesAutoDelayTestInterval { get; set; } = 10;

	public bool ConnectionsAutoRefresh { get; set; }

	public int ConnectionsRefreshInterval { get; set; } = 2;

	public List<ColumnItem> ConnectionsColumnItem { get; set; }
}

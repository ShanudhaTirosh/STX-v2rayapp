using System;
using System.Collections.Generic;

namespace ServiceLib.Models.Configs;

[Serializable]
public class CheckUpdateItem
{
	public bool CheckPreReleaseUpdate { get; set; }

	public List<string>? SelectedCoreTypes { get; set; }
}

using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class MsgUIItem
{
	public string? MainMsgFilter { get; set; }

	public bool? AutoRefresh { get; set; }
}

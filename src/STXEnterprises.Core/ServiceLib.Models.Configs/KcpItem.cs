using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class KcpItem
{
	public int Mtu { get; set; }

	public int Tti { get; set; }

	public int UplinkCapacity { get; set; }

	public int DownlinkCapacity { get; set; }

	public int CwndMultiplier { get; set; }

	public int MaxSendingWindow { get; set; }
}

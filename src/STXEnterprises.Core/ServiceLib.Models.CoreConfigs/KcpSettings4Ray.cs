namespace ServiceLib.Models.CoreConfigs;

public class KcpSettings4Ray
{
	public int mtu { get; set; }

	public int tti { get; set; }

	public int uplinkCapacity { get; set; }

	public int downlinkCapacity { get; set; }

	public int cwndMultiplier { get; set; }

	public int maxSendingWindow { get; set; }
}

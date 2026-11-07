namespace ServiceLib.Models.CoreConfigs;

public class QuicParams4Ray
{
	public string? congestion { get; set; }

	public string? brutalUp { get; set; }

	public string? brutalDown { get; set; }

	public UdpHop4Ray? udpHop { get; set; }
}

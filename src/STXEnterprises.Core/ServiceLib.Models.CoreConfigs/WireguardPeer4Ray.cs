namespace ServiceLib.Models.CoreConfigs;

public class WireguardPeer4Ray
{
	public string endpoint { get; set; }

	public string publicKey { get; set; }

	public string? preSharedKey { get; set; }
}

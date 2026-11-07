namespace ServiceLib.Manager;

public sealed class StxConfigTweak
{
	public StxConfigFields? Issued { get; set; }

	public StxConfigFields Changed { get; set; } = new StxConfigFields();
}

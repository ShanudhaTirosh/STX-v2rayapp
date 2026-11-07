namespace ServiceLib.Manager;

public static class StxAdBlock
{
	public const string Category = "geosite:category-ads-all";

	public static bool Enabled { get; private set; }

	public static void Set(bool enabled)
	{
		Enabled = enabled;
	}
}

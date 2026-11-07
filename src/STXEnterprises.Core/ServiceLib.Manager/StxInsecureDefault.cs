namespace ServiceLib.Manager;

public static class StxInsecureDefault
{
	public static bool Enabled { get; private set; } = true;

	public static void Set(bool enabled)
	{
		Enabled = enabled;
	}
}

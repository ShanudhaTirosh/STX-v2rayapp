namespace ServiceLib.Manager;

public static class StxTcpFastOpen
{
	public static bool Enabled { get; private set; }

	public static void Set(bool enabled)
	{
		Enabled = enabled;
	}
}

using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class TunModeItem
{
	public bool EnableTun { get; set; }

	public bool AutoRoute { get; set; } = true;

	public bool StrictRoute { get; set; } = true;

	public string Stack { get; set; }

	public int Mtu { get; set; }

	public bool EnableIPv6Address { get; set; }

	public string IcmpRouting { get; set; }

	public bool EnableLegacyProtect { get; set; }
}

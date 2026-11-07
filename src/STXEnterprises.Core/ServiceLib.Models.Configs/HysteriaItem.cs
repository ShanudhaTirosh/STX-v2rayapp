using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class HysteriaItem
{
	public int UpMbps { get; set; }

	public int DownMbps { get; set; }

	public int HopInterval { get; set; } = 30;
}

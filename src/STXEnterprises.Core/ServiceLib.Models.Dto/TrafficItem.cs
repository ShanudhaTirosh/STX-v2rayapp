using System;

namespace ServiceLib.Models.Dto;

[Serializable]
public class TrafficItem
{
	public ulong Up { get; set; }

	public ulong Down { get; set; }
}

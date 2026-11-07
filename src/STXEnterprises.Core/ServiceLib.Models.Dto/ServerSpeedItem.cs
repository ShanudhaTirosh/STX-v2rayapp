using System;
using ServiceLib.Models.Entities;

namespace ServiceLib.Models.Dto;

[Serializable]
public class ServerSpeedItem : ServerStatItem
{
	public long ProxyUp { get; set; }

	public long ProxyDown { get; set; }

	public long DirectUp { get; set; }

	public long DirectDown { get; set; }
}

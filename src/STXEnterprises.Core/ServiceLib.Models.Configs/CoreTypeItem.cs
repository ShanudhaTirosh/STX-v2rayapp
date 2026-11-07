using System;
using ServiceLib.Enums;

namespace ServiceLib.Models.Configs;

[Serializable]
public class CoreTypeItem
{
	public EConfigType ConfigType { get; set; }

	public ECoreType CoreType { get; set; }
}

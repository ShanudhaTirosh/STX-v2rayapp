using System;
using ServiceLib.Enums;

namespace ServiceLib.Models.Configs;

[Serializable]
public class SystemProxyItem
{
	public ESysProxyType SysProxyType { get; set; }

	public string SystemProxyExceptions { get; set; }

	public bool NotProxyLocalAddress { get; set; } = true;

	public string SystemProxyAdvancedProtocol { get; set; }

	public string? CustomSystemProxyPacPath { get; set; }

	public string? CustomSystemProxyScriptPath { get; set; }
}

using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class SimpleDNSItem
{
	public bool? UseSystemHosts { get; set; }

	public bool? AddCommonHosts { get; set; }

	public bool? FakeIP { get; set; }

	public bool? GlobalFakeIp { get; set; }

	public bool? BlockBindingQuery { get; set; }

	public string? DirectDNS { get; set; }

	public string? RemoteDNS { get; set; }

	public string? BootstrapDNS { get; set; }

	public string? Strategy4Freedom { get; set; }

	public string? Strategy4Proxy { get; set; }

	public bool? ServeStale { get; set; }

	public bool? ParallelQuery { get; set; }

	public string? Hosts { get; set; }

	public string? DirectExpectedIPs { get; set; }
}

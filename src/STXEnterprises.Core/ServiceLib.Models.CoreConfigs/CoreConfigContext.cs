using System.Collections.Generic;
using ServiceLib.Enums;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Entities;

namespace ServiceLib.Models.CoreConfigs;

public record CoreConfigContext
{
	public required ProfileItem Node { get; set; }

	public required ECoreType RunCoreType { get; set; }

	public RoutingItem? RoutingItem { get; set; }

	public DNSItem? RawDnsItem { get; set; }

	public SimpleDNSItem SimpleDnsItem { get; set; } = new SimpleDNSItem();

	public Dictionary<string, ProfileItem> AllProxiesMap { get; set; } = new Dictionary<string, ProfileItem>();

	public Config AppConfig { get; set; } = new Config();

	public FullConfigTemplateItem? FullConfigTemplate { get; set; } = new FullConfigTemplateItem();

	public Dictionary<string, string> ServerTestItemMap { get; set; } = new Dictionary<string, string>();

	public bool IsTunEnabled { get; set; }

	public HashSet<string> ProtectDomainList { get; set; } = new HashSet<string>();

	public bool IsWindows { get; set; }

	public bool IsMacOS { get; set; }
}

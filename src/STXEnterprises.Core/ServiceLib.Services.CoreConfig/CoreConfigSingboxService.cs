using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Services.CoreConfig;

public class CoreConfigSingboxService
{
	private static readonly string _tag = "CoreConfigSingboxService";

	private readonly Config _config;

	private readonly ProfileItem _node;

	private SingboxConfig _coreConfig;
	private readonly CoreConfigContext _context;

	public CoreConfigSingboxService(CoreConfigContext context)
	{
		_context = context;
		_config = _context.AppConfig;
		_node = _context.Node;
		_coreConfig = new SingboxConfig();
		
	}

	public RetResult GenerateClientConfigContent()
	{
		RetResult retResult = new RetResult();
		try
		{
			if (_node == null || !_node.IsValid())
			{
				retResult.Msg = ResUI.CheckServerSettings;
				return retResult;
			}
			string network = _node.GetNetwork();
			if ((network == "kcp" || network == "xhttp") ? true : false)
			{
				retResult.Msg = ResUI.Incorrectconfiguration + " - " + _node.GetNetwork();
				return retResult;
			}
			retResult.Msg = ResUI.InitialConfiguration;
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SingboxSampleClientConfig");
			if (embedText.IsNullOrEmpty())
			{
				retResult.Msg = ResUI.FailedGetDefaultConfiguration;
				return retResult;
			}
			_coreConfig = JsonUtils.Deserialize<SingboxConfig>(embedText);
			if (_coreConfig == null)
			{
				retResult.Msg = ResUI.FailedGenDefaultConfiguration;
				return retResult;
			}
			GenLog();
			GenInbounds();
			GenOutbounds();
			GenRouting();
			GenDns();
			GenExperimental();
			ConvertGeo2Ruleset();
			StripRemoteRulesets();
			ApplyOutboundBindInterface();
			ApplyOutboundSendThrough();
			retResult.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
			retResult.Success = true;
			retResult.Data = ApplyFullConfigTemplate();
			return retResult;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			retResult.Msg = ResUI.FailedGenDefaultConfiguration;
			return retResult;
		}
	}

	public RetResult GenerateClientSpeedtestConfig(List<ServerTestItem> selecteds)
	{
		RetResult retResult = new RetResult();
		checked
		{
			try
			{
				retResult.Msg = ResUI.InitialConfiguration;
				string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SingboxSampleClientConfig");
				string embedText2 = EmbedUtils.GetEmbedText("ServiceLib.Sample.SingboxSampleOutbound");
				if (embedText.IsNullOrEmpty() || embedText2.IsNullOrEmpty())
				{
					retResult.Msg = ResUI.FailedGetDefaultConfiguration;
					return retResult;
				}
				_coreConfig = JsonUtils.Deserialize<SingboxConfig>(embedText);
				if (_coreConfig == null)
				{
					retResult.Msg = ResUI.FailedGenDefaultConfiguration;
					return retResult;
				}
				var (list, list2) = Utils.GetActiveNetworkInfo();
				GenLog();
				GenMinimizedDns();
				_coreConfig.inbounds.Clear();
				_coreConfig.outbounds.RemoveAt(0);
				int num = AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest);
				foreach (ServerTestItem selected in selecteds)
				{
					if ((!Global.SingboxSupportConfigType.Contains(selected.ConfigType) && !selected.ConfigType.IsGroupType()) || (!selected.ConfigType.IsComplexType() && selected.Port <= 0))
					{
						continue;
					}
					string valueOrDefault = _context.ServerTestItemMap.GetValueOrDefault(selected.IndexId, selected.IndexId);
					ProfileItem valueOrDefault2 = _context.AllProxiesMap.GetValueOrDefault(valueOrDefault);
					if (valueOrDefault2 == null || valueOrDefault2.ConfigType == EConfigType.Custom || !valueOrDefault2.IsValid())
					{
						continue;
					}
					int port = num;
					int k;
					for (k = num; k < 65536; k++)
					{
						if ((list == null || list.FindIndex((IPEndPoint _it) => _it.Port == k) < 0) && (list2 == null || list2.FindIndex((TcpConnectionInformation _it) => _it.LocalEndPoint.Port == k) < 0))
						{
							port = k;
							num = port + 1;
							break;
						}
					}
					if (list == null || list.FindIndex((IPEndPoint _it) => _it.Port == port) < 0)
					{
						selected.Port = port;
						selected.AllowTest = true;
						Inbound4Sbox inbound4Sbox = new Inbound4Sbox
						{
							listen = "127.0.0.1",
							listen_port = port,
							type = EInboundProtocol.mixed.ToString()
						};
						inbound4Sbox.tag = inbound4Sbox.type + inbound4Sbox.listen_port;
						_coreConfig.inbounds.Add(inbound4Sbox);
						string text = "proxy" + inbound4Sbox.listen_port;
						FillRangeProxy(new CoreConfigSingboxService(_context with
						{
							Node = valueOrDefault2
						}).BuildAllProxyOutbounds(text), _coreConfig, prepend: false);
						Rule4Sbox item = new Rule4Sbox
						{
							inbound = new List<string> { inbound4Sbox.tag },
							outbound = text
						};
						_coreConfig.route.rules.Add(item);
					}
				}
				ApplyOutboundBindInterface();
				ApplyOutboundSendThrough();
				retResult.Success = true;
				retResult.Data = JsonUtils.Serialize(_coreConfig);
				return retResult;
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
				retResult.Msg = ResUI.FailedGenDefaultConfiguration;
				return retResult;
			}
		}
	}

	public RetResult GenerateClientSpeedtestConfig(int port)
	{
		RetResult retResult = new RetResult();
		try
		{
			if (_node == null || !_node.IsValid())
			{
				retResult.Msg = ResUI.CheckServerSettings;
				return retResult;
			}
			string network = _node.GetNetwork();
			if ((network == "kcp" || network == "xhttp") ? true : false)
			{
				retResult.Msg = ResUI.Incorrectconfiguration + " - " + _node.GetNetwork();
				return retResult;
			}
			retResult.Msg = ResUI.InitialConfiguration;
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SingboxSampleClientConfig");
			if (embedText.IsNullOrEmpty())
			{
				retResult.Msg = ResUI.FailedGetDefaultConfiguration;
				return retResult;
			}
			_coreConfig = JsonUtils.Deserialize<SingboxConfig>(embedText);
			if (_coreConfig == null)
			{
				retResult.Msg = ResUI.FailedGenDefaultConfiguration;
				return retResult;
			}
			GenLog();
			GenOutbounds();
			GenMinimizedDns();
			_coreConfig.route.rules.Clear();
			_coreConfig.inbounds.Clear();
			_coreConfig.inbounds.Add(new Inbound4Sbox
			{
				tag = $"{EInboundProtocol.mixed}{port}",
				listen = "127.0.0.1",
				listen_port = port,
				type = EInboundProtocol.mixed.ToString()
			});
			ApplyOutboundBindInterface();
			ApplyOutboundSendThrough();
			retResult.Msg = string.Format(ResUI.SuccessfulConfiguration, "");
			retResult.Success = true;
			retResult.Data = JsonUtils.Serialize(_coreConfig);
			return retResult;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			retResult.Msg = ResUI.FailedGenDefaultConfiguration;
			return retResult;
		}
	}

	private string ApplyFullConfigTemplate()
	{
		FullConfigTemplateItem fullConfigTemplate = _context.FullConfigTemplate;
		if (fullConfigTemplate == null || !fullConfigTemplate.Enabled)
		{
			return JsonUtils.Serialize(_coreConfig);
		}
		string text = (_context.IsTunEnabled ? fullConfigTemplate.TunConfig : fullConfigTemplate.Config);
		if (text.IsNullOrEmpty())
		{
			return JsonUtils.Serialize(_coreConfig);
		}
		JsonNode jsonNode = JsonNode.Parse(text);
		if (jsonNode == null)
		{
			return JsonUtils.Serialize(_coreConfig);
		}
		JsonArray jsonArray = ((jsonNode["outbounds"] is JsonArray jsonArray2) ? jsonArray2 : new JsonArray());
		foreach (Outbound4Sbox outbound in _coreConfig.outbounds)
		{
			string text2 = outbound.type.ToLower();
			if ((text2 == "direct" || text2 == "block") ? true : false)
			{
				if (fullConfigTemplate.AddProxyOnly == true)
				{
					continue;
				}
			}
			else if (outbound.detour.IsNullOrEmpty() && !fullConfigTemplate.ProxyDetour.IsNullOrEmpty() && !Utils.IsPrivateNetwork(outbound.server ?? string.Empty))
			{
				outbound.detour = fullConfigTemplate.ProxyDetour;
			}
			jsonArray.Add(JsonUtils.DeepCopy(outbound));
		}
		jsonNode["outbounds"] = jsonArray;
		if (_coreConfig.endpoints != null && _coreConfig.endpoints.Count > 0)
		{
			JsonArray jsonArray3 = ((jsonNode["endpoints"] is JsonArray jsonArray4) ? jsonArray4 : new JsonArray());
			foreach (Endpoints4Sbox endpoint in _coreConfig.endpoints)
			{
				if (endpoint.detour.IsNullOrEmpty() && !fullConfigTemplate.ProxyDetour.IsNullOrEmpty())
				{
					endpoint.detour = fullConfigTemplate.ProxyDetour;
				}
				jsonArray3.Add(JsonUtils.DeepCopy(endpoint));
			}
			jsonNode["endpoints"] = jsonArray3;
		}
		return JsonUtils.Serialize(jsonNode);
	}

	private void ApplyOutboundBindInterface()
	{
		string text = _config.CoreBasicItem.BindInterface?.TrimEx();
		if (text.IsNullOrEmpty() || (!_context.IsTunEnabled && !_context.IsWindows))
		{
			return;
		}
		foreach (Outbound4Sbox item in _coreConfig.outbounds ?? new List<Outbound4Sbox>())
		{
			item.bind_interface = (ShouldBindNet(item) ? text : null);
		}
	}

	private void ApplyOutboundSendThrough()
	{
		string text = _config.CoreBasicItem.SendThrough?.TrimEx();
		if (text.IsNullOrEmpty())
		{
			return;
		}
		foreach (Outbound4Sbox item in _coreConfig.outbounds ?? new List<Outbound4Sbox>())
		{
			item.inet4_bind_address = (ShouldBindNet(item) ? text : null);
		}
	}

	private static bool ShouldBindNet(Outbound4Sbox outbound)
	{
		bool flag;
		switch (outbound.type)
		{
		case "direct":
		case "block":
		case "dns":
		case "selector":
		case "urltest":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return false;
		}
		if (!outbound.detour.IsNullOrEmpty())
		{
			return false;
		}
		string text = outbound.server ?? string.Empty;
		if (text.Equals("localhost", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (IPAddress.TryParse(text, out IPAddress address))
		{
			return !IPAddress.IsLoopback(address);
		}
		return true;
	}

	private void GenDns()
	{
		try
		{
			DNSItem rawDnsItem = _context.RawDnsItem;
			if (rawDnsItem != null && rawDnsItem.Enabled)
			{
				GenDnsCustom();
				return;
			}
			GenDnsServers();
			GenDnsRules();
			SingboxConfig coreConfig = _coreConfig;
			if (coreConfig.dns == null)
			{
				Dns4Sbox dns4Sbox = (coreConfig.dns = new Dns4Sbox());
			}
			_coreConfig.dns.independent_cache = true;
			RoutingItem routingItem = _context.RoutingItem;
			bool flag = false;
			if (routingItem != null)
			{
				List<RulesItem>? obj = JsonUtils.Deserialize<List<RulesItem>>(routingItem.RuleSet) ?? new List<RulesItem>();
				RulesItem rulesItem = ((obj != null) ? obj.LastOrDefault() : null);
				if (rulesItem != null && rulesItem.OutboundTag == "direct")
				{
					bool num = rulesItem.Domain == null || rulesItem.Domain.Count == 0;
					bool flag2 = rulesItem.Process == null || rulesItem.Process.Count == 0;
					bool flag3 = rulesItem.Ip == null || rulesItem.Ip.Count == 0 || rulesItem.Ip.Contains("0.0.0.0/0");
					bool flag4 = string.IsNullOrEmpty(rulesItem.Port) || rulesItem.Port == "0-65535";
					bool flag5 = string.IsNullOrEmpty(rulesItem.Network) || rulesItem.Network == "tcp,udp";
					flag = num & flag2 & flag3 & flag4 & flag5;
				}
			}
			_coreConfig.dns.final = (flag ? "direct_dns" : "remote_dns");
			SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
			if (!flag && simpleDnsItem.FakeIP == true && simpleDnsItem.GlobalFakeIp == false)
			{
				_coreConfig.dns.rules.Add(new Rule4Sbox
				{
					server = "fake_dns",
					query_type = new List<int> { 1, 28 },
					rewrite_ttl = 1
				});
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void GenDnsServers()
	{
		SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
		Server4Sbox server4Sbox = GenBootstrapDns();
		Server4Sbox server4Sbox2 = ParseDnsAddress(simpleDnsItem.DirectDNS ?? Global.DomainDirectDNSAddress.First());
		server4Sbox2.tag = "direct_dns";
		server4Sbox2.domain_resolver = "local_local";
		Server4Sbox server4Sbox3 = ParseDnsAddress(simpleDnsItem.RemoteDNS ?? Global.DomainRemoteDNSAddress.First());
		server4Sbox3.tag = "remote_dns";
		server4Sbox3.detour = "proxy";
		server4Sbox3.domain_resolver = "local_local";
		Server4Sbox server4Sbox4 = new Server4Sbox
		{
			tag = "hosts_dns",
			type = "hosts",
			predefined = new Dictionary<string, List<string>>()
		};
		if (simpleDnsItem.AddCommonHosts == true)
		{
			server4Sbox4.predefined = new Dictionary<string, List<string>>(Global.PredefinedHosts);
		}
		if (simpleDnsItem.UseSystemHosts == true)
		{
			Dictionary<string, string> systemHosts = Utils.GetSystemHosts();
			if (systemHosts != null && systemHosts.Count > 0)
			{
				foreach (KeyValuePair<string, string> item2 in systemHosts)
				{
					server4Sbox4.predefined.TryAdd(item2.Key, new List<string> { item2.Value });
				}
			}
		}
		foreach (KeyValuePair<string, List<string>> item3 in Utils.ParseHostsToDictionary(simpleDnsItem.Hosts))
		{
			Rule4Sbox rule4Sbox = new Rule4Sbox();
			if (ParseV2Domain(item3.Key, rule4Sbox))
			{
				List<string>? domain_keyword = rule4Sbox.domain_keyword;
				if (domain_keyword != null && domain_keyword.Count > 0 && !item3.Key.Contains(':'))
				{
					rule4Sbox.domain = rule4Sbox.domain_keyword;
					rule4Sbox.domain_keyword = null;
				}
				List<string>? domain = rule4Sbox.domain;
				if (domain != null && domain.Count == 1)
				{
					server4Sbox4.predefined[rule4Sbox.domain.First()] = item3.Value.Where(Utils.IsIpAddress).ToList();
				}
			}
		}
		foreach (string protectDomain in _context.ProtectDomainList)
		{
			string text = StxServerAddress.Get(protectDomain) ?? (Utils.IsIpAddress(protectDomain) ? protectDomain : StxServerAddress.DirectProbe?.Ip);
			if (!text.IsNullOrEmpty())
			{
				Dictionary<string, List<string>>? predefined = server4Sbox4.predefined;
				int num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = text;
				predefined[protectDomain] = list;
			}
		}
		foreach (KeyValuePair<string, List<string>> item4 in server4Sbox4.predefined)
		{
			if (server4Sbox.server == item4.Key)
			{
				server4Sbox.domain_resolver = "hosts_dns";
			}
			if (server4Sbox3.server == item4.Key)
			{
				server4Sbox3.domain_resolver = "hosts_dns";
			}
			if (server4Sbox2.server == item4.Key)
			{
				server4Sbox2.domain_resolver = "hosts_dns";
			}
		}
		SingboxConfig coreConfig = _coreConfig;
		Dns4Sbox dns4Sbox;
		if (coreConfig.dns == null)
		{
			dns4Sbox = (coreConfig.dns = new Dns4Sbox());
		}
		dns4Sbox = _coreConfig.dns;
		if (dns4Sbox.servers == null)
		{
			List<Server4Sbox> list2 = (dns4Sbox.servers = new List<Server4Sbox>());
		}
		_coreConfig.dns.servers.Add(server4Sbox3);
		_coreConfig.dns.servers.Add(server4Sbox2);
		_coreConfig.dns.servers.Add(server4Sbox4);
		if (simpleDnsItem.FakeIP == true)
		{
			Server4Sbox item = new Server4Sbox
			{
				tag = "fake_dns",
				type = "fakeip",
				inet4_range = "198.18.0.0/15",
				inet6_range = "fc00::/18"
			};
			_coreConfig.dns.servers.Add(item);
		}
	}

	private Server4Sbox GenBootstrapDns()
	{
		Server4Sbox server4Sbox = ParseDnsAddress(_context.SimpleDnsItem?.BootstrapDNS ?? Global.DomainPureIPDNSAddress.First());
		server4Sbox.tag = "local_local";
		SingboxConfig coreConfig = _coreConfig;
		Dns4Sbox dns4Sbox;
		if (coreConfig.dns == null)
		{
			dns4Sbox = (coreConfig.dns = new Dns4Sbox());
		}
		dns4Sbox = _coreConfig.dns;
		if (dns4Sbox.servers == null)
		{
			List<Server4Sbox> list = (dns4Sbox.servers = new List<Server4Sbox>());
		}
		_coreConfig.dns.servers.Add(server4Sbox);
		return server4Sbox;
	}

	private void GenDnsRules()
	{
		SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
		SingboxConfig coreConfig = _coreConfig;
		Dns4Sbox dns4Sbox;
		if (coreConfig.dns == null)
		{
			dns4Sbox = (coreConfig.dns = new Dns4Sbox());
		}
		dns4Sbox = _coreConfig.dns;
		if (dns4Sbox.rules == null)
		{
			List<Rule4Sbox> list = (dns4Sbox.rules = new List<Rule4Sbox>());
		}
		_coreConfig.dns.rules.Add(new Rule4Sbox
		{
			ip_accept_any = true,
			server = "hosts_dns"
		});
		if (_context.ProtectDomainList.Count > 0)
		{
			_coreConfig.dns.rules.Add(new Rule4Sbox
			{
				server = "direct_dns",
				strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Freedom),
				domain = _context.ProtectDomainList.ToList()
			});
		}
		_coreConfig.dns.rules.AddRange(new Rule4Sbox[2]
		{
			new Rule4Sbox
			{
				server = "remote_dns",
				strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Proxy),
				clash_mode = ERuleMode.Global.ToString()
			},
			new Rule4Sbox
			{
				server = "direct_dns",
				strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Freedom),
				clash_mode = ERuleMode.Direct.ToString()
			}
		});
		foreach (KeyValuePair<string, List<string>> item2 in Utils.ParseHostsToDictionary(simpleDnsItem.Hosts))
		{
			string text = item2.Value.First();
			if (text.IsNullOrEmpty())
			{
				continue;
			}
			Rule4Sbox rule4Sbox = new Rule4Sbox();
			int num = 3;
			List<int> list3 = new List<int>(num);
			CollectionsMarshal.SetCount(list3, num);
			Span<int> span = CollectionsMarshal.AsSpan(list3);
			span[0] = 1;
			span[1] = 5;
			span[2] = 28;
			rule4Sbox.query_type = list3;
			rule4Sbox.action = "predefined";
			rule4Sbox.rcode = "NOERROR";
			Rule4Sbox rule4Sbox2 = rule4Sbox;
			if (!ParseV2Domain(item2.Key, rule4Sbox2))
			{
				continue;
			}
			List<string>? domain_keyword = rule4Sbox2.domain_keyword;
			if (domain_keyword != null && domain_keyword.Count > 0 && !item2.Key.Contains(':'))
			{
				rule4Sbox2.domain = rule4Sbox2.domain_keyword;
				rule4Sbox2.domain_keyword = null;
			}
			if (text.StartsWith('#') && int.TryParse(text.AsSpan(1), out var result))
			{
				Rule4Sbox rule4Sbox3 = rule4Sbox2;
				rule4Sbox3.rcode = result switch
				{
					0 => "NOERROR", 
					1 => "FORMERR", 
					2 => "SERVFAIL", 
					3 => "NXDOMAIN", 
					4 => "NOTIMP", 
					5 => "REFUSED", 
					_ => "NOERROR", 
				};
			}
			else if (Utils.IsDomain(text))
			{
				rule4Sbox2.answer = new List<string> { "*. IN CNAME " + text + "." };
			}
			else
			{
				if (!Utils.IsIpAddress(text))
				{
					continue;
				}
				List<string>? domain = rule4Sbox2.domain;
				if (domain != null && domain.Count != 0)
				{
					continue;
				}
				if (Utils.IsIpv6(text))
				{
					rule4Sbox2.answer = new List<string> { "*. IN AAAA " + text };
				}
				else
				{
					rule4Sbox2.answer = new List<string> { "*. IN A " + text };
				}
			}
			_coreConfig.dns.rules.Add(rule4Sbox2);
		}
		if (simpleDnsItem.BlockBindingQuery == true)
		{
			List<Rule4Sbox> rules = _coreConfig.dns.rules;
			Rule4Sbox rule4Sbox4 = new Rule4Sbox();
			int num = 2;
			List<int> list4 = new List<int>(num);
			CollectionsMarshal.SetCount(list4, num);
			Span<int> span2 = CollectionsMarshal.AsSpan(list4);
			span2[0] = 64;
			span2[1] = 65;
			rule4Sbox4.query_type = list4;
			rule4Sbox4.action = "predefined";
			rule4Sbox4.rcode = "NOERROR";
			rules.Add(rule4Sbox4);
		}
		if (simpleDnsItem.FakeIP == true && simpleDnsItem.GlobalFakeIp == true)
		{
			Rule4Sbox rule4Sbox5 = JsonUtils.Deserialize<Rule4Sbox>(EmbedUtils.GetEmbedText("ServiceLib.Sample.singbox_fakeip_filter"));
			rule4Sbox5.invert = true;
			Rule4Sbox rule4Sbox6 = new Rule4Sbox
			{
				server = "fake_dns",
				type = "logical",
				mode = "and",
				rewrite_ttl = 1
			};
			int num = 2;
			List<Rule4Sbox> list5 = new List<Rule4Sbox>(num);
			CollectionsMarshal.SetCount(list5, num);
			Span<Rule4Sbox> span3 = CollectionsMarshal.AsSpan(list5);
			ref Rule4Sbox reference = ref span3[0];
			Rule4Sbox rule4Sbox7 = new Rule4Sbox();
			int num2 = 2;
			List<int> list6 = new List<int>(num2);
			CollectionsMarshal.SetCount(list6, num2);
			Span<int> span4 = CollectionsMarshal.AsSpan(list6);
			span4[0] = 1;
			span4[1] = 28;
			rule4Sbox7.query_type = list6;
			reference = rule4Sbox7;
			span3[1] = rule4Sbox5;
			rule4Sbox6.rules = list5;
			Rule4Sbox item = rule4Sbox6;
			_coreConfig.dns.rules.Add(item);
		}
		RoutingItem routingItem = _context.RoutingItem;
		if (routingItem == null)
		{
			return;
		}
		List<RulesItem> list7 = JsonUtils.Deserialize<List<RulesItem>>(routingItem.RuleSet) ?? new List<RulesItem>();
		List<string> list8 = new List<string>();
		List<string> list9 = new List<string>();
		string regionName = string.Empty;
		if (!string.IsNullOrEmpty(simpleDnsItem?.DirectExpectedIPs))
		{
			foreach (string item3 in (from s in simpleDnsItem.DirectExpectedIPs.Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
				select s.Trim() into s
				where !string.IsNullOrEmpty(s)
				select s).ToList())
			{
				if (item3.StartsWith("geoip:", StringComparison.OrdinalIgnoreCase))
				{
					string text2 = item3.Substring("geoip:".Length);
					if (!string.IsNullOrEmpty(text2))
					{
						list9.Add(text2);
						regionName = text2;
					}
				}
				else
				{
					list8.Add(item3);
				}
			}
		}
		foreach (RulesItem item4 in list7)
		{
			if (!item4.Enabled || item4.Domain == null || item4.Domain.Count == 0 || item4.RuleType == ERuleType.Routing)
			{
				continue;
			}
			Rule4Sbox rule = new Rule4Sbox();
			if (Enumerable.Count(item4.Domain, (string it) => ParseV2Domain(it, rule)) <= 0)
			{
				continue;
			}
			if (item4.OutboundTag == "direct")
			{
				rule.server = "direct_dns";
				rule.strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Freedom);
				if (list9.Count > 0)
				{
					List<string>? geosite = rule.geosite;
					if (geosite != null && geosite.Count > 0 && !regionName.IsNullOrEmpty())
					{
						List<string> list10 = rule.geosite.Where((string g) => g.EndsWith("-" + regionName, StringComparison.OrdinalIgnoreCase) || g.EndsWith("@" + regionName, StringComparison.OrdinalIgnoreCase) || g == regionName).ToList();
						if (list10.Count > 0)
						{
							rule.geosite.RemoveAll(list10.Contains);
							Rule4Sbox rule4Sbox8 = JsonUtils.DeepCopy(rule);
							rule4Sbox8.geosite = list10;
							if (list9.Count > 0)
							{
								rule4Sbox8.geoip = list9;
							}
							if (list8.Count > 0)
							{
								rule4Sbox8.ip_cidr = list8;
							}
							_coreConfig.dns.rules.Add(rule4Sbox8);
						}
					}
				}
			}
			else if (item4.OutboundTag == "block")
			{
				rule.action = "predefined";
				rule.rcode = "NXDOMAIN";
			}
			else
			{
				if (simpleDnsItem.FakeIP == true && simpleDnsItem.GlobalFakeIp == false)
				{
					Rule4Sbox rule4Sbox9 = JsonUtils.DeepCopy(rule);
					rule4Sbox9.server = "fake_dns";
					rule4Sbox9.query_type = new List<int> { 1, 28 };
					rule4Sbox9.rewrite_ttl = 1;
					_coreConfig.dns.rules.Add(rule4Sbox9);
				}
				rule.server = "remote_dns";
				rule.strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Proxy);
			}
			_coreConfig.dns.rules.Add(rule);
		}
	}

	private void GenMinimizedDns()
	{
		GenDnsServers();
		foreach (Server4Sbox item in _coreConfig.dns.servers.Where((Server4Sbox s) => !string.IsNullOrEmpty(s.detour)).ToList())
		{
			_coreConfig.dns.servers.Remove(item);
		}
		SingboxConfig coreConfig = _coreConfig;
		Dns4Sbox dns4Sbox;
		if (coreConfig.dns == null)
		{
			dns4Sbox = (coreConfig.dns = new Dns4Sbox());
		}
		dns4Sbox = _coreConfig.dns;
		if (dns4Sbox.rules == null)
		{
			List<Rule4Sbox> list = (dns4Sbox.rules = new List<Rule4Sbox>());
		}
		_coreConfig.dns.rules.Clear();
		_coreConfig.dns.final = "direct_dns";
		_coreConfig.route.default_domain_resolver = new Rule4Sbox
		{
			server = "direct_dns"
		};
	}

	private void GenDnsCustom()
	{
		try
		{
			DNSItem rawDnsItem = _context.RawDnsItem;
			string empty = string.Empty;
			if (_context.IsTunEnabled)
			{
				empty = (string.IsNullOrEmpty(rawDnsItem?.TunDNS) ? EmbedUtils.GetEmbedText("ServiceLib.Sample.tun_singbox_dns") : rawDnsItem?.TunDNS);
			}
			else
			{
				empty = (string.IsNullOrEmpty(rawDnsItem?.NormalDNS) ? EmbedUtils.GetEmbedText("ServiceLib.Sample.dns_singbox_normal") : rawDnsItem?.NormalDNS);
			}
			Dns4Sbox dns4Sbox = JsonUtils.Deserialize<Dns4Sbox>(empty);
			if (dns4Sbox != null)
			{
				_coreConfig.dns = dns4Sbox;
				GenDnsProtectCustom();
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void GenDnsProtectCustom()
	{
		DNSItem rawDnsItem = _context.RawDnsItem;
		Dns4Sbox dns4Sbox = _coreConfig.dns ?? new Dns4Sbox();
		Dns4Sbox dns4Sbox2 = dns4Sbox;
		if (dns4Sbox2.servers == null)
		{
			List<Server4Sbox> list = (dns4Sbox2.servers = new List<Server4Sbox>());
		}
		dns4Sbox2 = dns4Sbox;
		if (dns4Sbox2.rules == null)
		{
			List<Rule4Sbox> list3 = (dns4Sbox2.rules = new List<Rule4Sbox>());
		}
		string text = "local_local";
		dns4Sbox.rules.Insert(0, new Rule4Sbox
		{
			server = text,
			clash_mode = ERuleMode.Direct.ToString()
		});
		dns4Sbox.rules.Insert(0, new Rule4Sbox
		{
			server = ((from t in dns4Sbox.servers
				where t.detour == "proxy"
				select t.tag).FirstOrDefault() ?? "remote"),
			clash_mode = ERuleMode.Global.ToString()
		});
		Server4Sbox server4Sbox = ParseDnsAddress(string.IsNullOrEmpty(rawDnsItem?.DomainDNSAddress) ? Global.DomainPureIPDNSAddress.FirstOrDefault() : rawDnsItem?.DomainDNSAddress);
		server4Sbox.tag = text;
		dns4Sbox.servers.Add(server4Sbox);
		Rule4Sbox rule4Sbox = BuildProtectDomainRule();
		if (rule4Sbox != null)
		{
			dns4Sbox.rules.Insert(0, rule4Sbox);
		}
		_coreConfig.dns = dns4Sbox;
	}

	private Rule4Sbox? BuildProtectDomainRule()
	{
		if (_context.ProtectDomainList.Count == 0)
		{
			return null;
		}
		return new Rule4Sbox
		{
			server = "local_local",
			domain = _context.ProtectDomainList.ToList()
		};
	}

	private static Server4Sbox? ParseDnsAddress(string address)
	{
		string text = address?.Split(address.Contains(',') ? ',' : ';').FirstOrDefault()?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Server4Sbox server4Sbox = new Server4Sbox();
		if ((text == "local" || text == "localhost") ? true : false)
		{
			server4Sbox.type = "local";
			return server4Sbox;
		}
		var (text2, text3, num, text4) = Utils.ParseUrl(text);
		if (text3.Equals("dhcp", StringComparison.OrdinalIgnoreCase))
		{
			server4Sbox.type = "dhcp";
			if (!text2.IsNullOrEmpty() && text2 != "auto")
			{
				server4Sbox.server = text2;
			}
			return server4Sbox;
		}
		if (text3.IsNullOrEmpty())
		{
			server4Sbox.type = "udp";
		}
		else
		{
			server4Sbox.type = text3.Replace("+local", "", StringComparison.OrdinalIgnoreCase).ToLower();
		}
		server4Sbox.server = text2;
		if (num != 0)
		{
			server4Sbox.server_port = num;
		}
		if ((server4Sbox.type == "https" || server4Sbox.type == "h3") && !string.IsNullOrEmpty(text4) && text4 != "/")
		{
			server4Sbox.path = text4;
		}
		return server4Sbox;
	}

	private void GenInbounds()
	{
		try
		{
			string listen = "0.0.0.0";
			int localPort = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
			bool flag = _node.Address == "127.0.0.1" && _node.Port == localPort;
			_coreConfig.inbounds = new List<Inbound4Sbox>();
			if (!_context.IsTunEnabled || !flag)
			{
				Inbound4Sbox inbound4Sbox = new Inbound4Sbox
				{
					type = EInboundProtocol.mixed.ToString(),
					tag = EInboundProtocol.socks.ToString(),
					listen = "127.0.0.1"
				};
				_coreConfig.inbounds.Add(inbound4Sbox);
				inbound4Sbox.listen_port = localPort;
				if (_config.Inbound.First().SecondLocalPortEnabled)
				{
					Inbound4Sbox item = BuildInbound(inbound4Sbox, EInboundProtocol.socks2, bSocks: true);
					_coreConfig.inbounds.Add(item);
				}
				if (_config.Inbound.First().AllowLANConn)
				{
					if (_config.Inbound.First().NewPort4LAN)
					{
						Inbound4Sbox inbound4Sbox2 = BuildInbound(inbound4Sbox, EInboundProtocol.socks3, bSocks: true);
						inbound4Sbox2.listen = listen;
						_coreConfig.inbounds.Add(inbound4Sbox2);
						if (_config.Inbound.First().User.IsNotEmpty() && _config.Inbound.First().Pass.IsNotEmpty())
						{
							inbound4Sbox2.users = new List<User4Sbox>
							{
								new User4Sbox
								{
									username = _config.Inbound.First().User,
									password = _config.Inbound.First().Pass
								}
							};
						}
					}
					else
					{
						inbound4Sbox.listen = listen;
					}
				}
			}
			if (_context.IsTunEnabled)
			{
				if (_config.TunModeItem.Mtu <= 0)
				{
					_config.TunModeItem.Mtu = Global.TunMtus.First();
				}
				if (_config.TunModeItem.Stack.IsNullOrEmpty())
				{
					_config.TunModeItem.Stack = Global.TunStacks.First();
				}
				Inbound4Sbox inbound4Sbox3 = JsonUtils.Deserialize<Inbound4Sbox>(EmbedUtils.GetEmbedText("ServiceLib.Sample.tun_singbox_inbound")) ?? new Inbound4Sbox();
				inbound4Sbox3.interface_name = (_context.IsMacOS ? $"utun{new Random().Next(99)}" : "STX Enterprises TUN");
				inbound4Sbox3.mtu = _config.TunModeItem.Mtu;
				inbound4Sbox3.auto_route = _config.TunModeItem.AutoRoute;
				inbound4Sbox3.strict_route = _config.TunModeItem.StrictRoute;
				inbound4Sbox3.stack = _config.TunModeItem.Stack;
				if (!_config.TunModeItem.EnableIPv6Address)
				{
					int num = 1;
					List<string> list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = "172.18.0.1/30";
					inbound4Sbox3.address = list;
				}
				List<string> excludeList = new List<string>
				{
					"127.0.0.0/8",
					"::1/128",
					"10.0.0.0/8",
					"172.16.0.0/12",
					"192.168.0.0/16",
					"169.254.0.0/16",
					"224.0.0.0/4",
					"fe80::/10",
					"ff00::/8"
				};
				inbound4Sbox3.route_exclude_address = excludeList;
				_coreConfig.inbounds.Add(inbound4Sbox3);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private Inbound4Sbox BuildInbound(Inbound4Sbox inItem, EInboundProtocol protocol, bool bSocks)
	{
		Inbound4Sbox? inbound4Sbox = JsonUtils.DeepCopy(inItem);
		inbound4Sbox.tag = protocol.ToString();
		inbound4Sbox.listen_port = (int?)checked(inItem.listen_port + protocol);
		inbound4Sbox.type = EInboundProtocol.mixed.ToString();
		return inbound4Sbox;
	}

	private void GenLog()
	{
		try
		{
			switch (_config.CoreBasicItem.Loglevel)
			{
			case "debug":
			case "info":
			case "error":
				_coreConfig.log.level = _config.CoreBasicItem.Loglevel;
				break;
			case "warning":
				_coreConfig.log.level = "warn";
				break;
			}
			if (_config.CoreBasicItem.Loglevel == "none")
			{
				_coreConfig.log.disabled = true;
			}
			if (_config.CoreBasicItem.LogEnabled)
			{
				DateTime now = DateTime.Now;
				_coreConfig.log.output = Utils.GetLogPath($"sbox_{now:yyyy-MM-dd}.txt");
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void StripRemoteRulesets()
	{
		checked
		{
			try
			{
				if (_coreConfig?.route == null)
				{
					return;
				}
				int num = _coreConfig.route.rule_set?.Count ?? 0;
				_coreConfig.route.rule_set = null;
				int num2 = 0;
				if (_coreConfig.route.rules != null)
				{
					num2 += _coreConfig.route.rules.RemoveAll((Rule4Sbox r) =>
					{
						List<string>? rule_set = r.rule_set;
						return rule_set != null && rule_set.Count > 0;
					});
					foreach (Rule4Sbox rule in _coreConfig.route.rules)
					{
						rule.geosite = null;
						rule.geoip = null;
						rule.rules?.RemoveAll((Rule4Sbox r) =>
						{
							List<string>? rule_set = r.rule_set;
							return rule_set != null && rule_set.Count > 0;
						});
					}
				}
				if (_coreConfig.dns?.rules != null)
				{
					num2 += _coreConfig.dns.rules.RemoveAll((Rule4Sbox r) =>
					{
						List<string>? rule_set = r.rule_set;
						return rule_set != null && rule_set.Count > 0;
					});
					foreach (Rule4Sbox rule2 in _coreConfig.dns.rules)
					{
						rule2.geosite = null;
						rule2.geoip = null;
						rule2.rules?.RemoveAll((Rule4Sbox r) =>
						{
							List<string>? rule_set = r.rule_set;
							return rule_set != null && rule_set.Count > 0;
						});
					}
				}
				if (num > 0 || num2 > 0)
				{
					Logging.SaveLog($"sing-box: dropped {num} remote rule-set(s) and {num2} rule(s) that needed them; " + "routing is Xray's job and the tunnel must start offline.");
				}
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
			}
		}
	}

	private void GenOutbounds()
	{
		FillRangeProxy(BuildAllProxyOutbounds(), _coreConfig);
	}

	private List<BaseServer4Sbox> BuildAllProxyOutbounds(string baseTagName = "proxy", bool withSelector = true)
	{
		List<BaseServer4Sbox> list = new List<BaseServer4Sbox>();
		if (!_node.ConfigType.IsComplexType())
		{
			BaseServer4Sbox item = BuildProxyOutbound(baseTagName);
			list.Add(item);
		}
		else
		{
			list.AddRange(BuildGroupProxyOutbounds(baseTagName));
		}
		if (withSelector)
		{
			List<string> list2 = (from n in list
				where n.tag.StartsWith(baseTagName)
				select n.tag).ToList();
			if (list2.Count > 1)
			{
				list.InsertRange(0, BuildSelectorOutbounds(list2, baseTagName));
			}
		}
		return list;
	}

	private BaseServer4Sbox BuildProxyOutbound(string baseTagName = "proxy")
	{
		BaseServer4Sbox baseServer4Sbox = BuildProxyServer();
		baseServer4Sbox.tag = baseTagName;
		return baseServer4Sbox;
	}

	private List<BaseServer4Sbox> BuildGroupProxyOutbounds(string baseTagName = "proxy")
	{
		List<BaseServer4Sbox> result = new List<BaseServer4Sbox>();
		switch (_node.ConfigType)
		{
		case EConfigType.PolicyGroup:
			result = BuildOutboundsList(baseTagName);
			break;
		case EConfigType.ProxyChain:
			result = BuildChainOutboundsList(baseTagName);
			break;
		}
		return result;
	}

	private BaseServer4Sbox BuildProxyServer()
	{
		try
		{
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SingboxSampleOutbound");
			if (_node.ConfigType == EConfigType.WireGuard)
			{
				Endpoints4Sbox endpoints4Sbox = JsonUtils.Deserialize<Endpoints4Sbox>(embedText);
				FillEndpoint(endpoints4Sbox);
				return endpoints4Sbox;
			}
			Outbound4Sbox outbound4Sbox = JsonUtils.Deserialize<Outbound4Sbox>(embedText);
			FillOutbound(outbound4Sbox);
			return outbound4Sbox;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		throw new InvalidOperationException();
	}

	private void FillOutbound(Outbound4Sbox outbound)
	{
		try
		{
			ProtocolExtraItem protocolExtra = _node.GetProtocolExtra();
			TransportExtraItem transportExtra = _node.GetTransportExtra();
			string network = _node.GetNetwork();
			outbound.server = _node.Address;
			outbound.server_port = _node.Port;
			outbound.type = Global.ProtocolTypes[_node.ConfigType];
			int value;
			int? num;
			int? num2;
			int value2;
			int? num3;
			string text5;
			int result;
			string? hopInterval;
			switch (_node.ConfigType)
			{
			case EConfigType.VMess:
			{
				outbound.uuid = _node.Password;
				outbound.alter_id = (int.TryParse(protocolExtra.AlterId, out var result4) ? result4 : 0);
				if (Global.VmessSecurities.Contains(protocolExtra.VmessSecurity))
				{
					outbound.security = protocolExtra.VmessSecurity;
				}
				else
				{
					outbound.security = "auto";
				}
				FillOutboundMux(outbound);
				FillOutboundTransport(outbound);
				break;
			}
			case EConfigType.Shadowsocks:
				outbound.method = (AppManager.Instance.GetShadowsocksSecurities(_node).Contains(protocolExtra.SsMethod) ? protocolExtra.SsMethod : "none");
				outbound.password = _node.Password;
				outbound.udp_over_tcp = ((protocolExtra.Uot == true) ? new bool?(true) : ((bool?)null));
				if (network == "raw" && transportExtra.RawHeaderType == "http")
				{
					outbound.plugin = "obfs-local";
					outbound.plugin_opts = "obfs=http;obfs-host=" + transportExtra.Host + ";";
				}
				else
				{
					string text = string.Empty;
					if (network == "ws")
					{
						text += "mode=websocket;";
						text = text + "host=" + transportExtra.Host + ";";
						string text2 = (transportExtra.Path ?? string.Empty).Replace("\\", "\\\\").Replace("=", "\\=").Replace(",", "\\,");
						text = text + "path=" + text2 + ";";
					}
					if (_node.StreamSecurity == "tls")
					{
						text += "tls;";
						List<string> list = CertPemManager.ParsePemChain(_node.Cert);
						if (list.Count > 0)
						{
							string text3 = list.First().Replace("-----BEGIN CERTIFICATE-----\n", "").Replace("\n-----END CERTIFICATE-----", "")
								.Trim();
							text3 = text3.Replace("=", "\\=");
							text = text + "certRaw=" + text3 + ";";
						}
					}
					if (text.Length > 0)
					{
						outbound.plugin = "v2ray-plugin";
						text += "mux=0;";
						string text4 = text;
						text = text4.Substring(0, text4.Length - 1);
						outbound.plugin_opts = text;
					}
				}
				FillOutboundMux(outbound);
				break;
			case EConfigType.SOCKS:
				outbound.version = "5";
				if (_node.Username.IsNotEmpty() && _node.Password.IsNotEmpty())
				{
					outbound.username = _node.Username;
					outbound.password = _node.Password;
				}
				break;
			case EConfigType.HTTP:
				if (_node.Username.IsNotEmpty() && _node.Password.IsNotEmpty())
				{
					outbound.username = _node.Username;
					outbound.password = _node.Password;
				}
				break;
			case EConfigType.VLESS:
				outbound.uuid = _node.Password;
				outbound.packet_encoding = "xudp";
				if (!protocolExtra.Flow.IsNullOrEmpty())
				{
					outbound.flow = protocolExtra.Flow;
				}
				else
				{
					FillOutboundMux(outbound);
				}
				FillOutboundTransport(outbound);
				break;
			case EConfigType.Trojan:
				outbound.password = _node.Password;
				FillOutboundMux(outbound);
				FillOutboundTransport(outbound);
				break;
			case EConfigType.Hysteria2:
				outbound.password = _node.Password;
				if (!protocolExtra.SalamanderPass.IsNullOrEmpty())
				{
					outbound.obfs = new HyObfs4Sbox
					{
						type = "salamander",
						password = protocolExtra.SalamanderPass.TrimEx()
					};
				}
				num2 = protocolExtra?.UpMbps;
				if (num2.HasValue)
				{
					int valueOrDefault2 = num2.GetValueOrDefault();
					if (valueOrDefault2 >= 0)
					{
						value = valueOrDefault2;
						goto IL_04e6;
					}
				}
				value = _config.HysteriaItem.UpMbps;
				goto IL_04e6;
			case EConfigType.TUIC:
				outbound.uuid = _node.Username;
				outbound.password = _node.Password;
				outbound.congestion_control = protocolExtra.CongestionControl;
				break;
			case EConfigType.Anytls:
				outbound.password = _node.Password;
				break;
			case EConfigType.Naive:
				{
					outbound.username = _node.Username;
					outbound.password = _node.Password;
					if (protocolExtra.NaiveQuic == true)
					{
						outbound.quic = true;
						outbound.quic_congestion_control = protocolExtra.CongestionControl.NullIfEmpty();
					}
					if (protocolExtra.InsecureConcurrency > 0)
					{
						outbound.insecure_concurrency = protocolExtra.InsecureConcurrency;
					}
					outbound.udp_over_tcp = ((protocolExtra.Uot == true) ? new bool?(true) : ((bool?)null));
					break;
				}
				IL_04e6:
				num = value;
				num2 = protocolExtra?.DownMbps;
				if (num2.HasValue)
				{
					int valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault >= 0)
					{
						value2 = valueOrDefault;
						goto IL_052f;
					}
				}
				value2 = _config.HysteriaItem.DownMbps;
				goto IL_052f;
				IL_052f:
				num3 = value2;
				outbound.up_mbps = ((num > 0) ? num : ((int?)null));
				outbound.down_mbps = ((num3 > 0) ? num3 : ((int?)null));
				text5 = (((object)protocolExtra != null && protocolExtra.Ports?.IsNullOrEmpty() == false) ? protocolExtra.Ports : null);
				if (text5.IsNullOrEmpty() || (!text5.Contains(':') && !text5.Contains('-') && !text5.Contains(',')))
				{
					break;
				}
				outbound.server_port = null;
				outbound.server_ports = (from p in text5.Split(',')
					select p.Trim() into p
					where p.IsNotEmpty()
					select p).Select((string p) =>
				{
					string text6 = p.Replace('-', ':');
					return (!text6.Contains(':')) ? (text6 + ":" + text6) : text6;
				}).ToList();
				outbound.hop_interval = ((_config.HysteriaItem.HopInterval >= 5) ? $"{_config.HysteriaItem.HopInterval}s" : $"{30}s");
				if (int.TryParse(protocolExtra.HopInterval, out result))
				{
					outbound.hop_interval = ((result >= 5) ? $"{result}s" : outbound.hop_interval);
					break;
				}
				hopInterval = protocolExtra.HopInterval;
				if (hopInterval != null && hopInterval.Contains('-'))
				{
					string[] array = protocolExtra.HopInterval.Split('-');
					if (array.Length == 2 && int.TryParse(array[0], out var result2) && int.TryParse(array[0], out var result3))
					{
						int num4 = checked(result2 + result3) / 2;
						outbound.hop_interval = ((num4 >= 5) ? $"{num4}s" : outbound.hop_interval);
					}
				}
				break;
			}
			FillOutboundTls(outbound);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillEndpoint(Endpoints4Sbox endpoint)
	{
		try
		{
			ProtocolExtraItem protocolExtra = _node.GetProtocolExtra();
			List<string> list = Utils.String2List(protocolExtra.WgInterfaceAddress)?.Select((string s) => s.Trim()).ToList();
			if (list == null)
			{
				int num = 1;
				list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = "172.16.0.2/32";
			}
			endpoint.address = list;
			endpoint.type = Global.ProtocolTypes[_node.ConfigType];
			if (_node.ConfigType == EConfigType.WireGuard)
			{
				Peer4Sbox peer4Sbox = new Peer4Sbox
				{
					public_key = (protocolExtra.WgPublicKey ?? string.Empty),
					pre_shared_key = protocolExtra.WgPresharedKey,
					reserved = Utils.String2List(protocolExtra.WgReserved)?.Select((string s) => s.Trim()).Select(int.Parse).ToList(),
					address = _node.Address,
					port = _node.Port
				};
				int num = 2;
				List<string> list2 = new List<string>(num);
				CollectionsMarshal.SetCount(list2, num);
				Span<string> span = CollectionsMarshal.AsSpan(list2);
				span[0] = "0.0.0.0/0";
				span[1] = "::/0";
				peer4Sbox.allowed_ips = list2;
				Peer4Sbox peer4Sbox2 = peer4Sbox;
				endpoint.private_key = _node.Password;
				endpoint.mtu = ((protocolExtra.WgMtu > 0) ? protocolExtra.WgMtu : new int?(Global.TunMtus.First()));
				num = 1;
				List<Peer4Sbox> list3 = new List<Peer4Sbox>(num);
				CollectionsMarshal.SetCount(list3, num);
				CollectionsMarshal.AsSpan(list3)[0] = peer4Sbox2;
				endpoint.peers = list3;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillOutboundMux(Outbound4Sbox outbound)
	{
		try
		{
			if ((_node.MuxEnabled ?? _config.CoreBasicItem.MuxEnabled) && _config.Mux4SboxItem.Protocol.IsNotEmpty())
			{
				Multiplex4Sbox multiplex = new Multiplex4Sbox
				{
					enabled = true,
					protocol = _config.Mux4SboxItem.Protocol,
					max_connections = _config.Mux4SboxItem.MaxConnections,
					padding = _config.Mux4SboxItem.Padding
				};
				outbound.multiplex = multiplex;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillOutboundTls(Outbound4Sbox outbound)
	{
		try
		{
			string streamSecurity = _node.StreamSecurity;
			if ((!(streamSecurity == "reality") && !(streamSecurity == "tls")) || 1 == 0)
			{
				return;
			}
			EConfigType configType = _node.ConfigType;
			if (((uint)(configType - 3) <= 1u || configType == EConfigType.WireGuard) ? true : false)
			{
				return;
			}
			string empty = string.Empty;
			empty = ((!_node.Sni.IsNotEmpty()) ? Utils.String2List(_node.GetNetwork() switch
			{
				"raw" => _node.GetTransportExtra().Host, 
				"ws" => _node.GetTransportExtra().Host, 
				"httpupgrade" => _node.GetTransportExtra().Host, 
				"xhttp" => _node.GetTransportExtra().Host, 
				"grpc" => _node.GetTransportExtra().GrpcAuthority, 
				_ => null, 
			})?.First() : _node.Sni);
			Tls4Sbox tls4Sbox = new Tls4Sbox
			{
				enabled = true,
				record_fragment = (_config.CoreBasicItem.EnableFragment ? new bool?(true) : ((bool?)null)),
				server_name = empty,
				insecure = Utils.ToBool(_node.AllowInsecure),
				alpn = _node.GetAlpn()
			};
			if (_node.Fingerprint.IsNotEmpty())
			{
				tls4Sbox.utls = new Utls4Sbox
				{
					enabled = true,
					fingerprint = (_node.Fingerprint.IsNullOrEmpty() ? _config.CoreBasicItem.DefFingerprint : _node.Fingerprint)
				};
			}
			if (_node.StreamSecurity == "tls")
			{
				List<string> list = CertPemManager.ParsePemChain(_node.Cert);
				if (list.Count > 0)
				{
					tls4Sbox.certificate = list;
					tls4Sbox.insecure = false;
				}
			}
			else if (_node.StreamSecurity == "reality")
			{
				tls4Sbox.reality = new Reality4Sbox
				{
					enabled = true,
					public_key = _node.PublicKey,
					short_id = _node.ShortId
				};
				tls4Sbox.insecure = false;
			}
			Ech4Sbox item = ParseEchParam(_node.EchConfigList).ech;
			if (item != null)
			{
				tls4Sbox.ech = item;
			}
			outbound.tls = tls4Sbox;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillOutboundTransport(Outbound4Sbox outbound)
	{
		try
		{
			Transport4Sbox transport4Sbox = new Transport4Sbox();
			TransportExtraItem transportExtra = _node.GetTransportExtra();
			string text = _config.CoreBasicItem.DefUserAgent ?? string.Empty;
			string valueOrDefault = Global.RawHttpUserAgentTexts.GetValueOrDefault(text, text);
			switch (_node.GetNetwork())
			{
			case "raw":
				if (!(transportExtra.RawHeaderType == "http"))
				{
					break;
				}
				transport4Sbox.type = "http";
				transport4Sbox.host = (transportExtra.Host.IsNullOrEmpty() ? null : Utils.String2List(transportExtra.Host));
				transport4Sbox.path = transportExtra.Path.NullIfEmpty();
				if (!valueOrDefault.IsNullOrEmpty())
				{
					Transport4Sbox transport4Sbox2 = transport4Sbox;
					if (transport4Sbox2.headers == null)
					{
						Headers4Sbox headers4Sbox = (transport4Sbox2.headers = new Headers4Sbox());
					}
					transport4Sbox.headers.UserAgent = valueOrDefault;
				}
				break;
			case "ws":
			{
				transport4Sbox.type = "ws";
				string text2 = transportExtra.Path;
				if (!text2.IsNullOrEmpty())
				{
					Regex regex = new Regex("[?&]ed=(\\d+)");
					Match match = regex.Match(text2);
					if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
					{
						transport4Sbox.max_early_data = result;
						transport4Sbox.early_data_header_name = "Sec-WebSocket-Protocol";
						text2 = regex.Replace(text2, "");
						text2 = text2.Replace("?&", "?");
						if (text2.EndsWith('?'))
						{
							text2 = text2.TrimEnd('?');
						}
					}
					Match match2 = new Regex("[?&]eh=([^&]+)").Match(text2);
					if (match2.Success)
					{
						transport4Sbox.early_data_header_name = Uri.UnescapeDataString(match2.Groups[1].Value);
					}
				}
				transport4Sbox.path = text2.NullIfEmpty();
				if (transportExtra.Host.IsNotEmpty())
				{
					transport4Sbox.headers = new Headers4Sbox
					{
						Host = transportExtra.Host
					};
				}
				if (!valueOrDefault.IsNullOrEmpty())
				{
					Transport4Sbox transport4Sbox2 = transport4Sbox;
					if (transport4Sbox2.headers == null)
					{
						Headers4Sbox headers4Sbox = (transport4Sbox2.headers = new Headers4Sbox());
					}
					transport4Sbox.headers.UserAgent = valueOrDefault;
				}
				break;
			}
			case "httpupgrade":
				transport4Sbox.type = "httpupgrade";
				transport4Sbox.path = transportExtra.Path.NullIfEmpty();
				transport4Sbox.host = transportExtra.Host.NullIfEmpty();
				if (!valueOrDefault.IsNullOrEmpty())
				{
					Transport4Sbox transport4Sbox2 = transport4Sbox;
					if (transport4Sbox2.headers == null)
					{
						Headers4Sbox headers4Sbox = (transport4Sbox2.headers = new Headers4Sbox());
					}
					transport4Sbox.headers.UserAgent = valueOrDefault;
				}
				break;
			case "grpc":
				transport4Sbox.type = "grpc";
				transport4Sbox.service_name = transportExtra.GrpcServiceName;
				transport4Sbox.idle_timeout = _config.GrpcItem.IdleTimeout?.ToString("##s");
				transport4Sbox.ping_timeout = _config.GrpcItem.HealthCheckTimeout?.ToString("##s");
				transport4Sbox.permit_without_stream = _config.GrpcItem.PermitWithoutStream;
				break;
			}
			if (transport4Sbox.type != null)
			{
				outbound.transport = transport4Sbox;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private List<Outbound4Sbox> BuildSelectorOutbounds(List<string> proxyTags, string baseTagName = "proxy")
	{
		EMultipleLoad valueOrDefault = _node.GetProtocolExtra().MultipleLoad.GetValueOrDefault();
		Outbound4Sbox outbound4Sbox = new Outbound4Sbox
		{
			type = "urltest",
			tag = baseTagName + "-auto",
			outbounds = proxyTags,
			interrupt_exist_connections = false
		};
		if (valueOrDefault == EMultipleLoad.Fallback)
		{
			outbound4Sbox.tolerance = 5000;
		}
		Outbound4Sbox outbound4Sbox2 = new Outbound4Sbox
		{
			type = "selector",
			tag = baseTagName,
			outbounds = JsonUtils.DeepCopy(proxyTags),
			interrupt_exist_connections = false
		};
		outbound4Sbox2.outbounds.Insert(0, outbound4Sbox.tag);
		int num = 2;
		List<Outbound4Sbox> list = new List<Outbound4Sbox>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Outbound4Sbox> span = CollectionsMarshal.AsSpan(list);
		span[0] = outbound4Sbox2;
		span[1] = outbound4Sbox;
		return list;
	}

	private List<BaseServer4Sbox> BuildOutboundsList(string baseTagName = "proxy")
	{
		List<ProfileItem> list = new List<ProfileItem>();
		foreach (string item in Utils.String2List(_node.GetProtocolExtra().ChildItems) ?? new List<string>())
		{
			if (_context.AllProxiesMap.TryGetValue(item, out ProfileItem value))
			{
				list.Add(value);
			}
		}
		List<BaseServer4Sbox> list2 = new List<BaseServer4Sbox>();
		checked
		{
			for (int i = 0; i < list.Count; i++)
			{
				ProfileItem profileItem = list[i];
				string text = $"{baseTagName}-{i + 1}-{profileItem.Remarks}";
				if (list.Count == 1)
				{
					text = baseTagName;
				}
				if (profileItem.ConfigType.IsGroupType())
				{
					List<BaseServer4Sbox> collection = new CoreConfigSingboxService(_context with
					{
						Node = profileItem
					}).BuildGroupProxyOutbounds(text);
					list2.AddRange(collection);
				}
				else
				{
					BaseServer4Sbox baseServer4Sbox = new CoreConfigSingboxService(_context with
					{
						Node = profileItem
					}).BuildProxyOutbound();
					baseServer4Sbox.tag = text;
					list2.Add(baseServer4Sbox);
				}
			}
			return list2;
		}
	}

	private List<BaseServer4Sbox> BuildChainOutboundsList(string baseTagName = "proxy")
	{
		List<ProfileItem> list = new List<ProfileItem>();
		foreach (string item in Utils.String2List(_node.GetProtocolExtra().ChildItems) ?? new List<string>())
		{
			if (_context.AllProxiesMap.TryGetValue(item, out ProfileItem value))
			{
				list.Add(value);
			}
		}
		List<ProfileItem> list2 = list.AsEnumerable().Reverse().ToList();
		List<BaseServer4Sbox> list3 = new List<BaseServer4Sbox>();
		checked
		{
			for (int i = 0; i < list2.Count; i++)
			{
				ProfileItem profileItem = list2[i];
				string currentTag = ((i == 0) ? baseTagName : $"chain-{baseTagName}-{i}-{profileItem.Remarks}");
				string text = ((i != list2.Count - 1) ? $"chain-{baseTagName}-{i + 1}-{list2[i + 1].Remarks}" : null);
				if (profileItem.ConfigType.IsGroupType())
				{
					List<BaseServer4Sbox> list4 = new CoreConfigSingboxService(_context with
					{
						Node = profileItem
					}).BuildGroupProxyOutbounds(currentTag);
					if (!text.IsNullOrEmpty())
					{
						foreach (BaseServer4Sbox item2 in list4.Where((BaseServer4Sbox n) => n?.detour.IsNullOrEmpty() ?? true))
						{
							item2.detour = text;
						}
					}
					if (i != 0)
					{
						List<BaseServer4Sbox> list5 = list4.Where((BaseServer4Sbox n) => n.tag.StartsWith(currentTag)).ToList();
						if (list5.Count == 1)
						{
							foreach (BaseServer4Sbox item3 in list3.Where((BaseServer4Sbox n) => n.detour == currentTag))
							{
								item3.detour = list5.First().tag;
							}
						}
						else if (list5.Count > 1)
						{
							List<BaseServer4Sbox> source = CloneOutbounds(list3);
							list3.Clear();
							int num = 0;
							foreach (BaseServer4Sbox item4 in list5)
							{
								List<BaseServer4Sbox> list6 = CloneOutbounds(source);
								foreach (BaseServer4Sbox item5 in list6)
								{
									string tag = $"{item5.tag}-clone-{num + 1}";
									item5.tag = tag;
								}
								for (int num2 = 0; num2 < list6.Count; num2++)
								{
									BaseServer4Sbox baseServer4Sbox = list6[num2];
									string detour = baseServer4Sbox.detour;
									string text2 = ((num2 + 1 < list6.Count) ? list6[num2 + 1].tag : item4.tag);
									baseServer4Sbox.detour = ((detour == currentTag) ? item4.tag : text2);
									list3.Add(baseServer4Sbox);
								}
								num++;
							}
						}
					}
					list3.AddRange(list4);
				}
				else
				{
					BaseServer4Sbox baseServer4Sbox2 = new CoreConfigSingboxService(_context with
					{
						Node = profileItem
					}).BuildProxyOutbound();
					baseServer4Sbox2.tag = currentTag;
					if (!text.IsNullOrEmpty())
					{
						baseServer4Sbox2.detour = text;
					}
					list3.Add(baseServer4Sbox2);
				}
			}
			return list3;
		}
	}

	private static List<BaseServer4Sbox> CloneOutbounds(List<BaseServer4Sbox> source)
	{
		if (source == null || source.Count == 0)
		{
			return new List<BaseServer4Sbox>();
		}
		List<BaseServer4Sbox> list = new List<BaseServer4Sbox>(source.Count);
		foreach (BaseServer4Sbox item in source)
		{
			BaseServer4Sbox baseServer4Sbox = null;
			if (item is Outbound4Sbox obj)
			{
				baseServer4Sbox = JsonUtils.DeepCopy(obj);
			}
			else if (item is Endpoints4Sbox obj2)
			{
				baseServer4Sbox = JsonUtils.DeepCopy(obj2);
			}
			if (baseServer4Sbox != null)
			{
				list.Add(baseServer4Sbox);
			}
		}
		return list;
	}

	private static void FillRangeProxy(List<BaseServer4Sbox> servers, SingboxConfig singboxConfig, bool prepend = true)
	{
		try
		{
			if (servers != null && servers.Count > 0)
			{
				List<Outbound4Sbox> collection = servers.Where((BaseServer4Sbox s) => s is Outbound4Sbox).Cast<Outbound4Sbox>().ToList();
				List<Endpoints4Sbox> collection2 = servers.Where((BaseServer4Sbox s) => s is Endpoints4Sbox).Cast<Endpoints4Sbox>().ToList();
				if (singboxConfig.endpoints == null)
				{
					List<Endpoints4Sbox> list = (singboxConfig.endpoints = new List<Endpoints4Sbox>());
				}
				if (prepend)
				{
					singboxConfig.outbounds.InsertRange(0, collection);
					singboxConfig.endpoints.InsertRange(0, collection2);
				}
				else
				{
					singboxConfig.outbounds.AddRange(collection);
					singboxConfig.endpoints.AddRange(collection2);
				}
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private static (Ech4Sbox? ech, Server4Sbox? dnsServer) ParseEchParam(string? echConfig)
	{
		if (echConfig.IsNullOrEmpty())
		{
			return (ech: null, dnsServer: null);
		}
		if (!echConfig.Contains("://"))
		{
			Ech4Sbox ech4Sbox = new Ech4Sbox
			{
				enabled = true
			};
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			CollectionsMarshal.AsSpan(list)[0] = "-----BEGIN ECH CONFIGS-----\n" + echConfig + "\n-----END ECH CONFIGS-----";
			ech4Sbox.config = list;
			return (ech: ech4Sbox, dnsServer: null);
		}
		int num2 = echConfig.IndexOf('+');
		string query_server_name = ((num2 > 0) ? echConfig.Substring(0, num2) : null);
		string address = ((num2 > 0) ? echConfig.Substring(checked(num2 + 1)) : echConfig);
		return (ech: new Ech4Sbox
		{
			enabled = true,
			query_server_name = query_server_name
		}, dnsServer: ParseDnsAddress(address));
	}

	private void GenRouting()
	{
		checked
		{
			try
			{
				_coreConfig.route.final = "proxy";
				SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
				string server = "direct_dns";
				string strategy = Utils.DomainStrategy4Sbox(simpleDnsItem.Strategy4Freedom);
				DNSItem rawDnsItem = _context.RawDnsItem;
				if (rawDnsItem != null && rawDnsItem.Enabled)
				{
					server = "local_local";
					strategy = (rawDnsItem.DomainStrategy4Freedom.IsNullOrEmpty() ? null : rawDnsItem.DomainStrategy4Freedom);
				}
				_coreConfig.route.default_domain_resolver = new Rule4Sbox
				{
					server = server,
					strategy = strategy
				};
				if (_context.IsTunEnabled)
				{
					UplinkChoice current = StxUplink.Current;
					if ((object)current != null)
					{
						_coreConfig.route.default_interface = current.Name;
						_coreConfig.route.auto_detect_interface = null;
					}
					else
					{
						_coreConfig.route.auto_detect_interface = true;
					}
					List<Rule4Sbox> list = JsonUtils.Deserialize<List<Rule4Sbox>>(EmbedUtils.GetEmbedText("ServiceLib.Sample.tun_singbox_rules"));
					if (list != null)
					{
						_coreConfig.route.rules.AddRange(list);
					}
					(List<string> lstDnsExe, List<string> lstDirectExe) tuple = BuildRoutingDirectExe();
					List<string> item = tuple.lstDnsExe;
					List<string> item2 = tuple.lstDirectExe;
					List<Rule4Sbox> rules = _coreConfig.route.rules;
					Rule4Sbox rule4Sbox = new Rule4Sbox();
					int num = 1;
					List<int> list2 = new List<int>(num);
					CollectionsMarshal.SetCount(list2, num);
					CollectionsMarshal.AsSpan(list2)[0] = 53;
					rule4Sbox.port = list2;
					rule4Sbox.action = "hijack-dns";
					rule4Sbox.process_name = item;
					rules.Add(rule4Sbox);
					_coreConfig.route.rules.Add(new Rule4Sbox
					{
						outbound = "direct",
						process_name = item2
					});
					_coreConfig.route.rules.Add(new Rule4Sbox
					{
						outbound = "direct",
						ip_cidr = new List<string> { "127.0.0.0/8", "::1/128" }
					});
					string text = _config.TunModeItem.IcmpRouting ?? "";
					if (!Global.TunIcmpRoutingPolicies.Contains(text))
					{
						text = Global.TunIcmpRoutingPolicies.First();
					}
					if (text == "direct")
					{
						List<Rule4Sbox> rules3 = _coreConfig.route.rules;
						Rule4Sbox rule4Sbox3 = new Rule4Sbox();
						num = 1;
						List<string> list6 = new List<string>(num);
						CollectionsMarshal.SetCount(list6, num);
						CollectionsMarshal.AsSpan(list6)[0] = "icmp";
						rule4Sbox3.network = list6;
						rule4Sbox3.outbound = "direct";
						rules3.Add(rule4Sbox3);
					}
					else if (text != "rule")
					{
						string text2;
						if (text == "unreachable")
						{
							text2 = "default";
						}
						else
						{
							text2 = ((!(text == "drop")) ? "reply" : "drop");
						}
						string method = text2;
						List<Rule4Sbox> rules4 = _coreConfig.route.rules;
						Rule4Sbox rule4Sbox4 = new Rule4Sbox();
						num = 1;
						List<string> list7 = new List<string>(num);
						CollectionsMarshal.SetCount(list7, num);
						CollectionsMarshal.AsSpan(list7)[0] = "icmp";
						rule4Sbox4.network = list7;
						rule4Sbox4.action = "reject";
						rule4Sbox4.method = method;
						rules4.Add(rule4Sbox4);
					}
				}
				if (_config.Inbound.First().SniffingEnabled)
				{
					_coreConfig.route.rules.Add(new Rule4Sbox
					{
						action = "sniff"
					});
					List<Rule4Sbox> rules5 = _coreConfig.route.rules;
					Rule4Sbox rule4Sbox5 = new Rule4Sbox
					{
						type = "logical",
						mode = "or",
						action = "hijack-dns"
					};
					int num = 2;
					List<Rule4Sbox> list8 = new List<Rule4Sbox>(num);
					CollectionsMarshal.SetCount(list8, num);
					Span<Rule4Sbox> span = CollectionsMarshal.AsSpan(list8);
					ref Rule4Sbox reference = ref span[0];
					Rule4Sbox rule4Sbox6 = new Rule4Sbox();
					int num2 = 1;
					List<int> list9 = new List<int>(num2);
					CollectionsMarshal.SetCount(list9, num2);
					CollectionsMarshal.AsSpan(list9)[0] = 53;
					rule4Sbox6.port = list9;
					reference = rule4Sbox6;
					ref Rule4Sbox reference2 = ref span[1];
					Rule4Sbox rule4Sbox7 = new Rule4Sbox();
					num2 = 1;
					List<string> list10 = new List<string>(num2);
					CollectionsMarshal.SetCount(list10, num2);
					CollectionsMarshal.AsSpan(list10)[0] = "dns";
					rule4Sbox7.protocol = list10;
					reference2 = rule4Sbox7;
					rule4Sbox5.rules = list8;
					rules5.Add(rule4Sbox5);
				}
				else
				{
					List<Rule4Sbox> rules6 = _coreConfig.route.rules;
					Rule4Sbox rule4Sbox8 = new Rule4Sbox();
					int num = 1;
					List<int> list11 = new List<int>(num);
					CollectionsMarshal.SetCount(list11, num);
					CollectionsMarshal.AsSpan(list11)[0] = 53;
					rule4Sbox8.port = list11;
					rule4Sbox8.action = "hijack-dns";
					rules6.Add(rule4Sbox8);
				}
				List<string> list12 = new List<string>();
				if (rawDnsItem == null || !rawDnsItem.Enabled)
				{
					Dictionary<string, List<string>> source = Utils.ParseHostsToDictionary(simpleDnsItem.Hosts);
					list12.AddRange(source.Select((KeyValuePair<string, List<string>> kvp) => kvp.Key));
					if (simpleDnsItem.UseSystemHosts == true)
					{
						Dictionary<string, string> systemHosts = Utils.GetSystemHosts();
						list12.AddRange(systemHosts.Select((KeyValuePair<string, string> kvp) => kvp.Key));
					}
				}
				if (list12.Count > 0)
				{
					Rule4Sbox rule4Sbox9 = new Rule4Sbox
					{
						action = "resolve"
					};
					int num3 = 0;
					foreach (string item4 in list12)
					{
						Rule4Sbox rule4Sbox10 = new Rule4Sbox();
						if (!ParseV2Domain(item4, rule4Sbox10))
						{
							continue;
						}
						List<string>? domain_keyword = rule4Sbox10.domain_keyword;
						if (domain_keyword != null && domain_keyword.Count > 0 && !item4.Contains(':'))
						{
							rule4Sbox10.domain = rule4Sbox10.domain_keyword;
							rule4Sbox10.domain_keyword = null;
						}
						List<string>? domain = rule4Sbox10.domain;
						if (domain != null && domain.Count > 0)
						{
							Rule4Sbox rule4Sbox11 = rule4Sbox9;
							if (rule4Sbox11.domain == null)
							{
								List<string> list13 = (rule4Sbox11.domain = new List<string>());
							}
							rule4Sbox9.domain.AddRange(rule4Sbox10.domain);
							num3++;
							continue;
						}
						List<string>? domain_keyword2 = rule4Sbox10.domain_keyword;
						if (domain_keyword2 != null && domain_keyword2.Count > 0)
						{
							Rule4Sbox rule4Sbox11 = rule4Sbox9;
							if (rule4Sbox11.domain_keyword == null)
							{
								List<string> list13 = (rule4Sbox11.domain_keyword = new List<string>());
							}
							rule4Sbox9.domain_keyword.AddRange(rule4Sbox10.domain_keyword);
							num3++;
							continue;
						}
						List<string>? domain_suffix = rule4Sbox10.domain_suffix;
						if (domain_suffix != null && domain_suffix.Count > 0)
						{
							Rule4Sbox rule4Sbox11 = rule4Sbox9;
							if (rule4Sbox11.domain_suffix == null)
							{
								List<string> list13 = (rule4Sbox11.domain_suffix = new List<string>());
							}
							rule4Sbox9.domain_suffix.AddRange(rule4Sbox10.domain_suffix);
							num3++;
							continue;
						}
						List<string>? domain_regex = rule4Sbox10.domain_regex;
						if (domain_regex != null && domain_regex.Count > 0)
						{
							Rule4Sbox rule4Sbox11 = rule4Sbox9;
							if (rule4Sbox11.domain_regex == null)
							{
								List<string> list13 = (rule4Sbox11.domain_regex = new List<string>());
							}
							rule4Sbox9.domain_regex.AddRange(rule4Sbox10.domain_regex);
							num3++;
							continue;
						}
						List<string>? geosite = rule4Sbox10.geosite;
						if (geosite != null && geosite.Count > 0)
						{
							Rule4Sbox rule4Sbox11 = rule4Sbox9;
							if (rule4Sbox11.geosite == null)
							{
								List<string> list13 = (rule4Sbox11.geosite = new List<string>());
							}
							rule4Sbox9.geosite.AddRange(rule4Sbox10.geosite);
							num3++;
						}
					}
					if (num3 > 0)
					{
						_coreConfig.route.rules.Add(rule4Sbox9);
					}
				}
				if (StxSplitTunnel.IsActive)
				{
					List<string> processes = StxSplitTunnel.Processes;
					if (StxSplitTunnel.Mode == ESplitTunnelMode.Exclude)
					{
						_coreConfig.route.rules.Add(new Rule4Sbox
						{
							process_name = processes,
							outbound = "direct"
						});
					}
					else
					{
						_coreConfig.route.rules.Add(new Rule4Sbox
						{
							process_name = processes,
							outbound = "proxy"
						});
						List<Rule4Sbox> rules7 = _coreConfig.route.rules;
						Rule4Sbox rule4Sbox12 = new Rule4Sbox();
						int num = 1;
						List<string> list19 = new List<string>(num);
						CollectionsMarshal.SetCount(list19, num);
						CollectionsMarshal.AsSpan(list19)[0] = "tun";
						rule4Sbox12.inbound = list19;
						rule4Sbox12.outbound = "direct";
						rules7.Add(rule4Sbox12);
					}
				}
				_coreConfig.route.rules.Add(new Rule4Sbox
				{
					outbound = "direct",
					clash_mode = ERuleMode.Direct.ToString()
				});
				_coreConfig.route.rules.Add(new Rule4Sbox
				{
					outbound = "proxy",
					clash_mode = ERuleMode.Global.ToString()
				});
				string strategy2 = _config.RoutingBasicItem.DomainStrategy4Singbox.NullIfEmpty();
				RoutingItem routingItem = _context.RoutingItem;
				if (routingItem.DomainStrategy4Singbox.IsNotEmpty())
				{
					strategy2 = routingItem.DomainStrategy4Singbox;
				}
				Rule4Sbox item3 = new Rule4Sbox
				{
					action = "resolve",
					strategy = strategy2
				};
				if (_config.RoutingBasicItem.DomainStrategy == "IPOnDemand")
				{
					_coreConfig.route.rules.Add(item3);
				}
				List<RulesItem> list20 = new List<RulesItem>();
				if (routingItem != null)
				{
					foreach (RulesItem item5 in JsonUtils.Deserialize<List<RulesItem>>(routingItem.RuleSet) ?? new List<RulesItem>())
					{
						if (item5.Enabled && item5.RuleType != ERuleType.DNS)
						{
							GenRoutingUserRule(item5);
							List<string>? ip = item5.Ip;
							if (ip != null && ip.Count > 0)
							{
								list20.Add(item5);
							}
						}
					}
				}
				if (!(_config.RoutingBasicItem.DomainStrategy == "IPIfNonMatch"))
				{
					return;
				}
				_coreConfig.route.rules.Add(item3);
				foreach (RulesItem item6 in list20)
				{
					GenRoutingUserRule(item6);
				}
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
			}
		}
	}

	private static (List<string> lstDnsExe, List<string> lstDirectExe) BuildRoutingDirectExe()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CoreInfo item3 in CoreInfoManager.Instance.GetCoreInfo())
		{
			if (item3.CoreType == ECoreType.app)
			{
				continue;
			}
			foreach (string coreEx in item3.CoreExes)
			{
				if (item3.CoreType != ECoreType.sing_box)
				{
					hashSet.Add(Utils.GetExeName(coreEx));
				}
				hashSet2.Add(Utils.GetExeName(coreEx));
			}
		}
		string fileName = Path.GetFileName(Environment.ProcessPath ?? string.Empty);
		if (fileName.IsNotEmpty())
		{
			hashSet.Add(fileName);
		}
		List<string> item = new List<string>(hashSet);
		List<string> item2 = new List<string>(hashSet2);
		return (lstDnsExe: item, lstDirectExe: item2);
	}

	private void GenRoutingUserRule(RulesItem? item)
	{
		checked
		{
			try
			{
				if (item == null)
				{
					return;
				}
				item.OutboundTag = GenRoutingUserRuleOutbound(item.OutboundTag ?? "proxy");
				List<Rule4Sbox> rules = _coreConfig.route.rules;
				Rule4Sbox rule4Sbox = new Rule4Sbox();
				if (item.OutboundTag == "block")
				{
					rule4Sbox.action = "reject";
				}
				else
				{
					rule4Sbox.outbound = item.OutboundTag;
				}
				if (item.Port.IsNotEmpty())
				{
					List<string> list = (from it in item.Port.Split(',')
						where it.Contains('-')
						select it.Replace("-", ":")).ToList();
					List<int> list2 = (from it in item.Port.Split(',')
						where !it.Contains('-')
						select it.ToInt()).ToList();
					rule4Sbox.port_range = ((list.Count > 0) ? list : null);
					rule4Sbox.port = ((list2.Count > 0) ? list2 : null);
				}
				if (item.Network.IsNotEmpty())
				{
					rule4Sbox.network = Utils.String2List(item.Network);
				}
				List<string>? protocol = item.Protocol;
				if (protocol != null && protocol.Count > 0)
				{
					rule4Sbox.protocol = item.Protocol;
				}
				List<string>? inboundTag = item.InboundTag;
				if (inboundTag != null && inboundTag.Count >= 0)
				{
					rule4Sbox.inbound = item.InboundTag;
				}
				Rule4Sbox rule4Sbox2 = JsonUtils.DeepCopy(rule4Sbox);
				Rule4Sbox rule4Sbox3 = JsonUtils.DeepCopy(rule4Sbox);
				Rule4Sbox obj = JsonUtils.DeepCopy(rule4Sbox);
				bool flag = false;
				List<string>? domain = item.Domain;
				if (domain != null && domain.Count > 0)
				{
					int num = 0;
					foreach (string item2 in item.Domain)
					{
						if (ParseV2Domain(item2, rule4Sbox2))
						{
							num++;
						}
					}
					if (num > 0)
					{
						rules.Add(rule4Sbox2);
						flag = true;
					}
				}
				List<string>? ip = item.Ip;
				if (ip != null && ip.Count > 0)
				{
					int num2 = 0;
					List<string> list3 = item.Ip.Where((string it) => it.StartsWith('!')).ToList();
					if (list3.Count > 0)
					{
						List<string> list4 = item.Ip.Except(list3).ToList();
						Rule4Sbox rule4Sbox4 = rule4Sbox3;
						rule4Sbox4 = JsonUtils.DeepCopy(rule4Sbox3);
						rule4Sbox4.outbound = null;
						rule4Sbox4.action = null;
						foreach (string item3 in list4)
						{
							if (ParseV2Address(item3, rule4Sbox4))
							{
								num2++;
							}
						}
						Rule4Sbox rule4Sbox5 = new Rule4Sbox();
						foreach (string item4 in list3)
						{
							if (ParseV2Address(item4.Substring(1).Trim(), rule4Sbox5))
							{
								num2++;
							}
						}
						rule4Sbox5.invert = true;
						Rule4Sbox rule4Sbox6 = new Rule4Sbox
						{
							outbound = rule4Sbox3.outbound,
							action = rule4Sbox3.action,
							type = "logical",
							mode = "or"
						};
						int num3 = 2;
						List<Rule4Sbox> list5 = new List<Rule4Sbox>(num3);
						CollectionsMarshal.SetCount(list5, num3);
						Span<Rule4Sbox> span = CollectionsMarshal.AsSpan(list5);
						span[0] = rule4Sbox4;
						span[1] = rule4Sbox5;
						rule4Sbox6.rules = list5;
						rule4Sbox3 = rule4Sbox6;
					}
					else
					{
						foreach (string item5 in item.Ip)
						{
							if (ParseV2Address(item5, rule4Sbox3))
							{
								num2++;
							}
						}
					}
					if (num2 > 0)
					{
						rules.Add(rule4Sbox3);
						flag = true;
					}
				}
				List<string>? process = item.Process;
				if (process != null && process.Count > 0)
				{
					Rule4Sbox rule4Sbox7 = JsonUtils.DeepCopy(obj);
					Rule4Sbox rule4Sbox8 = rule4Sbox7;
					if (rule4Sbox8.process_name == null)
					{
						List<string> list6 = (rule4Sbox8.process_name = new List<string>());
					}
					Rule4Sbox rule4Sbox9 = JsonUtils.DeepCopy(obj);
					rule4Sbox8 = rule4Sbox9;
					if (rule4Sbox8.process_path == null)
					{
						List<string> list6 = (rule4Sbox8.process_path = new List<string>());
					}
					foreach (string item6 in item.Process)
					{
						if ((item6 == "self/" || item6 == "xray/") ? true : false)
						{
							rule4Sbox7.process_name.Add(Utils.GetExeName("sing-box"));
						}
						else if (item6.Contains('/') || item6.Contains('\\'))
						{
							string text = item6;
							if (Utils.IsWindows())
							{
								text = text.Replace('/', '\\');
							}
							rule4Sbox9.process_path.Add(text);
						}
						else
						{
							string exeName = Utils.GetExeName(item6);
							rule4Sbox7.process_name.Add(exeName);
						}
					}
					if (rule4Sbox7.process_name.Count > 0)
					{
						rules.Add(rule4Sbox7);
						flag = true;
					}
					if (rule4Sbox9.process_path.Count > 0)
					{
						rules.Add(rule4Sbox9);
						flag = true;
					}
				}
				if (!flag && (rule4Sbox.port != null || rule4Sbox.port_range != null || rule4Sbox.protocol != null || rule4Sbox.inbound != null || rule4Sbox.network != null))
				{
					rules.Add(rule4Sbox);
				}
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
			}
		}
	}

	private static bool ParseV2Domain(string domain, Rule4Sbox rule)
	{
		if (domain.StartsWith('#') || domain.StartsWith("ext:") || domain.StartsWith("ext-domain:"))
		{
			return false;
		}
		if (domain.StartsWith("geosite:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.geosite == null)
			{
				List<string> list = (rule4Sbox.geosite = new List<string>());
			}
			rule.geosite?.Add(domain.Substring("geosite:".Length));
		}
		else if (domain.StartsWith("regexp:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain_regex == null)
			{
				List<string> list = (rule4Sbox.domain_regex = new List<string>());
			}
			rule.domain_regex?.Add(domain.Replace("<COMMA>", ",").Substring(7));
		}
		else if (domain.StartsWith("domain:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain_suffix == null)
			{
				List<string> list = (rule4Sbox.domain_suffix = new List<string>());
			}
			rule.domain_suffix?.Add(domain.Substring(7));
		}
		else if (domain.StartsWith("full:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain == null)
			{
				List<string> list = (rule4Sbox.domain = new List<string>());
			}
			rule.domain?.Add(domain.Substring(5));
		}
		else if (domain.StartsWith("keyword:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain_keyword == null)
			{
				List<string> list = (rule4Sbox.domain_keyword = new List<string>());
			}
			rule.domain_keyword?.Add(domain.Substring(8));
		}
		else if (domain.StartsWith("dotless:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain_keyword == null)
			{
				List<string> list = (rule4Sbox.domain_keyword = new List<string>());
			}
			rule.domain_keyword?.Add(domain.Substring(8));
		}
		else
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.domain_keyword == null)
			{
				List<string> list = (rule4Sbox.domain_keyword = new List<string>());
			}
			rule.domain_keyword?.Add(domain);
		}
		return true;
	}

	private static bool ParseV2Address(string address, Rule4Sbox rule)
	{
		if (address.StartsWith("ext:") || address.StartsWith("ext-ip:"))
		{
			return false;
		}
		if (address.Equals("geoip:private"))
		{
			rule.ip_is_private = true;
		}
		else if (address.StartsWith("geoip:"))
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.geoip == null)
			{
				List<string> list = (rule4Sbox.geoip = new List<string>());
			}
			rule.geoip?.Add(address.Substring("geoip:".Length));
		}
		else
		{
			Rule4Sbox rule4Sbox = rule;
			if (rule4Sbox.ip_cidr == null)
			{
				List<string> list = (rule4Sbox.ip_cidr = new List<string>());
			}
			rule.ip_cidr?.Add(address);
		}
		return true;
	}

	private string GenRoutingUserRuleOutbound(string outboundTag)
	{
		if (Global.OutboundTags.Contains(outboundTag))
		{
			return outboundTag;
		}
		ProfileItem valueOrDefault = _context.AllProxiesMap.GetValueOrDefault("remark:" + outboundTag);
		if (valueOrDefault == null || (!Global.SingboxSupportConfigType.Contains(valueOrDefault.ConfigType) && !valueOrDefault.ConfigType.IsGroupType()))
		{
			return "proxy";
		}
		string tag = $"{valueOrDefault.IndexId}-{"proxy"}-{valueOrDefault.Remarks}";
		if (_coreConfig.outbounds.Any((Outbound4Sbox o) => o.tag.StartsWith(tag)) || (_coreConfig.endpoints != null && _coreConfig.endpoints.Any((Endpoints4Sbox e) => e.tag.StartsWith(tag))))
		{
			return tag;
		}
		FillRangeProxy(new CoreConfigSingboxService(_context with
		{
			Node = valueOrDefault
		}).BuildAllProxyOutbounds(tag), _coreConfig, prepend: false);
		return tag;
	}

	private void ConvertGeo2Ruleset()
	{
		string geosite = "geosite";
		string geoip = "geoip";
		List<string> list = new List<string>();
		foreach (Rule4Sbox item2 in _coreConfig.route.rules.Where((Rule4Sbox t) =>
		{
			List<string>? geosite2 = t.geosite;
			return geosite2 != null && geosite2.Count > 0;
		}).ToList() ?? new List<Rule4Sbox>())
		{
			Rule4Sbox rule4Sbox = item2;
			if (rule4Sbox.rule_set == null)
			{
				List<string> list2 = (rule4Sbox.rule_set = new List<string>());
			}
			item2.rule_set.AddRange(item2?.geosite?.Select((string t) => geosite + "-" + t).ToList());
			item2.geosite = null;
			AddRuleSets(list, item2.rule_set);
		}
		foreach (Rule4Sbox item3 in _coreConfig.route.rules.Where((Rule4Sbox t) =>
		{
			List<string>? geoip2 = t.geoip;
			return geoip2 != null && geoip2.Count > 0;
		}).ToList() ?? new List<Rule4Sbox>())
		{
			Rule4Sbox rule4Sbox = item3;
			if (rule4Sbox.rule_set == null)
			{
				List<string> list2 = (rule4Sbox.rule_set = new List<string>());
			}
			item3.rule_set.AddRange(item3?.geoip?.Select((string t) => geoip + "-" + t).ToList());
			item3.geoip = null;
			AddRuleSets(list, item3.rule_set);
		}
		foreach (Rule4Sbox item4 in _coreConfig.dns?.rules.Where((Rule4Sbox t) =>
		{
			List<string>? geosite2 = t.geosite;
			return geosite2 != null && geosite2.Count > 0;
		}).ToList() ?? new List<Rule4Sbox>())
		{
			Rule4Sbox rule4Sbox = item4;
			if (rule4Sbox.rule_set == null)
			{
				List<string> list2 = (rule4Sbox.rule_set = new List<string>());
			}
			item4.rule_set.AddRange(item4?.geosite?.Select((string t) => geosite + "-" + t).ToList());
			item4.geosite = null;
		}
		foreach (Rule4Sbox item5 in _coreConfig.dns?.rules.Where((Rule4Sbox t) =>
		{
			List<string>? geoip2 = t.geoip;
			return geoip2 != null && geoip2.Count > 0;
		}).ToList() ?? new List<Rule4Sbox>())
		{
			Rule4Sbox rule4Sbox = item5;
			if (rule4Sbox.rule_set == null)
			{
				List<string> list2 = (rule4Sbox.rule_set = new List<string>());
			}
			item5.rule_set.AddRange(item5?.geoip?.Select((string t) => geoip + "-" + t).ToList());
			item5.geoip = null;
		}
		foreach (Rule4Sbox item6 in _coreConfig.dns?.rules.Where((Rule4Sbox t) =>
		{
			List<string>? rule_set = t.rule_set;
			return rule_set != null && rule_set.Count > 0;
		}).ToList() ?? new List<Rule4Sbox>())
		{
			AddRuleSets(list, item6.rule_set);
		}
		foreach (List<Rule4Sbox> item7 in _coreConfig.dns?.rules.Where((Rule4Sbox t) =>
		{
			List<Rule4Sbox>? rules = t.rules;
			return rules != null && rules.Count > 0;
		}).Select((Rule4Sbox t) => t.rules).ToList() ?? new List<List<Rule4Sbox>>())
		{
			foreach (Rule4Sbox item8 in item7 ?? new List<Rule4Sbox>())
			{
				AddRuleSets(list, item8.rule_set);
			}
		}
		List<Ruleset4Sbox> source = new List<Ruleset4Sbox>();
		RoutingItem routingItem = _context.RoutingItem;
		if (routingItem.CustomRulesetPath4Singbox.IsNotEmpty())
		{
			string text = EmbedUtils.LoadResource(routingItem.CustomRulesetPath4Singbox);
			if (text.IsNotEmpty())
			{
				source = (from t in JsonUtils.Deserialize<List<Ruleset4Sbox>>(text) ?? new List<Ruleset4Sbox>()
					where t.tag != null
					where t.type != null
					where t.format != null
					select t).ToList();
			}
		}
		string binPath = Utils.GetBinPath("srss");
		_coreConfig.route.rule_set = new List<Ruleset4Sbox>();
		foreach (string item in new HashSet<string>(list))
		{
			if (item.IsNullOrEmpty())
			{
				continue;
			}
			Ruleset4Sbox ruleset4Sbox = source.FirstOrDefault((Ruleset4Sbox t) => t.tag != null && t.tag.Equals(item));
			if (ruleset4Sbox == null)
			{
				string path = Path.Combine(binPath, item + ".srs");
				if (File.Exists(path))
				{
					ruleset4Sbox = new Ruleset4Sbox
					{
						type = "local",
						format = "binary",
						tag = item,
						path = path
					};
				}
				else
				{
					string format = (string.IsNullOrEmpty(_config.ConstItem.SrsSourceUrl) ? "" : _config.ConstItem.SrsSourceUrl);
					ruleset4Sbox = new Ruleset4Sbox
					{
						type = "remote",
						format = "binary",
						tag = item,
						url = string.Format(format, item.StartsWith(geosite) ? geosite : geoip, item),
						download_detour = "proxy"
					};
				}
			}
			_coreConfig.route.rule_set.Add(ruleset4Sbox);
		}
		static void AddRuleSets(List<string> ruleSets, List<string>? rule_set)
		{
			if (rule_set != null)
			{
				ruleSets.AddRange(rule_set);
			}
		}
	}

	private void GenExperimental()
	{
		SingboxConfig coreConfig = _coreConfig;
		if (coreConfig.experimental == null)
		{
			Experimental4Sbox experimental4Sbox = (coreConfig.experimental = new Experimental4Sbox());
		}
		_coreConfig.experimental.clash_api = new Clash_Api4Sbox
		{
			external_controller = $"{"127.0.0.1"}:{AppManager.Instance.StatePort2}"
		};
		if (_config.CoreBasicItem.EnableCacheFile4Sbox)
		{
			coreConfig = _coreConfig;
			if (coreConfig.experimental == null)
			{
				Experimental4Sbox experimental4Sbox = (coreConfig.experimental = new Experimental4Sbox());
			}
			_coreConfig.experimental.cache_file = new CacheFile4Sbox
			{
				enabled = true,
				path = Utils.GetBinPath("cache.db"),
				store_fakeip = (_context.SimpleDnsItem.FakeIP == true)
			};
		}
	}
}

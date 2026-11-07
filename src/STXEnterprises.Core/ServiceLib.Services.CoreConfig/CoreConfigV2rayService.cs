using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Services.CoreConfig;

public class CoreConfigV2rayService
{
	private static readonly string _tag = "CoreConfigV2rayService";

	private readonly Config _config;

	private readonly ProfileItem _node;

	private V2rayConfig _coreConfig;
	private readonly CoreConfigContext _context;

	public CoreConfigV2rayService(CoreConfigContext context)
	{
		_context = context;
		_config = _context.AppConfig;
		_node = _context.Node;
		_coreConfig = new V2rayConfig();
		
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
			if (_node.GetNetwork() == "quic")
			{
				retResult.Msg = ResUI.Incorrectconfiguration + " - " + _node.GetNetwork();
				return retResult;
			}
			retResult.Msg = ResUI.InitialConfiguration;
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleClientConfig");
			if (embedText.IsNullOrEmpty())
			{
				retResult.Msg = ResUI.FailedGetDefaultConfiguration;
				return retResult;
			}
			_coreConfig = JsonUtils.Deserialize<V2rayConfig>(embedText);
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
			GenStatistic();
			if (_config.CoreBasicItem.EnableFragment)
			{
				ApplyOutboundFragment();
			}
			ApplyOutboundBindInterface();
			ApplyOutboundSendThrough();
			RulesItem4Ray rulesItem4Ray = BuildFinalRule();
			if (!string.IsNullOrEmpty(rulesItem4Ray?.balancerTag))
			{
				_coreConfig.routing.rules.Add(rulesItem4Ray);
			}
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
				string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleClientConfig");
				string embedText2 = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleOutbound");
				if (embedText.IsNullOrEmpty() || embedText2.IsNullOrEmpty())
				{
					retResult.Msg = ResUI.FailedGetDefaultConfiguration;
					return retResult;
				}
				_coreConfig = JsonUtils.Deserialize<V2rayConfig>(embedText);
				if (_coreConfig == null)
				{
					retResult.Msg = ResUI.FailedGenDefaultConfiguration;
					return retResult;
				}
				var (list, list2) = Utils.GetActiveNetworkInfo();
				GenLog();
				_coreConfig.inbounds.Clear();
				_coreConfig.outbounds.Clear();
				_coreConfig.routing.rules.Clear();
				int num = AppManager.Instance.GetLocalPort(EInboundProtocol.speedtest);
				foreach (ServerTestItem selected in selecteds)
				{
					if ((!Global.XraySupportConfigType.Contains(selected.ConfigType) && !selected.ConfigType.IsGroupType()) || (!selected.ConfigType.IsComplexType() && selected.Port <= 0))
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
						Inbounds4Ray inbounds4Ray = new Inbounds4Ray
						{
							listen = "127.0.0.1",
							port = port,
							protocol = EInboundProtocol.mixed.ToString(),
							settings = new Inboundsettings4Ray
							{
								udp = true,
								auth = "noauth"
							}
						};
						inbounds4Ray.tag = inbounds4Ray.protocol + inbounds4Ray.port;
						_coreConfig.inbounds.Add(inbounds4Ray);
						string tag = "proxy" + inbounds4Ray.port;
						bool flag = false;
						List<Outbounds4Ray> list3 = new CoreConfigV2rayService(_context with
						{
							Node = valueOrDefault2
						}).BuildAllProxyOutbounds(tag);
						_coreConfig.outbounds.AddRange(list3);
						if (Enumerable.Count(list3, (Outbounds4Ray n) => n.tag.StartsWith(tag)) > 1)
						{
							flag = true;
							EMultipleLoad valueOrDefault3 = _node.GetProtocolExtra().MultipleLoad.GetValueOrDefault();
							GenObservatory(valueOrDefault3, tag);
							GenBalancer(valueOrDefault3, tag);
						}
						RulesItem4Ray rulesItem4Ray = new RulesItem4Ray();
						int num2 = 1;
						List<string> list4 = new List<string>(num2);
						CollectionsMarshal.SetCount(list4, num2);
						CollectionsMarshal.AsSpan(list4)[0] = inbounds4Ray.tag;
						rulesItem4Ray.inboundTag = list4;
						rulesItem4Ray.outboundTag = tag;
						rulesItem4Ray.type = "field";
						RulesItem4Ray rulesItem4Ray2 = rulesItem4Ray;
						if (flag)
						{
							rulesItem4Ray2.balancerTag = tag + "-balancer";
							rulesItem4Ray2.outboundTag = null;
						}
						_coreConfig.routing.rules.Add(rulesItem4Ray2);
					}
				}
				if (_config.CoreBasicItem.EnableFragment)
				{
					ApplyOutboundFragment();
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
			if (_node.GetNetwork() == "quic")
			{
				retResult.Msg = ResUI.Incorrectconfiguration + " - " + _node.GetNetwork();
				return retResult;
			}
			string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleClientConfig");
			if (embedText.IsNullOrEmpty())
			{
				retResult.Msg = ResUI.FailedGetDefaultConfiguration;
				return retResult;
			}
			_coreConfig = JsonUtils.Deserialize<V2rayConfig>(embedText);
			if (_coreConfig == null)
			{
				retResult.Msg = ResUI.FailedGenDefaultConfiguration;
				return retResult;
			}
			GenLog();
			GenOutbounds();
			_coreConfig.routing.domainStrategy = "AsIs";
			_coreConfig.routing.rules.Clear();
			_coreConfig.inbounds.Clear();
			_coreConfig.inbounds.Add(new Inbounds4Ray
			{
				tag = $"{EInboundProtocol.socks}{port}",
				listen = "127.0.0.1",
				port = port,
				protocol = EInboundProtocol.mixed.ToString(),
				settings = new Inboundsettings4Ray
				{
					udp = true,
					auth = "noauth"
				}
			});
			_coreConfig.routing.rules.Add(BuildFinalRule());
			if (_config.CoreBasicItem.EnableFragment)
			{
				ApplyOutboundFragment();
			}
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

	private void GenObservatory(EMultipleLoad multipleLoad, string baseTagName = "proxy")
	{
		List<string> list = new List<string>();
		list.AddRange(_coreConfig.burstObservatory?.subjectSelector ?? new List<string>());
		list.AddRange(_coreConfig.observatory?.subjectSelector ?? new List<string>());
		if (list.Any(baseTagName.StartsWith))
		{
			return;
		}
		string text = list.FirstOrDefault((string s) => s.StartsWith(baseTagName));
		if (text != null)
		{
			baseTagName = text;
			BurstObservatory4Ray? burstObservatory = _coreConfig.burstObservatory;
			if (burstObservatory != null && burstObservatory.subjectSelector?.Contains(baseTagName) == true)
			{
				_coreConfig.burstObservatory.subjectSelector.Remove(baseTagName);
				_coreConfig.burstObservatory.subjectSelector.Insert(0, baseTagName);
			}
			Observatory4Ray? observatory = _coreConfig.observatory;
			if (observatory != null && observatory.subjectSelector?.Contains(baseTagName) == true)
			{
				_coreConfig.observatory.subjectSelector.Remove(baseTagName);
				_coreConfig.observatory.subjectSelector.Insert(0, baseTagName);
			}
		}
		else if ((multipleLoad == EMultipleLoad.Fallback || multipleLoad == EMultipleLoad.LeastLoad) ? true : false)
		{
			if (_coreConfig.burstObservatory == null)
			{
				V2rayConfig coreConfig = _coreConfig;
				BurstObservatory4Ray burstObservatory4Ray = new BurstObservatory4Ray();
				int num = 1;
				List<string> list2 = new List<string>(num);
				CollectionsMarshal.SetCount(list2, num);
				CollectionsMarshal.AsSpan(list2)[0] = baseTagName;
				burstObservatory4Ray.subjectSelector = list2;
				burstObservatory4Ray.pingConfig = new BurstObservatoryPingConfig4Ray
				{
					destination = AppManager.Instance.Config.SpeedTestItem.SpeedPingTestUrl,
					interval = "5m",
					timeout = "30s",
					sampling = 2
				};
				coreConfig.burstObservatory = burstObservatory4Ray;
			}
			else
			{
				BurstObservatory4Ray burstObservatory2 = _coreConfig.burstObservatory;
				if (burstObservatory2.subjectSelector == null)
				{
					List<string> list3 = (burstObservatory2.subjectSelector = new List<string>());
				}
				_coreConfig.burstObservatory.subjectSelector.Add(baseTagName);
			}
		}
		else
		{
			if (multipleLoad != EMultipleLoad.LeastPing)
			{
				return;
			}
			if (_coreConfig.observatory == null)
			{
				V2rayConfig coreConfig2 = _coreConfig;
				Observatory4Ray observatory4Ray = new Observatory4Ray();
				int num = 1;
				List<string> list5 = new List<string>(num);
				CollectionsMarshal.SetCount(list5, num);
				CollectionsMarshal.AsSpan(list5)[0] = baseTagName;
				observatory4Ray.subjectSelector = list5;
				observatory4Ray.probeUrl = AppManager.Instance.Config.SpeedTestItem.SpeedPingTestUrl;
				observatory4Ray.probeInterval = "3m";
				observatory4Ray.enableConcurrency = true;
				coreConfig2.observatory = observatory4Ray;
			}
			else
			{
				Observatory4Ray observatory2 = _coreConfig.observatory;
				if (observatory2.subjectSelector == null)
				{
					List<string> list3 = (observatory2.subjectSelector = new List<string>());
				}
				_coreConfig.observatory.subjectSelector.Add(baseTagName);
			}
		}
	}

	private void GenBalancer(EMultipleLoad multipleLoad, string selector = "proxy")
	{
		string type = multipleLoad switch
		{
			EMultipleLoad.Random => "random", 
			EMultipleLoad.RoundRobin => "roundRobin", 
			EMultipleLoad.LeastPing => "leastPing", 
			EMultipleLoad.LeastLoad => "leastLoad", 
			_ => "roundRobin", 
		};
		string tag = selector + "-balancer";
		BalancersItem4Ray balancersItem4Ray = new BalancersItem4Ray();
		int num = 1;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = selector;
		balancersItem4Ray.selector = list;
		balancersItem4Ray.strategy = new BalancersStrategy4Ray
		{
			type = type,
			settings = new BalancersStrategySettings4Ray
			{
				expected = 1
			}
		};
		balancersItem4Ray.tag = tag;
		balancersItem4Ray.fallbackTag = ((multipleLoad == EMultipleLoad.Fallback) ? "direct" : null);
		BalancersItem4Ray item = balancersItem4Ray;
		Routing4Ray routing = _coreConfig.routing;
		if (routing.balancers == null)
		{
			List<BalancersItem4Ray> list2 = (routing.balancers = new List<BalancersItem4Ray>());
		}
		_coreConfig.routing.balancers.Add(item);
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
		Routing4Ray routing = _coreConfig.routing;
		if (routing != null && routing.balancers?.Count > 0)
		{
			BalancersItem4Ray balancersItem4Ray = _coreConfig.routing.balancers.FirstOrDefault((BalancersItem4Ray b) => b.tag == "proxy-balancer", null);
			if (balancersItem4Ray != null)
			{
				JsonNode jsonNode2 = jsonNode["routing"]?["rules"];
				if (jsonNode2 != null)
				{
					foreach (JsonNode item in jsonNode2.AsArray())
					{
						if (item["outboundTag"]?.GetValue<string>() == "proxy")
						{
							item.AsObject().Remove("outboundTag");
							item["balancerTag"] = balancersItem4Ray.tag;
						}
					}
				}
			}
			if (jsonNode["routing"] == null)
			{
				jsonNode["routing"] = new JsonObject();
			}
			if (jsonNode["routing"]["balancers"] is JsonArray jsonArray)
			{
				if (JsonNode.Parse(JsonUtils.Serialize(_coreConfig.routing.balancers)) is JsonArray jsonArray2)
				{
					foreach (JsonNode item2 in jsonArray2)
					{
						jsonArray.Add(item2?.DeepClone());
					}
				}
			}
			else
			{
				jsonNode["routing"]["balancers"] = JsonNode.Parse(JsonUtils.Serialize(_coreConfig.routing.balancers));
			}
		}
		if (_coreConfig.observatory != null)
		{
			if (jsonNode["observatory"] == null)
			{
				jsonNode["observatory"] = JsonNode.Parse(JsonUtils.Serialize(_coreConfig.observatory));
			}
			else
			{
				List<string> subjectSelector = _coreConfig.observatory.subjectSelector;
				subjectSelector.AddRange(jsonNode["observatory"]?["subjectSelector"]?.AsArray()?.Select((JsonNode x) => x?.GetValue<string>()) ?? Array.Empty<string>());
				jsonNode["observatory"]["subjectSelector"] = JsonNode.Parse(JsonUtils.Serialize(subjectSelector.Distinct().ToList()));
			}
		}
		if (_coreConfig.burstObservatory != null)
		{
			if (jsonNode["burstObservatory"] == null)
			{
				jsonNode["burstObservatory"] = JsonNode.Parse(JsonUtils.Serialize(_coreConfig.burstObservatory));
			}
			else
			{
				List<string> subjectSelector2 = _coreConfig.burstObservatory.subjectSelector;
				subjectSelector2.AddRange(jsonNode["burstObservatory"]?["subjectSelector"]?.AsArray()?.Select((JsonNode x) => x?.GetValue<string>()) ?? Array.Empty<string>());
				jsonNode["burstObservatory"]["subjectSelector"] = JsonNode.Parse(JsonUtils.Serialize(subjectSelector2.Distinct().ToList()));
			}
		}
		JsonArray jsonArray3 = new JsonArray();
		foreach (Outbounds4Ray outbound in _coreConfig.outbounds)
		{
			bool flag;
			switch (outbound.protocol.ToLower())
			{
			case "blackhole":
			case "dns":
			case "freedom":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				if (fullConfigTemplate.AddProxyOnly == true)
				{
					continue;
				}
			}
			else if (!fullConfigTemplate.ProxyDetour.IsNullOrEmpty() && (outbound.streamSettings?.sockopt?.dialerProxy.IsNullOrEmpty() ?? true) && !Utils.IsPrivateNetwork(outbound.settings?.servers?.FirstOrDefault()?.address ?? outbound.settings?.vnext?.FirstOrDefault()?.address ?? string.Empty))
			{
				FillDialerProxy(outbound, fullConfigTemplate.ProxyDetour);
			}
			jsonArray3.Add(JsonUtils.DeepCopy(outbound));
		}
		if (jsonNode["outbounds"] is JsonArray jsonArray4)
		{
			foreach (JsonNode item3 in jsonArray4)
			{
				jsonArray3.Add(item3?.DeepClone());
			}
		}
		jsonNode["outbounds"] = jsonArray3;
		return JsonUtils.Serialize(jsonNode);
	}

	private void ApplyOutboundBindInterface()
	{
		string text = _config.CoreBasicItem.BindInterface?.TrimEx();
		if (text.IsNullOrEmpty() || (!_context.IsTunEnabled && !_context.IsWindows))
		{
			return;
		}
		foreach (Outbounds4Ray item in _coreConfig.outbounds ?? new List<Outbounds4Ray>())
		{
			if (ShouldBindNet(item))
			{
				Outbounds4Ray outbounds4Ray = item;
				StreamSettings4Ray streamSettings4Ray;
				if (outbounds4Ray.streamSettings == null)
				{
					streamSettings4Ray = (outbounds4Ray.streamSettings = new StreamSettings4Ray());
				}
				streamSettings4Ray = item.streamSettings;
				if (streamSettings4Ray.sockopt == null)
				{
					Sockopt4Ray sockopt4Ray = (streamSettings4Ray.sockopt = new Sockopt4Ray());
				}
				item.streamSettings.sockopt.Interface = text;
				if (item?.streamSettings?.xhttpSettings?.extra != null && JsonUtils.ParseJson(JsonUtils.Serialize(item.streamSettings.xhttpSettings.extra)) is JsonObject jsonObject && jsonObject["downloadSettings"] is JsonObject jsonObject2)
				{
					JsonObject jsonObject3 = (jsonObject2["sockopt"] as JsonObject) ?? new JsonObject();
					jsonObject3["interface"] = text;
					jsonObject2["sockopt"] = jsonObject3;
					item.streamSettings.xhttpSettings.extra = jsonObject;
				}
			}
		}
	}

	private void ApplyOutboundSendThrough()
	{
		string text = _config.CoreBasicItem.SendThrough?.TrimEx();
		if (text.IsNullOrEmpty())
		{
			return;
		}
		foreach (Outbounds4Ray item in _coreConfig.outbounds ?? new List<Outbounds4Ray>())
		{
			item.sendThrough = (ShouldBindNet(item) ? text : null);
		}
	}

	private static bool ShouldBindNet(Outbounds4Ray outbound)
	{
		bool flag;
		switch (outbound.protocol)
		{
		case "freedom":
		case "blackhole":
		case "dns":
		case "loopback":
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
		StreamSettings4Ray streamSettings = outbound.streamSettings;
		if (streamSettings != null && streamSettings.sockopt?.dialerProxy.IsNullOrEmpty() == false)
		{
			return false;
		}
		string text = outbound.settings?.servers?.FirstOrDefault()?.address ?? outbound.settings?.vnext?.FirstOrDefault()?.address ?? outbound.settings?.address?.ToString() ?? string.Empty;
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
				if (!(_coreConfig.routing.domainStrategy != "IPIfNonMatch"))
				{
					JsonNode jsonNode = JsonUtils.SerializeToNode(_coreConfig.dns);
					if (jsonNode != null)
					{
						jsonNode["tag"] = "dns-module";
						_coreConfig.dns = JsonUtils.Deserialize<Dns4Ray>(JsonUtils.Serialize(jsonNode));
						_coreConfig.routing.rules.Add(new RulesItem4Ray
						{
							type = "field",
							inboundTag = new List<string> { "dns-module" },
							outboundTag = "proxy"
						});
					}
				}
				return;
			}
			SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
			Dns4Ray dns4Ray = ((_coreConfig.dns is Dns4Ray dns4Ray2) ? dns4Ray2 : new Dns4Ray());
			string text = simpleDnsItem?.Strategy4Freedom ?? "AsIs";
			if (text.IsNotEmpty() && text != "AsIs")
			{
				Outbounds4Ray outbounds4Ray = _coreConfig.outbounds.FirstOrDefault((Outbounds4Ray t) => t != null && t.protocol == "freedom" && t.tag == "direct");
				if (outbounds4Ray != null)
				{
					outbounds4Ray.settings = new Outboundsettings4Ray
					{
						domainStrategy = text,
						userLevel = 0
					};
				}
			}
			string strategy4Proxy = simpleDnsItem?.Strategy4Proxy ?? "AsIs";
			if (strategy4Proxy.IsNotEmpty() && strategy4Proxy != "AsIs")
			{
				HashSet<string> xraySupportConfigTypeNames = Global.XraySupportConfigType.Select((EConfigType x) => (x != EConfigType.Hysteria2) ? Global.ProtocolTypes[x] : "hysteria").ToHashSet();
				_coreConfig.outbounds.Where((Outbounds4Ray t) => xraySupportConfigTypeNames.Contains(t.protocol)).ToList().ForEach((Outbounds4Ray outbound) =>
				{
					outbound.targetStrategy = strategy4Proxy;
				});
			}
			FillDnsServers(dns4Ray);
			FillDnsHosts(dns4Ray);
			dns4Ray.serveStale = ((simpleDnsItem?.ServeStale ?? false) ? new bool?(true) : ((bool?)null));
			dns4Ray.enableParallelQuery = ((simpleDnsItem?.ParallelQuery ?? false) ? new bool?(true) : ((bool?)null));
			List<string> list = (from server in dns4Ray.servers
				select (!((server as JsonObject)?["tag"] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value)) ? null : value into tag
				where tag?.StartsWith("direct-dns", StringComparison.Ordinal) ?? false
				select (tag)).ToList();
			if (list.Count > 0)
			{
				_coreConfig.routing.rules.Add(new RulesItem4Ray
				{
					type = "field",
					inboundTag = list,
					outboundTag = "direct"
				});
			}
			RulesItem4Ray rulesItem4Ray = BuildFinalRule();
			dns4Ray.tag = "dns-module";
			List<RulesItem4Ray> rules = _coreConfig.routing.rules;
			RulesItem4Ray rulesItem4Ray2 = new RulesItem4Ray
			{
				type = "field"
			};
			int num = 1;
			List<string> list2 = new List<string>(num);
			CollectionsMarshal.SetCount(list2, num);
			CollectionsMarshal.AsSpan(list2)[0] = "dns-module";
			rulesItem4Ray2.inboundTag = list2;
			rulesItem4Ray2.outboundTag = rulesItem4Ray.outboundTag;
			rulesItem4Ray2.balancerTag = rulesItem4Ray.balancerTag;
			rules.Add(rulesItem4Ray2);
			_coreConfig.dns = dns4Ray;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillDnsServers(Dns4Ray dnsItem)
	{
		SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
		List<string> list = ParseDnsAddresses(simpleDnsItem?.DirectDNS, Global.DomainDirectDNSAddress.First());
		List<string> list2 = ParseDnsAddresses(simpleDnsItem?.RemoteDNS, Global.DomainRemoteDNSAddress.First());
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		List<string> list5 = new List<string>();
		List<string> list6 = new List<string>();
		List<string> list7 = new List<string>();
		List<string> list8 = new List<string>();
		string text = string.Empty;
		List<string> dnsAddresses = ParseDnsAddresses(simpleDnsItem?.BootstrapDNS, Global.DomainPureIPDNSAddress.First());
		List<string> list9 = new List<string>();
		foreach (string item6 in list)
		{
			string item = Utils.ParseUrl(item6).domain;
			if (!(item == "localhost") && Utils.IsDomain(item))
			{
				list9.Add("full:" + item);
			}
		}
		foreach (string item7 in list2)
		{
			string item2 = Utils.ParseUrl(item7).domain;
			if (!(item2 == "localhost") && Utils.IsDomain(item2))
			{
				list9.Add("full:" + item2);
			}
		}
		list9 = list9.Distinct().ToList();
		if (!string.IsNullOrEmpty(simpleDnsItem?.DirectExpectedIPs))
		{
			list8 = (from s in simpleDnsItem.DirectExpectedIPs.Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
				select s.Trim() into s
				where !string.IsNullOrEmpty(s)
				select s).ToList();
			foreach (string item8 in from ip in list8
				where ip.StartsWith("geoip:", StringComparison.OrdinalIgnoreCase)
				select ip.Substring("geoip:".Length) into region
				where !string.IsNullOrEmpty(region)
				select region)
			{
				text = item8;
			}
		}
		RoutingItem? routingItem = _context.RoutingItem;
		List<RulesItem> list10 = null;
		list10 = JsonUtils.Deserialize<List<RulesItem>>(routingItem?.RuleSet) ?? new List<RulesItem>();
		foreach (RulesItem item9 in list10)
		{
			if (!item9.Enabled || item9.Domain == null || item9.Domain.Count == 0 || item9.RuleType == ERuleType.Routing)
			{
				continue;
			}
			foreach (string item10 in item9.Domain)
			{
				if (item10.StartsWith('#'))
				{
					continue;
				}
				string text2 = item10.Replace("<COMMA>", ",");
				if (item9.OutboundTag == "direct")
				{
					if (text2.StartsWith("geosite:") || text2.StartsWith("ext:"))
					{
						((!text.IsNullOrEmpty() && (text2.EndsWith("-" + text) || text2.EndsWith("@" + text) || text2 == "geosite:" + text)) ? list7 : list4).Add(text2);
					}
					else
					{
						list3.Add(text2);
					}
				}
				else if (item9.OutboundTag != "block")
				{
					if (text2.StartsWith("geosite:") || text2.StartsWith("ext:"))
					{
						list6.Add(text2);
					}
					else
					{
						list5.Add(text2);
					}
				}
			}
		}
		if (_context.ProtectDomainList.Count > 0)
		{
			list3.AddRange(_context.ProtectDomainList);
		}
		Dns4Ray dns4Ray = dnsItem;
		if (dns4Ray.servers == null)
		{
			List<object> list11 = (dns4Ray.servers = new List<object>());
		}
		int directDnsTagIndex = 1;
		AddDnsServers(list2, list5);
		AddDnsServers(list, list3, isDirectDns: true);
		AddDnsServers(list2, list6);
		AddDnsServers(list, list4, isDirectDns: true);
		AddDnsServers(list, list7, isDirectDns: true, list8);
		if (list9.Count > 0)
		{
			AddDnsServers(dnsAddresses, list9);
		}
		bool flag = false;
		RulesItem rulesItem = ((list10 != null) ? list10.LastOrDefault() : null);
		if (rulesItem != null && rulesItem.OutboundTag == "direct")
		{
			bool num = rulesItem.Domain == null || rulesItem.Domain.Count == 0;
			bool flag2 = rulesItem.Process == null || rulesItem.Process.Count == 0;
			bool flag3 = rulesItem.Ip == null || rulesItem.Ip.Count == 0 || rulesItem.Ip.Contains("0.0.0.0/0");
			bool flag4 = string.IsNullOrEmpty(rulesItem.Port) || rulesItem.Port == "0-65535";
			bool flag5 = string.IsNullOrEmpty(rulesItem.Network) || rulesItem.Network == "tcp,udp";
			flag = num & flag2 & flag3 & flag4 & flag5;
		}
		if (!flag)
		{
			dnsItem.servers.AddRange(list2);
			return;
		}
		foreach (string item11 in list)
		{
			DnsServer4Ray dnsServer4Ray = CreateDnsServer(item11, new List<string>());
			dnsServer4Ray.tag = $"{"direct-dns"}-{checked(directDnsTagIndex++)}";
			dnsServer4Ray.skipFallback = false;
			dnsItem.servers.Add(JsonUtils.SerializeToNode(dnsServer4Ray, new JsonSerializerOptions
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			}));
		}
		void AddDnsServers(List<string> list13, List<string> domains, bool isDirectDns = false, List<string>? expectedIPs = null)
		{
			if (domains.Count <= 0)
			{
				return;
			}
			foreach (string item12 in list13)
			{
				DnsServer4Ray dnsServer4Ray2 = CreateDnsServer(item12, domains, expectedIPs);
				if (isDirectDns)
				{
					dnsServer4Ray2.tag = $"{"direct-dns"}-{checked(directDnsTagIndex++)}";
				}
				JsonNode item3 = JsonUtils.SerializeToNode(dnsServer4Ray2, new JsonSerializerOptions
				{
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				});
				dnsItem.servers.Add(item3);
			}
		}
		static DnsServer4Ray CreateDnsServer(string dnsAddress, List<string> domains, List<string>? expectedIPs = null)
		{
			(string domain, string scheme, int port, string path) tuple = Utils.ParseUrl(dnsAddress);
			string item3 = tuple.domain;
			string item4 = tuple.scheme;
			int item5 = tuple.port;
			string address = dnsAddress;
			int? port = null;
			if (item4.IsNullOrEmpty() || item4.StartsWith("udp", StringComparison.OrdinalIgnoreCase))
			{
				address = item3;
				port = ((item5 > 0) ? new int?(item5) : ((int?)null));
			}
			else if (item4.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
			{
				address = item4 + "://" + item3;
				port = ((item5 > 0) ? new int?(item5) : ((int?)null));
			}
			return new DnsServer4Ray
			{
				address = address,
				port = port,
				skipFallback = true,
				domains = ((domains.Count > 0) ? domains : null),
				expectedIPs = ((expectedIPs != null && expectedIPs.Count > 0) ? expectedIPs : null)
			};
		}
		static List<string> ParseDnsAddresses(string? dnsInput, string defaultAddress)
		{
			List<string> list13 = dnsInput?.Split(dnsInput.Contains(',') ? ',' : ';').Select((string addr) => addr.Trim()).Where((string addr) => !string.IsNullOrEmpty(addr))
				.Select((string addr) => (!addr.StartsWith("dhcp", StringComparison.OrdinalIgnoreCase)) ? addr : "localhost")
				.Distinct()
				.ToList();
			if (list13 == null)
			{
				int num2 = 1;
				list13 = new List<string>(num2);
				CollectionsMarshal.SetCount(list13, num2);
				CollectionsMarshal.AsSpan(list13)[0] = defaultAddress;
			}
			List<string> list14 = list13;
			if (list14.Count <= 0)
			{
				return new List<string> { defaultAddress };
			}
			return list14;
		}
	}

	private void FillDnsHosts(Dns4Ray dnsItem)
	{
		SimpleDNSItem simpleDnsItem = _context.SimpleDnsItem;
		List<(string, string)> list = (from d in _context.ProtectDomainList
			select (Domain: d, Ip: StxServerAddress.Get(d)) into p
			where !p.Ip.IsNullOrEmpty()
			select p).ToList();
		if (simpleDnsItem.AddCommonHosts == false && simpleDnsItem.UseSystemHosts == false && simpleDnsItem.Hosts.IsNullOrEmpty() && list.Count == 0)
		{
			return;
		}
		if (dnsItem.hosts == null)
		{
			Dictionary<string, object> dictionary = (dnsItem.hosts = new Dictionary<string, object>());
		}
		if (simpleDnsItem.AddCommonHosts == true)
		{
			dnsItem.hosts = ((IEnumerable<KeyValuePair<string, List<string>>>)Global.PredefinedHosts).ToDictionary((Func<KeyValuePair<string, List<string>>, string>)((KeyValuePair<string, List<string>> kvp) => kvp.Key), (Func<KeyValuePair<string, List<string>>, object>)((KeyValuePair<string, List<string>> kvp) => kvp.Value));
		}
		if (simpleDnsItem.UseSystemHosts == true)
		{
			Dictionary<string, string> systemHosts = Utils.GetSystemHosts();
			Dictionary<string, object> hosts = dnsItem.hosts;
			if (hosts != null && systemHosts != null && systemHosts.Count > 0)
			{
				foreach (KeyValuePair<string, string> item2 in systemHosts)
				{
					hosts.TryAdd(item2.Key, new List<string> { item2.Value });
				}
			}
		}
		foreach (KeyValuePair<string, List<string>> item3 in Utils.ParseHostsToDictionary(simpleDnsItem.Hosts))
		{
			dnsItem.hosts[item3.Key] = item3.Value;
		}
		foreach (var (key, item) in list)
		{
			dnsItem.hosts[key] = new List<string> { item };
		}
	}

	private void GenDnsCustom()
	{
		try
		{
			DNSItem rawDnsItem = _context.RawDnsItem;
			string text = ((!_context.IsTunEnabled) ? rawDnsItem?.NormalDNS : rawDnsItem?.TunDNS);
			string text2 = rawDnsItem?.DomainStrategy4Freedom;
			if (text.IsNullOrEmpty())
			{
				text = EmbedUtils.GetEmbedText("ServiceLib.Sample.dns_v2ray_normal");
			}
			if (text2.IsNotEmpty())
			{
				Outbounds4Ray outbounds4Ray = _coreConfig.outbounds.FirstOrDefault((Outbounds4Ray t) => t != null && t.protocol == "freedom" && t.tag == "direct");
				if (outbounds4Ray != null)
				{
					outbounds4Ray.settings = new Outboundsettings4Ray();
					outbounds4Ray.settings.domainStrategy = text2;
					outbounds4Ray.settings.userLevel = 0;
				}
			}
			JsonNode jsonNode = JsonUtils.ParseJson(text);
			if (jsonNode == null)
			{
				List<string> list = new List<string>();
				string[] array = text.Split(',');
				foreach (string item in array)
				{
					list.Add(item);
				}
				jsonNode = JsonUtils.ParseJson("{}");
				jsonNode["servers"] = JsonUtils.SerializeToNode(list);
			}
			if (rawDnsItem.UseSystemHosts)
			{
				Dictionary<string, string> systemHosts = Utils.GetSystemHosts();
				if (systemHosts.Count > 0)
				{
					JsonNode jsonNode2 = jsonNode["hosts"];
					if (jsonNode2 != null)
					{
						foreach (KeyValuePair<string, string> item2 in systemHosts)
						{
							if (jsonNode2[item2.Key] == null)
							{
								jsonNode2[item2.Key] = item2.Value;
							}
						}
					}
				}
			}
			JsonNode jsonNode3 = jsonNode["hosts"];
			if (jsonNode3 != null)
			{
				foreach (KeyValuePair<string, JsonNode> item3 in jsonNode3.AsObject().ToList())
				{
					if (item3.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
					{
						jsonNode3[item3.Key] = new JsonArray { (JsonNode)value };
					}
				}
			}
			FillDnsDomainsCustom(jsonNode);
			_coreConfig.dns = jsonNode;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillDnsDomainsCustom(JsonNode dns)
	{
		JsonNode jsonNode = dns["servers"];
		if (jsonNode != null)
		{
			HashSet<string> protectDomainList = _context.ProtectDomainList;
			if (protectDomainList.Count > 0)
			{
				DNSItem rawDnsItem = _context.RawDnsItem;
				DnsServer4Ray obj = new DnsServer4Ray
				{
					address = (string.IsNullOrEmpty(rawDnsItem?.DomainDNSAddress) ? Global.DomainPureIPDNSAddress.FirstOrDefault() : rawDnsItem?.DomainDNSAddress),
					skipFallback = true,
					domains = protectDomainList.ToList()
				};
				jsonNode.AsArray().Add(JsonUtils.SerializeToNode(obj));
			}
		}
	}

	private void GenInbounds()
	{
		try
		{
			string listen = "0.0.0.0";
			int localPort = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
			_coreConfig.inbounds = new List<Inbounds4Ray>();
			Inbounds4Ray inbounds4Ray = BuildInbound(_config.Inbound.First(), EInboundProtocol.socks, bSocks: true);
			bool flag = _node.Address == "127.0.0.1" && _node.Port == localPort;
			if (!_context.IsTunEnabled || !flag)
			{
				_coreConfig.inbounds.Add(inbounds4Ray);
				if (_config.Inbound.First().SecondLocalPortEnabled)
				{
					Inbounds4Ray item = BuildInbound(_config.Inbound.First(), EInboundProtocol.socks2, bSocks: true);
					_coreConfig.inbounds.Add(item);
				}
				if (_config.Inbound.First().AllowLANConn)
				{
					if (_config.Inbound.First().NewPort4LAN)
					{
						Inbounds4Ray inbounds4Ray2 = BuildInbound(_config.Inbound.First(), EInboundProtocol.socks3, bSocks: true);
						inbounds4Ray2.listen = listen;
						_coreConfig.inbounds.Add(inbounds4Ray2);
						if (_config.Inbound.First().User.IsNotEmpty() && _config.Inbound.First().Pass.IsNotEmpty())
						{
							inbounds4Ray2.settings.auth = "password";
							inbounds4Ray2.settings.accounts = new List<AccountsItem4Ray>
							{
								new AccountsItem4Ray
								{
									user = _config.Inbound.First().User,
									pass = _config.Inbound.First().Pass
								}
							};
						}
					}
					else
					{
						inbounds4Ray.listen = listen;
					}
				}
			}
			if (_context.IsTunEnabled)
			{
				if (_config.TunModeItem.Mtu <= 0)
				{
					_config.TunModeItem.Mtu = Global.TunMtus.First();
				}
				Inbounds4Ray inbounds4Ray3 = JsonUtils.Deserialize<Inbounds4Ray>(EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleTunInbound")) ?? new Inbounds4Ray();
				inbounds4Ray3.settings.name = (_context.IsMacOS ? $"utun{new Random().Next(99)}" : "xray_tun");
				inbounds4Ray3.settings.MTU = _config.TunModeItem.Mtu;
				if (!_config.TunModeItem.EnableIPv6Address)
				{
					Inboundsettings4Ray settings = inbounds4Ray3.settings;
					int num = 1;
					List<string> list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = "172.18.0.1/30";
					settings.gateway = list;
				}
				string text = _config.CoreBasicItem.BindInterface?.TrimEx();
				if (!text.IsNullOrEmpty())
				{
					inbounds4Ray3.settings.autoOutboundsInterface = text;
				}
				inbounds4Ray3.sniffing = inbounds4Ray.sniffing;
				_coreConfig.inbounds.Add(inbounds4Ray3);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private Inbounds4Ray BuildInbound(InItem inItem, EInboundProtocol protocol, bool bSocks)
	{
		string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleInbound");
		if (embedText.IsNullOrEmpty())
		{
			return new Inbounds4Ray();
		}
		Inbounds4Ray inbounds4Ray = JsonUtils.Deserialize<Inbounds4Ray>(embedText);
		if (inbounds4Ray == null)
		{
			return new Inbounds4Ray();
		}
		inbounds4Ray.tag = protocol.ToString();
		inbounds4Ray.port = (int)checked(inItem.LocalPort + protocol);
		inbounds4Ray.protocol = EInboundProtocol.mixed.ToString();
		inbounds4Ray.settings.udp = inItem.UdpEnabled;
		inbounds4Ray.sniffing.enabled = inItem.SniffingEnabled;
		inbounds4Ray.sniffing.destOverride = inItem.DestOverride;
		inbounds4Ray.sniffing.routeOnly = inItem.RouteOnly;
		return inbounds4Ray;
	}

	private void GenLog()
	{
		try
		{
			if (_config.CoreBasicItem.LogEnabled)
			{
				DateTime now = DateTime.Now;
				_coreConfig.log.loglevel = _config.CoreBasicItem.Loglevel;
				_coreConfig.log.access = null;
				_coreConfig.log.error = Utils.GetLogPath($"Verror_{now:yyyy-MM-dd}.txt");
			}
			else
			{
				_coreConfig.log.loglevel = _config.CoreBasicItem.Loglevel;
				_coreConfig.log.access = null;
				_coreConfig.log.error = null;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void GenOutbounds()
	{
		List<Outbounds4Ray> list = BuildAllProxyOutbounds();
		_coreConfig.outbounds.InsertRange(0, list);
		if (Enumerable.Count(list, (Outbounds4Ray n) => n.tag.StartsWith("proxy")) > 1)
		{
			EMultipleLoad valueOrDefault = _node.GetProtocolExtra().MultipleLoad.GetValueOrDefault();
			GenObservatory(valueOrDefault);
			GenBalancer(valueOrDefault);
		}
		if (_context.IsTunEnabled)
		{
			_coreConfig.outbounds.Add(BuildDnsOutbound());
		}
	}

	private List<Outbounds4Ray> BuildAllProxyOutbounds(string baseTagName = "proxy")
	{
		List<Outbounds4Ray> list = new List<Outbounds4Ray>();
		if (_node.ConfigType.IsGroupType())
		{
			list.AddRange(BuildGroupProxyOutbounds(baseTagName));
		}
		else
		{
			list.Add(BuildProxyOutbound(baseTagName));
		}
		return list;
	}

	private List<Outbounds4Ray> BuildGroupProxyOutbounds(string baseTagName = "proxy")
	{
		List<Outbounds4Ray> list = new List<Outbounds4Ray>();
		switch (_node.ConfigType)
		{
		case EConfigType.PolicyGroup:
			list.AddRange(BuildOutboundsList(baseTagName));
			break;
		case EConfigType.ProxyChain:
			list.AddRange(BuildChainOutboundsList(baseTagName));
			break;
		}
		return list;
	}

	private Outbounds4Ray BuildProxyOutbound(string baseTagName = "proxy")
	{
		Outbounds4Ray outbounds4Ray = JsonUtils.Deserialize<Outbounds4Ray>(EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleOutbound"));
		FillOutbound(outbounds4Ray);
		outbounds4Ray.tag = baseTagName;
		return outbounds4Ray;
	}

	private void FillOutbound(Outbounds4Ray outbound)
	{
		try
		{
			ProtocolExtraItem protocolExtra = _node.GetProtocolExtra();
			bool flag = _node.MuxEnabled ?? _config.CoreBasicItem.MuxEnabled;
			switch (_node.ConfigType)
			{
			case EConfigType.VMess:
			{
				VnextItem4Ray vnextItem4Ray;
				if (outbound.settings.vnext.Count <= 0)
				{
					vnextItem4Ray = new VnextItem4Ray();
					outbound.settings.vnext.Add(vnextItem4Ray);
				}
				else
				{
					vnextItem4Ray = outbound.settings.vnext.First();
				}
				vnextItem4Ray.address = _node.Address;
				vnextItem4Ray.port = _node.Port;
				UsersItem4Ray usersItem4Ray;
				if (vnextItem4Ray.users.Count <= 0)
				{
					usersItem4Ray = new UsersItem4Ray();
					vnextItem4Ray.users.Add(usersItem4Ray);
				}
				else
				{
					usersItem4Ray = vnextItem4Ray.users.First();
				}
				usersItem4Ray.id = _node.Password;
				usersItem4Ray.alterId = (int.TryParse(protocolExtra?.AlterId, out var result) ? result : 0);
				usersItem4Ray.email = "t@t.tt";
				if (Global.VmessSecurities.Contains(protocolExtra.VmessSecurity))
				{
					usersItem4Ray.security = protocolExtra.VmessSecurity;
				}
				else
				{
					usersItem4Ray.security = "auto";
				}
				FillOutboundMux(outbound, flag, flag);
				outbound.settings.servers = null;
				break;
			}
			case EConfigType.Shadowsocks:
			{
				ServersItem4Ray serversItem4Ray2;
				if (outbound.settings.servers.Count <= 0)
				{
					serversItem4Ray2 = new ServersItem4Ray();
					outbound.settings.servers.Add(serversItem4Ray2);
				}
				else
				{
					serversItem4Ray2 = outbound.settings.servers.First();
				}
				serversItem4Ray2.address = _node.Address;
				serversItem4Ray2.port = _node.Port;
				serversItem4Ray2.password = _node.Password;
				serversItem4Ray2.method = (AppManager.Instance.GetShadowsocksSecurities(_node).Contains(protocolExtra.SsMethod) ? protocolExtra.SsMethod : "none");
				serversItem4Ray2.uot = ((protocolExtra.Uot == true) ? new bool?(true) : ((bool?)null));
				serversItem4Ray2.ota = false;
				serversItem4Ray2.level = 1;
				FillOutboundMux(outbound);
				outbound.settings.vnext = null;
				break;
			}
			case EConfigType.SOCKS:
			case EConfigType.HTTP:
			{
				ServersItem4Ray serversItem4Ray3;
				if (outbound.settings.servers.Count <= 0)
				{
					serversItem4Ray3 = new ServersItem4Ray();
					outbound.settings.servers.Add(serversItem4Ray3);
				}
				else
				{
					serversItem4Ray3 = outbound.settings.servers.First();
				}
				serversItem4Ray3.address = _node.Address;
				serversItem4Ray3.port = _node.Port;
				serversItem4Ray3.method = null;
				serversItem4Ray3.password = null;
				if (_node.Username.IsNotEmpty() && _node.Password.IsNotEmpty())
				{
					SocksUsersItem4Ray item = new SocksUsersItem4Ray
					{
						user = (_node.Username ?? ""),
						pass = _node.Password,
						level = 1
					};
					serversItem4Ray3.users = new List<SocksUsersItem4Ray> { item };
				}
				FillOutboundMux(outbound);
				outbound.settings.vnext = null;
				break;
			}
			case EConfigType.VLESS:
			{
				List<VnextItem4Ray>? vnext = outbound.settings.vnext;
				VnextItem4Ray vnextItem4Ray2;
				if (vnext != null && vnext.Count <= 0)
				{
					vnextItem4Ray2 = new VnextItem4Ray();
					outbound.settings.vnext.Add(vnextItem4Ray2);
				}
				else
				{
					vnextItem4Ray2 = outbound.settings.vnext.First();
				}
				vnextItem4Ray2.address = _node.Address;
				vnextItem4Ray2.port = _node.Port;
				UsersItem4Ray usersItem4Ray2;
				if (vnextItem4Ray2.users.Count <= 0)
				{
					usersItem4Ray2 = new UsersItem4Ray();
					vnextItem4Ray2.users.Add(usersItem4Ray2);
				}
				else
				{
					usersItem4Ray2 = vnextItem4Ray2.users.First();
				}
				usersItem4Ray2.id = _node.Password;
				usersItem4Ray2.email = "t@t.tt";
				usersItem4Ray2.encryption = protocolExtra.VlessEncryption;
				if (protocolExtra.Flow.IsNullOrEmpty())
				{
					FillOutboundMux(outbound, flag, flag);
				}
				else
				{
					usersItem4Ray2.flow = protocolExtra.Flow;
					FillOutboundMux(outbound, enabledTCP: false, flag);
				}
				outbound.settings.servers = null;
				break;
			}
			case EConfigType.Trojan:
			{
				ServersItem4Ray serversItem4Ray;
				if (outbound.settings.servers.Count <= 0)
				{
					serversItem4Ray = new ServersItem4Ray();
					outbound.settings.servers.Add(serversItem4Ray);
				}
				else
				{
					serversItem4Ray = outbound.settings.servers.First();
				}
				serversItem4Ray.address = _node.Address;
				serversItem4Ray.port = _node.Port;
				serversItem4Ray.password = _node.Password;
				serversItem4Ray.ota = false;
				serversItem4Ray.level = 1;
				FillOutboundMux(outbound);
				outbound.settings.vnext = null;
				break;
			}
			case EConfigType.Hysteria2:
				outbound.settings = new Outboundsettings4Ray
				{
					version = 2,
					address = _node.Address,
					port = _node.Port
				};
				outbound.settings.vnext = null;
				outbound.settings.servers = null;
				break;
			case EConfigType.WireGuard:
			{
				string text = _node.Address;
				if (Utils.IsIpv6(text))
				{
					text = "[" + text + "]";
				}
				WireguardPeer4Ray wireguardPeer4Ray = new WireguardPeer4Ray
				{
					publicKey = (protocolExtra.WgPublicKey ?? ""),
					endpoint = text + ":" + _node.Port,
					preSharedKey = protocolExtra.WgPresharedKey
				};
				Outboundsettings4Ray outboundsettings4Ray = new Outboundsettings4Ray();
				List<string> list = Utils.String2List(protocolExtra.WgInterfaceAddress)?.Select((string s) => s.Trim()).ToList();
				int num;
				if (list == null)
				{
					num = 1;
					list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = "172.16.0.2/32";
				}
				outboundsettings4Ray.address = list;
				outboundsettings4Ray.secretKey = _node.Password;
				outboundsettings4Ray.reserved = Utils.String2List(protocolExtra.WgReserved)?.Select((string s) => s.Trim()).Select(int.Parse).ToList();
				outboundsettings4Ray.mtu = ((protocolExtra.WgMtu > 0) ? protocolExtra.WgMtu : new int?(Global.TunMtus.First()));
				num = 1;
				List<WireguardPeer4Ray> list2 = new List<WireguardPeer4Ray>(num);
				CollectionsMarshal.SetCount(list2, num);
				CollectionsMarshal.AsSpan(list2)[0] = wireguardPeer4Ray;
				outboundsettings4Ray.peers = list2;
				Outboundsettings4Ray settings = outboundsettings4Ray;
				outbound.settings = settings;
				outbound.settings.vnext = null;
				outbound.settings.servers = null;
				break;
			}
			}
			outbound.protocol = Global.ProtocolTypes[_node.ConfigType];
			if (_node.ConfigType == EConfigType.Hysteria2)
			{
				outbound.protocol = "hysteria";
			}
			FillBoundStreamSettings(outbound);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillOutboundMux(Outbounds4Ray outbound, bool enabledTCP = false, bool enabledUDP = false)
	{
		try
		{
			outbound.mux.enabled = false;
			outbound.mux.concurrency = -1;
			if (enabledTCP)
			{
				outbound.mux.enabled = true;
				outbound.mux.concurrency = _config.Mux4RayItem.Concurrency;
			}
			else if (enabledUDP)
			{
				outbound.mux.enabled = true;
				outbound.mux.xudpConcurrency = _config.Mux4RayItem.XudpConcurrency;
				outbound.mux.xudpProxyUDP443 = _config.Mux4RayItem.XudpProxyUDP443;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void FillBoundStreamSettings(Outbounds4Ray outbound)
	{
		try
		{
			StreamSettings4Ray streamSettings = outbound.streamSettings;
			if (StxTcpFastOpen.Enabled)
			{
				StreamSettings4Ray streamSettings4Ray = streamSettings;
				if (streamSettings4Ray.sockopt == null)
				{
					Sockopt4Ray sockopt4Ray = (streamSettings4Ray.sockopt = new Sockopt4Ray());
				}
				streamSettings.sockopt.tcpFastOpen = true;
			}
			string text = _node.GetNetwork();
			if (_node.ConfigType == EConfigType.Hysteria2)
			{
				text = "hysteria";
			}
			streamSettings.network = text;
			TransportExtraItem transportExtra = _node.GetTransportExtra();
			string text2 = string.Empty;
			string text3 = string.Empty;
			string text4 = string.Empty;
			int mtu = 0;
			string text5 = string.Empty;
			string text6 = string.Empty;
			switch (text)
			{
			case "raw":
				text2 = transportExtra.Host?.TrimEx() ?? string.Empty;
				text3 = transportExtra.Path?.TrimEx() ?? string.Empty;
				text5 = transportExtra.RawHeaderType?.TrimEx() ?? string.Empty;
				break;
			case "kcp":
				text4 = transportExtra.KcpSeed?.TrimEx() ?? string.Empty;
				text5 = transportExtra.KcpHeaderType?.TrimEx() ?? string.Empty;
				mtu = ((transportExtra.KcpMtu > 0) ? transportExtra.KcpMtu.Value : _config.KcpItem.Mtu);
				break;
			case "ws":
				text2 = transportExtra.Host?.TrimEx() ?? string.Empty;
				text3 = transportExtra.Path?.TrimEx() ?? string.Empty;
				break;
			case "httpupgrade":
				text2 = transportExtra.Host?.TrimEx() ?? string.Empty;
				text3 = transportExtra.Path?.TrimEx() ?? string.Empty;
				break;
			case "xhttp":
				text2 = transportExtra.Host?.TrimEx() ?? string.Empty;
				text3 = transportExtra.Path?.TrimEx() ?? string.Empty;
				text5 = transportExtra.XhttpMode?.TrimEx() ?? string.Empty;
				text6 = transportExtra.XhttpExtra?.TrimEx() ?? string.Empty;
				break;
			case "grpc":
				text2 = transportExtra.GrpcAuthority?.TrimEx() ?? string.Empty;
				text3 = transportExtra.GrpcServiceName?.TrimEx() ?? string.Empty;
				text5 = transportExtra.GrpcMode?.TrimEx() ?? string.Empty;
				break;
			}
			string text7 = _node.Sni.TrimEx();
			string text8 = _config.CoreBasicItem.DefUserAgent ?? string.Empty;
			if (_node.StreamSecurity == "tls")
			{
				streamSettings.security = _node.StreamSecurity;
				bool flag = Utils.ToBool(_node.AllowInsecure);
				bool flag2 = _node.AllowInsecure.IsNullOrEmpty();
				bool num = flag || (flag2 && StxInsecureDefault.Enabled);
				TlsSettings4Ray tlsSettings4Ray = new TlsSettings4Ray
				{
					alpn = _node.GetAlpn(),
					fingerprint = (_node.Fingerprint.IsNullOrEmpty() ? _config.CoreBasicItem.DefFingerprint : _node.Fingerprint),
					echConfigList = _node.EchConfigList.NullIfEmpty()
				};
				if (text7.IsNotEmpty())
				{
					tlsSettings4Ray.serverName = text7;
				}
				else if (text2.IsNotEmpty())
				{
					tlsSettings4Ray.serverName = Utils.String2List(text2)?.First();
				}
				if (num)
				{
					tlsSettings4Ray.allowInsecure = true;
				}
				if (!tlsSettings4Ray.echConfigList.IsNullOrEmpty())
				{
					tlsSettings4Ray.echForceQuery = "full";
				}
				List<string> list = CertPemManager.ParsePemChain(_node.Cert);
				if (list.Count > 0)
				{
					List<CertificateSettings4Ray> list2 = new List<CertificateSettings4Ray>();
					foreach (string item in list)
					{
						List<string> certificate = item.Split("\n").ToList();
						list2.Add(new CertificateSettings4Ray
						{
							certificate = certificate,
							usage = "verify"
						});
					}
					tlsSettings4Ray.certificates = list2;
					tlsSettings4Ray.disableSystemRoot = true;
					tlsSettings4Ray.allowInsecure = false;
				}
				else if (!_node.CertSha.IsNullOrEmpty())
				{
					tlsSettings4Ray.pinnedPeerCertSha256 = _node.CertSha;
					tlsSettings4Ray.allowInsecure = false;
				}
				streamSettings.tlsSettings = tlsSettings4Ray;
			}
			if (_node.StreamSecurity == "reality")
			{
				streamSettings.security = _node.StreamSecurity;
				TlsSettings4Ray realitySettings = new TlsSettings4Ray
				{
					fingerprint = (_node.Fingerprint.IsNullOrEmpty() ? _config.CoreBasicItem.DefFingerprint : _node.Fingerprint),
					serverName = text7,
					publicKey = _node.PublicKey,
					shortId = _node.ShortId,
					spiderX = _node.SpiderX,
					mldsa65Verify = _node.Mldsa65Verify,
					show = false
				};
				streamSettings.realitySettings = realitySettings;
			}
			ProtocolExtraItem protocolExtra;
			string text9;
			int value5;
			int? num3;
			int? num2;
			int value6;
			int? num4;
			string interval;
			Finalmask4Ray finalmask4Ray;
			QuicParams4Ray quicParams4Ray;
			switch (text)
			{
			case "kcp":
			{
				KcpSettings4Ray kcpSettings4Ray = new KcpSettings4Ray
				{
					mtu = mtu,
					tti = _config.KcpItem.Tti
				};
				kcpSettings4Ray.uplinkCapacity = _config.KcpItem.UplinkCapacity;
				kcpSettings4Ray.downlinkCapacity = _config.KcpItem.DownlinkCapacity;
				kcpSettings4Ray.cwndMultiplier = _config.KcpItem.CwndMultiplier;
				kcpSettings4Ray.maxSendingWindow = _config.KcpItem.MaxSendingWindow;
				Finalmask4Ray finalmask4Ray2 = new Finalmask4Ray();
				if (Global.KcpHeaderMaskMap.TryGetValue(text5, out string value7))
				{
					int num5 = 1;
					List<Mask4Ray> list4 = new List<Mask4Ray>(num5);
					CollectionsMarshal.SetCount(list4, num5);
					CollectionsMarshal.AsSpan(list4)[0] = new Mask4Ray
					{
						type = value7,
						settings = null
					};
					finalmask4Ray2.udp = list4;
				}
				Finalmask4Ray finalmask4Ray3 = finalmask4Ray2;
				if (finalmask4Ray3.udp == null)
				{
					List<Mask4Ray> list5 = (finalmask4Ray3.udp = new List<Mask4Ray>());
				}
				if (text4.IsNullOrEmpty())
				{
					finalmask4Ray2.udp.Add(new Mask4Ray
					{
						type = "mkcp-original"
					});
				}
				else
				{
					finalmask4Ray2.udp.Add(new Mask4Ray
					{
						type = "mkcp-aes128gcm",
						settings = new MaskSettings4Ray
						{
							password = text4
						}
					});
				}
				streamSettings.kcpSettings = kcpSettings4Ray;
				streamSettings.finalmask = finalmask4Ray2;
				break;
			}
			case "ws":
			{
				WsSettings4Ray wsSettings4Ray = new WsSettings4Ray();
				if (text2.IsNotEmpty())
				{
					wsSettings4Ray.host = text2;
				}
				if (text3.IsNotEmpty())
				{
					wsSettings4Ray.path = text3;
				}
				if (text8.IsNotEmpty())
				{
					WsSettings4Ray wsSettings4Ray2 = wsSettings4Ray;
					if (wsSettings4Ray2.headers == null)
					{
						Headers4Ray headers4Ray = (wsSettings4Ray2.headers = new Headers4Ray());
					}
					wsSettings4Ray.headers.UserAgent = text8;
				}
				streamSettings.wsSettings = wsSettings4Ray;
				break;
			}
			case "httpupgrade":
			{
				HttpupgradeSettings4Ray httpupgradeSettings4Ray = new HttpupgradeSettings4Ray();
				if (text2.IsNotEmpty())
				{
					httpupgradeSettings4Ray.host = text2;
				}
				if (text3.IsNotEmpty())
				{
					httpupgradeSettings4Ray.path = text3;
				}
				if (text8.IsNotEmpty())
				{
					HttpupgradeSettings4Ray httpupgradeSettings4Ray2 = httpupgradeSettings4Ray;
					if (httpupgradeSettings4Ray2.headers == null)
					{
						Headers4Ray headers4Ray = (httpupgradeSettings4Ray2.headers = new Headers4Ray());
					}
					httpupgradeSettings4Ray.headers.UserAgent = text8;
				}
				streamSettings.httpupgradeSettings = httpupgradeSettings4Ray;
				break;
			}
			case "xhttp":
			{
				streamSettings.network = ETransport.xhttp.ToString();
				XhttpSettings4Ray xhttpSettings4Ray = new XhttpSettings4Ray();
				if (text3.IsNotEmpty())
				{
					xhttpSettings4Ray.path = text3;
				}
				if (text2.IsNotEmpty())
				{
					xhttpSettings4Ray.host = text2;
				}
				if (text5.IsNotEmpty() && Global.XhttpMode.Contains(text5))
				{
					xhttpSettings4Ray.mode = text5;
				}
				if (text6.IsNotEmpty())
				{
					xhttpSettings4Ray.extra = JsonUtils.ParseJson(text6);
				}
				streamSettings.xhttpSettings = xhttpSettings4Ray;
				FillOutboundMux(outbound);
				break;
			}
			case "grpc":
			{
				GrpcSettings4Ray grpcSettings = new GrpcSettings4Ray
				{
					authority = text2.NullIfEmpty(),
					serviceName = text3,
					multiMode = (text5 == "multi"),
					idle_timeout = _config.GrpcItem.IdleTimeout,
					health_check_timeout = _config.GrpcItem.HealthCheckTimeout,
					permit_without_stream = _config.GrpcItem.PermitWithoutStream,
					initial_windows_size = _config.GrpcItem.InitialWindowsSize,
					user_agent = text8.NullIfEmpty()
				};
				streamSettings.grpcSettings = grpcSettings;
				break;
			}
			case "hysteria":
				protocolExtra = _node.GetProtocolExtra();
				text9 = protocolExtra?.Ports;
				num2 = protocolExtra?.UpMbps;
				if (num2.HasValue)
				{
					int valueOrDefault2 = num2.GetValueOrDefault();
					if (valueOrDefault2 >= 0)
					{
						value5 = valueOrDefault2;
						goto IL_0a2a;
					}
				}
				value5 = _config.HysteriaItem.UpMbps;
				goto IL_0a2a;
			default:
				{
					if (text5 == "http")
					{
						RawSettings4Ray rawSettings4Ray = new RawSettings4Ray
						{
							header = new Header4Ray
							{
								type = text5
							}
						};
						string embedText = EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleHttpRequest");
						string valueOrDefault = Global.RawHttpUserAgentTexts.GetValueOrDefault(text8, text8);
						string[] value = text2.Split(',');
						string value2 = string.Join(",".AppendQuotes(), value);
						embedText = embedText.Replace("$requestHost$", value2.AppendQuotes() ?? "");
						embedText = embedText.Replace("$requestUserAgent$", valueOrDefault.AppendQuotes() ?? "");
						string value3 = "/";
						if (text3.IsNotEmpty())
						{
							string[] value4 = text3.Split(',');
							value3 = string.Join(",".AppendQuotes(), value4);
						}
						embedText = embedText.Replace("$requestPath$", value3.AppendQuotes() ?? "");
						rawSettings4Ray.header.request = JsonUtils.Deserialize<object>(embedText);
						streamSettings.rawSettings = rawSettings4Ray;
					}
					break;
				}
				IL_0a2a:
				num3 = value5;
				num2 = protocolExtra?.DownMbps;
				if (num2.HasValue)
				{
					int valueOrDefault3 = num2.GetValueOrDefault();
					if (valueOrDefault3 >= 0)
					{
						value6 = valueOrDefault3;
						goto IL_0a75;
					}
				}
				value6 = _config.HysteriaItem.DownMbps;
				goto IL_0a75;
				IL_0a75:
				num4 = value6;
				interval = ((!protocolExtra.HopInterval.IsNullOrEmpty()) ? protocolExtra.HopInterval : ((_config.HysteriaItem.HopInterval >= 5) ? _config.HysteriaItem.HopInterval : 30).ToString());
				finalmask4Ray = new Finalmask4Ray();
				quicParams4Ray = new QuicParams4Ray();
				if (!text9.IsNullOrEmpty() && (text9.Contains(':') || text9.Contains('-') || text9.Contains(',')))
				{
					UdpHop4Ray udpHop = new UdpHop4Ray
					{
						ports = text9.Replace(':', '-'),
						interval = interval
					};
					quicParams4Ray.udpHop = udpHop;
				}
				if (num3 > 0 || num4 > 0)
				{
					quicParams4Ray.congestion = "brutal";
					quicParams4Ray.brutalUp = ((num3 > 0) ? $"{num3}mbps" : null);
					quicParams4Ray.brutalDown = ((num4 > 0) ? $"{num4}mbps" : null);
				}
				else
				{
					quicParams4Ray.congestion = "bbr";
				}
				finalmask4Ray.quicParams = quicParams4Ray;
				if (!protocolExtra.SalamanderPass.IsNullOrEmpty())
				{
					int num5 = 1;
					List<Mask4Ray> list3 = new List<Mask4Ray>(num5);
					CollectionsMarshal.SetCount(list3, num5);
					CollectionsMarshal.AsSpan(list3)[0] = new Mask4Ray
					{
						type = "salamander",
						settings = new MaskSettings4Ray
						{
							password = protocolExtra.SalamanderPass.TrimEx()
						}
					};
					finalmask4Ray.udp = list3;
				}
				streamSettings.hysteriaSettings = new HysteriaSettings4Ray
				{
					version = 2,
					auth = _node.Password
				};
				streamSettings.finalmask = finalmask4Ray;
				break;
			}
			if (!_node.Finalmask.IsNullOrEmpty())
			{
				streamSettings.finalmask = JsonUtils.ParseJson(_node.Finalmask);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private List<Outbounds4Ray> BuildOutboundsList(string baseTagName = "proxy")
	{
		List<ProfileItem> list = new List<ProfileItem>();
		foreach (string item in Utils.String2List(_node.GetProtocolExtra().ChildItems) ?? new List<string>())
		{
			if (_context.AllProxiesMap.TryGetValue(item, out ProfileItem value))
			{
				list.Add(value);
			}
		}
		List<Outbounds4Ray> list2 = new List<Outbounds4Ray>();
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
					List<Outbounds4Ray> collection = new CoreConfigV2rayService(_context with
					{
						Node = profileItem
					}).BuildGroupProxyOutbounds(text);
					list2.AddRange(collection);
				}
				else
				{
					Outbounds4Ray outbounds4Ray = new CoreConfigV2rayService(_context with
					{
						Node = profileItem
					}).BuildProxyOutbound();
					outbounds4Ray.tag = text;
					list2.Add(outbounds4Ray);
				}
			}
			return list2;
		}
	}

	private List<Outbounds4Ray> BuildChainOutboundsList(string baseTagName = "proxy")
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
		List<Outbounds4Ray> list3 = new List<Outbounds4Ray>();
		checked
		{
			for (int i = 0; i < list2.Count; i++)
			{
				ProfileItem profileItem = list2[i];
				string currentTag = ((i == 0) ? baseTagName : $"chain-{baseTagName}-{i}-{profileItem.Remarks}");
				string text = ((i != list2.Count - 1) ? $"chain-{baseTagName}-{i + 1}-{list2[i + 1].Remarks}" : null);
				if (profileItem.ConfigType.IsGroupType())
				{
					List<Outbounds4Ray> list4 = new CoreConfigV2rayService(_context with
					{
						Node = profileItem
					}).BuildGroupProxyOutbounds(currentTag);
					if (!text.IsNullOrEmpty())
					{
						foreach (Outbounds4Ray item2 in list4.Where((Outbounds4Ray n) => n?.streamSettings?.sockopt?.dialerProxy?.IsNullOrEmpty() ?? true))
						{
							FillDialerProxy(item2, text);
						}
					}
					if (i != 0)
					{
						List<Outbounds4Ray> list5 = list4.Where((Outbounds4Ray n) => n.tag.StartsWith(currentTag)).ToList();
						if (list5.Count == 1)
						{
							string tag = list5.First().tag;
							foreach (Outbounds4Ray item3 in list3.Where((Outbounds4Ray n) => n.streamSettings?.sockopt?.dialerProxy == currentTag))
							{
								FillDialerProxy(item3, tag);
							}
						}
						else if (list5.Count > 1)
						{
							List<Outbounds4Ray> obj = JsonUtils.DeepCopy(list3);
							list3.Clear();
							int num = 0;
							foreach (Outbounds4Ray item4 in list5)
							{
								List<Outbounds4Ray> list6 = JsonUtils.DeepCopy(obj);
								foreach (Outbounds4Ray item5 in list6)
								{
									string tag2 = $"{item5.tag}-clone-{num + 1}";
									item5.tag = tag2;
								}
								for (int num2 = 0; num2 < list6.Count; num2++)
								{
									Outbounds4Ray outbounds4Ray = list6[num2];
									string text2 = outbounds4Ray.streamSettings?.sockopt?.dialerProxy;
									string text3 = ((num2 + 1 < list6.Count) ? list6[num2 + 1].tag : item4.tag);
									FillDialerProxy(outbounds4Ray, (text2 == currentTag) ? item4.tag : text3);
									list3.Add(outbounds4Ray);
								}
								num++;
							}
						}
					}
					list3.AddRange(list4);
				}
				else
				{
					Outbounds4Ray outbounds4Ray2 = new CoreConfigV2rayService(_context with
					{
						Node = profileItem
					}).BuildProxyOutbound();
					outbounds4Ray2.tag = currentTag;
					if (!text.IsNullOrEmpty())
					{
						FillDialerProxy(outbounds4Ray2, text);
					}
					list3.Add(outbounds4Ray2);
				}
			}
			return list3;
		}
	}

	private static void FillDialerProxy(Outbounds4Ray outbound, string dialerProxyTag)
	{
		StreamSettings4Ray streamSettings4Ray;
		if (outbound.streamSettings == null)
		{
			streamSettings4Ray = (outbound.streamSettings = new StreamSettings4Ray());
		}
		streamSettings4Ray = outbound.streamSettings;
		if (streamSettings4Ray.sockopt == null)
		{
			Sockopt4Ray sockopt4Ray = (streamSettings4Ray.sockopt = new Sockopt4Ray());
		}
		outbound.streamSettings.sockopt.dialerProxy = dialerProxyTag;
		if (outbound?.streamSettings?.xhttpSettings?.extra != null && JsonUtils.ParseJson(JsonUtils.Serialize(outbound.streamSettings.xhttpSettings.extra)) is JsonObject jsonObject && jsonObject["downloadSettings"] is JsonObject jsonObject2)
		{
			JsonObject jsonObject3 = (jsonObject2["sockopt"] as JsonObject) ?? new JsonObject();
			jsonObject3["dialerProxy"] = dialerProxyTag;
			jsonObject2["sockopt"] = jsonObject3;
			outbound.streamSettings.xhttpSettings.extra = jsonObject;
		}
	}

	private static Outbounds4Ray BuildDnsOutbound()
	{
		return new Outbounds4Ray
		{
			tag = "dns",
			protocol = "dns"
		};
	}

	private void ApplyOutboundFragment()
	{
		List<Outbounds4Ray> list = _coreConfig.outbounds.Where((Outbounds4Ray n) =>
		{
			StreamSettings4Ray streamSettings = n.streamSettings;
			return streamSettings != null && !streamSettings.security.IsNullOrEmpty() && (n.streamSettings?.sockopt?.dialerProxy?.IsNullOrEmpty() ?? true);
		}).ToList();
		string packets = _config.Fragment4RayItem?.Packets ?? "tlshello";
		string length = _config.Fragment4RayItem?.Length ?? "50-100";
		string delay = _config.Fragment4RayItem?.Interval ?? "10-20";
		Mask4Ray obj = new Mask4Ray
		{
			type = "fragment",
			settings = new MaskSettings4Ray
			{
				packets = packets,
				length = length,
				delay = delay
			}
		};
		Mask4Ray obj2 = new Mask4Ray
		{
			type = "noise",
			settings = new MaskSettings4Ray
			{
				length = "10-20",
				delay = "10-16"
			}
		};
		foreach (Outbounds4Ray item in list)
		{
			JsonObject jsonObject = (JsonUtils.ParseJson(JsonUtils.Serialize(item.streamSettings?.finalmask)) as JsonObject) ?? new JsonObject();
			JsonArray jsonArray = (jsonObject["tcp"] as JsonArray) ?? new JsonArray();
			if (jsonArray.Count == 0)
			{
				jsonArray.Add(JsonUtils.SerializeToNode(obj));
				jsonObject["tcp"] = jsonArray;
			}
			JsonArray jsonArray2 = (jsonObject["udp"] as JsonArray) ?? new JsonArray();
			if (jsonArray2.Count == 0)
			{
				jsonArray2.Add(JsonUtils.SerializeToNode(obj2));
				jsonObject["udp"] = jsonArray2;
			}
			item.streamSettings.finalmask = jsonObject;
		}
	}

	private void GenRouting()
	{
		try
		{
			if (_coreConfig.routing?.rules == null)
			{
				return;
			}

			// Localhost and loopback bypass for local development tools (Antigravity IDE, debuggers, local services)
			_coreConfig.routing.rules.Add(new RulesItem4Ray
			{
				type = "field",
				ip = new List<string> { "127.0.0.0/8", "::1/128", "geoip:private" },
				outboundTag = "direct"
			});
			_coreConfig.routing.rules.Add(new RulesItem4Ray
			{
				type = "field",
				domain = new List<string> { "localhost", "regexp:.*\\.local$", "regexp:.*\\.localhost$" },
				outboundTag = "direct"
			});

			if (_context.IsTunEnabled)
			{
				List<RulesItem4Ray> list = JsonUtils.Deserialize<List<RulesItem4Ray>>(EmbedUtils.GetEmbedText("ServiceLib.Sample.SampleTunRules"));
				if (list != null)
				{
					_coreConfig.routing.rules.AddRange(list);
				}
				var (process, process2) = BuildRoutingDirectExe();
				_coreConfig.routing.rules.Add(new RulesItem4Ray
				{
					port = "53",
					process = process,
					outboundTag = "dns"
				});
				_coreConfig.routing.rules.Add(new RulesItem4Ray
				{
					process = process2,
					outboundTag = "direct"
				});
				List<RulesItem4Ray> rules = _coreConfig.routing.rules;
				RulesItem4Ray rulesItem4Ray = new RulesItem4Ray();
				int num = 1;
				List<string> list2 = new List<string>(num);
				CollectionsMarshal.SetCount(list2, num);
				CollectionsMarshal.AsSpan(list2)[0] = "tun";
				rulesItem4Ray.inboundTag = list2;
				rulesItem4Ray.port = "53";
				rulesItem4Ray.outboundTag = "dns";
				rules.Add(rulesItem4Ray);
			}
			if (_coreConfig.routing?.rules == null)
			{
				return;
			}
			_coreConfig.routing.domainStrategy = _config.RoutingBasicItem.DomainStrategy;
			if (StxAdBlock.Enabled)
			{
				List<RulesItem4Ray> rules2 = _coreConfig.routing.rules;
				RulesItem4Ray rulesItem4Ray2 = new RulesItem4Ray
				{
					type = "field"
				};
				int num = 1;
				List<string> list3 = new List<string>(num);
				CollectionsMarshal.SetCount(list3, num);
				CollectionsMarshal.AsSpan(list3)[0] = "geosite:category-ads-all";
				rulesItem4Ray2.domain = list3;
				rulesItem4Ray2.outboundTag = "block";
				rules2.Add(rulesItem4Ray2);
			}
			RoutingItem routingItem = _context.RoutingItem;
			if (routingItem != null)
			{
				if (routingItem.DomainStrategy.IsNotEmpty())
				{
					_coreConfig.routing.domainStrategy = routingItem.DomainStrategy;
				}
				foreach (RulesItem item in JsonUtils.Deserialize<List<RulesItem>>(routingItem.RuleSet))
				{
					if (item.Enabled && item.RuleType != ERuleType.DNS)
					{
						RulesItem4Ray userRule = JsonUtils.Deserialize<RulesItem4Ray>(JsonUtils.Serialize(item));
						GenRoutingUserRule(userRule);
					}
				}
			}
			List<string> balancerTagList = _coreConfig.routing.balancers?.Select((BalancersItem4Ray p) => p.tag).ToList() ?? new List<string>();
			if (balancerTagList.Count <= 0)
			{
				return;
			}
			foreach (RulesItem4Ray item2 in _coreConfig.routing.rules.Where((RulesItem4Ray r) => balancerTagList.Contains(r.outboundTag + "-balancer")))
			{
				item2.balancerTag = item2.outboundTag + "-balancer";
				item2.outboundTag = null;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private void GenRoutingUserRule(RulesItem4Ray? userRule)
	{
		checked
		{
			try
			{
				if (userRule == null)
				{
					return;
				}
				userRule.outboundTag = GenRoutingUserRuleOutbound(userRule.outboundTag ?? "proxy");
				if (userRule.port.IsNullOrEmpty())
				{
					userRule.port = null;
				}
				if (userRule.network.IsNullOrEmpty())
				{
					userRule.network = null;
				}
				List<string>? domain = userRule.domain;
				if (domain != null && domain.Count == 0)
				{
					userRule.domain = null;
				}
				List<string>? ip = userRule.ip;
				if (ip != null && ip.Count == 0)
				{
					userRule.ip = null;
				}
				List<string>? protocol = userRule.protocol;
				if (protocol != null && protocol.Count == 0)
				{
					userRule.protocol = null;
				}
				List<string>? inboundTag = userRule.inboundTag;
				if (inboundTag != null && inboundTag.Count == 0)
				{
					userRule.inboundTag = null;
				}
				List<string>? process = userRule.process;
				if (process != null && process.Count == 0)
				{
					userRule.process = null;
				}
				bool flag = false;
				List<string>? domain2 = userRule.domain;
				if (domain2 != null && domain2.Count > 0)
				{
					RulesItem4Ray rulesItem4Ray = JsonUtils.DeepCopy(userRule);
					rulesItem4Ray.ip = null;
					rulesItem4Ray.process = null;
					rulesItem4Ray.type = "field";
					for (int num = rulesItem4Ray.domain.Count - 1; num >= 0; num--)
					{
						if (rulesItem4Ray.domain[num].StartsWith('#'))
						{
							rulesItem4Ray.domain.RemoveAt(num);
						}
						rulesItem4Ray.domain[num] = rulesItem4Ray.domain[num].Replace("<COMMA>", ",");
					}
					_coreConfig.routing.rules.Add(rulesItem4Ray);
					flag = true;
				}
				List<string>? ip2 = userRule.ip;
				if (ip2 != null && ip2.Count > 0)
				{
					RulesItem4Ray rulesItem4Ray2 = JsonUtils.DeepCopy(userRule);
					rulesItem4Ray2.domain = null;
					rulesItem4Ray2.process = null;
					rulesItem4Ray2.type = "field";
					_coreConfig.routing.rules.Add(rulesItem4Ray2);
					flag = true;
				}
				List<string>? process2 = userRule.process;
				if (process2 != null && process2.Count > 0)
				{
					RulesItem4Ray rulesItem4Ray3 = JsonUtils.DeepCopy(userRule);
					rulesItem4Ray3.domain = null;
					rulesItem4Ray3.ip = null;
					rulesItem4Ray3.type = "field";
					_coreConfig.routing.rules.Add(rulesItem4Ray3);
					flag = true;
				}
				if (flag)
				{
					return;
				}
				if (!userRule.port.IsNotEmpty())
				{
					List<string>? protocol2 = userRule.protocol;
					if (protocol2 == null || protocol2.Count <= 0)
					{
						List<string>? inboundTag2 = userRule.inboundTag;
						if ((inboundTag2 == null || inboundTag2.Count <= 0) && userRule.network == null)
						{
							return;
						}
					}
				}
				RulesItem4Ray rulesItem4Ray4 = JsonUtils.DeepCopy(userRule);
				rulesItem4Ray4.type = "field";
				_coreConfig.routing.rules.Add(rulesItem4Ray4);
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
			}
		}
	}

	private string GenRoutingUserRuleOutbound(string outboundTag)
	{
		if (Global.OutboundTags.Contains(outboundTag))
		{
			return outboundTag;
		}
		ProfileItem valueOrDefault = _context.AllProxiesMap.GetValueOrDefault("remark:" + outboundTag);
		if (valueOrDefault == null || (!Global.XraySupportConfigType.Contains(valueOrDefault.ConfigType) && !valueOrDefault.ConfigType.IsGroupType()))
		{
			return "proxy";
		}
		string tag = $"{valueOrDefault.IndexId}-{"proxy"}-{valueOrDefault.Remarks}";
		if (_coreConfig.outbounds.Any((Outbounds4Ray p) => p.tag.StartsWith(tag)))
		{
			return tag;
		}
		List<Outbounds4Ray> list = new CoreConfigV2rayService(_context with
		{
			Node = valueOrDefault
		}).BuildAllProxyOutbounds(tag);
		_coreConfig.outbounds.AddRange(list);
		if (Enumerable.Count(list, (Outbounds4Ray n) => n.tag.StartsWith(tag)) > 1)
		{
			EMultipleLoad valueOrDefault2 = valueOrDefault.GetProtocolExtra().MultipleLoad.GetValueOrDefault();
			GenObservatory(valueOrDefault2, tag);
			GenBalancer(valueOrDefault2, tag);
		}
		return tag;
	}

	private RulesItem4Ray BuildFinalRule()
	{
		RulesItem4Ray rulesItem4Ray = new RulesItem4Ray
		{
			type = "field",
			network = "tcp,udp",
			outboundTag = "proxy"
		};
		V2rayConfig coreConfig = _coreConfig;
		object obj;
		if (coreConfig == null)
		{
			obj = null;
		}
		else
		{
			Routing4Ray routing = coreConfig.routing;
			if (routing == null)
			{
				obj = null;
			}
			else
			{
				List<BalancersItem4Ray>? balancers = routing.balancers;
				obj = ((balancers != null) ? balancers.FirstOrDefault((BalancersItem4Ray b) => b.tag == "proxy-balancer", null) : null);
			}
		}
		BalancersItem4Ray balancersItem4Ray = (BalancersItem4Ray)obj;
		string obj2 = _coreConfig.routing?.domainStrategy ?? "AsIs";
		if (balancersItem4Ray != null)
		{
			rulesItem4Ray.outboundTag = null;
			rulesItem4Ray.balancerTag = balancersItem4Ray.tag;
		}
		if (obj2 == "IPIfNonMatch")
		{
			rulesItem4Ray.network = null;
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			span[0] = "0.0.0.0/0";
			span[1] = "::/0";
			rulesItem4Ray.ip = list;
		}
		return rulesItem4Ray;
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
				if (item3.CoreType != ECoreType.Xray)
				{
					hashSet.Add(Utils.GetExeName(coreEx));
				}
				hashSet2.Add(Utils.GetExeName(coreEx));
			}
		}
		hashSet2.Add("xray/");
		hashSet2.Add("self/");
		List<string> item = new List<string>(hashSet);
		List<string> item2 = new List<string>(hashSet2);
		return (lstDnsExe: item, lstDirectExe: item2);
	}

	private void GenStatistic()
	{
		if (_config.GuiItem.EnableStatistics || _config.GuiItem.DisplayRealTimeSpeed)
		{
			string tag = EInboundProtocol.api.ToString();
			Metrics4Ray metrics4Ray = new Metrics4Ray();
			Policy4Ray policy4Ray = new Policy4Ray();
			SystemPolicy4Ray systemPolicy4Ray = new SystemPolicy4Ray();
			_coreConfig.stats = new Stats4Ray();
			metrics4Ray.tag = tag;
			metrics4Ray.listen = $"127.0.0.1:{AppManager.Instance.StatePort}";
			_coreConfig.metrics = metrics4Ray;
			systemPolicy4Ray.statsOutboundDownlink = true;
			systemPolicy4Ray.statsOutboundUplink = true;
			policy4Ray.system = systemPolicy4Ray;
			_coreConfig.policy = policy4Ray;
			if (!_coreConfig.inbounds.Exists((Inbounds4Ray inbounds4Ray2) => inbounds4Ray2.tag == tag))
			{
				Inbounds4Ray inbounds4Ray = new Inbounds4Ray();
				Inboundsettings4Ray inboundsettings4Ray = new Inboundsettings4Ray();
				inbounds4Ray.tag = tag;
				inbounds4Ray.listen = "127.0.0.1";
				inbounds4Ray.port = AppManager.Instance.StatePort;
				inbounds4Ray.protocol = "dokodemo-door";
				inboundsettings4Ray.address = "127.0.0.1";
				inbounds4Ray.settings = inboundsettings4Ray;
				_coreConfig.inbounds.Add(inbounds4Ray);
			}
			if (!_coreConfig.routing.rules.Exists((RulesItem4Ray rulesItem4Ray) => rulesItem4Ray.outboundTag == tag))
			{
				RulesItem4Ray item = new RulesItem4Ray
				{
					inboundTag = new List<string> { tag },
					outboundTag = tag,
					type = "field"
				};
				_coreConfig.routing.rules.Add(item);
			}
		}
	}
}

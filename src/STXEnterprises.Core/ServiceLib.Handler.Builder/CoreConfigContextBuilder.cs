using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Builder;

public class CoreConfigContextBuilder
{
	public static async Task<CoreConfigContextBuilderResult> Build(Config config, ProfileItem node)
	{
		ECoreType coreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
		ECoreType coreType2 = ((coreType == ECoreType.sing_box) ? ECoreType.sing_box : ECoreType.Xray);
		CoreConfigContext coreConfigContext = new CoreConfigContext
		{
			Node = node,
			RunCoreType = coreType,
			AllProxiesMap = new Dictionary<string, ProfileItem>(),
			AppConfig = config
		};
		CoreConfigContext coreConfigContext2 = coreConfigContext;
		coreConfigContext2.FullConfigTemplate = await AppManager.Instance.GetFullConfigTemplateItem(coreType2);
		coreConfigContext.IsTunEnabled = config.TunModeItem.EnableTun;
		coreConfigContext.SimpleDnsItem = config.SimpleDNSItem;
		coreConfigContext.ProtectDomainList = new HashSet<string>();
		CoreConfigContext coreConfigContext3 = coreConfigContext;
		coreConfigContext3.RawDnsItem = await AppManager.Instance.GetDNSItem(coreType2);
		CoreConfigContext coreConfigContext4 = coreConfigContext;
		coreConfigContext4.RoutingItem = await ConfigHandler.GetDefaultRouting(config);
		coreConfigContext.IsWindows = Utils.IsWindows();
		coreConfigContext.IsMacOS = Utils.IsMacOS();
		CoreConfigContext context = coreConfigContext;
		NodeValidatorResult validatorResult = NodeValidatorResult.Empty();
		var (node2, nodeValidatorResult) = await ResolveNodeAsync(context, node);
		if (!nodeValidatorResult.Success)
		{
			return new CoreConfigContextBuilderResult(context, nodeValidatorResult);
		}
		context = context with
		{
			Node = node2
		};
		validatorResult.Warnings.AddRange(nodeValidatorResult.Warnings);
		if (!(context.RoutingItem?.RuleSet.IsNullOrEmpty() ?? true))
		{
			List<RulesItem> source = JsonUtils.Deserialize<List<RulesItem>>(context.RoutingItem?.RuleSet) ?? new List<RulesItem>();
			foreach (RulesItem ruleItem in source.Where((RulesItem rulesItem) => rulesItem.Enabled && !Global.OutboundTags.Contains(rulesItem.OutboundTag)))
			{
				if (ruleItem.OutboundTag.IsNullOrEmpty())
				{
					validatorResult.Warnings.Add(string.Format(ResUI.MsgRoutingRuleEmptyOutboundTag, ruleItem.Remarks));
					ruleItem.OutboundTag = "proxy";
					continue;
				}
				ProfileItem profileItem = await AppManager.Instance.GetProfileItemViaRemarks(ruleItem.OutboundTag);
				if (profileItem == null)
				{
					validatorResult.Warnings.Add(string.Format(ResUI.MsgRoutingRuleOutboundNodeNotFound, ruleItem.Remarks, ruleItem.OutboundTag));
					ruleItem.OutboundTag = "proxy";
					continue;
				}
				var (value, nodeValidatorResult2) = await ResolveNodeAsync(context, profileItem, includeSubChain: false);
				validatorResult.Warnings.AddRange(nodeValidatorResult2.Warnings.Select((string w) => string.Format(ResUI.MsgRoutingRuleOutboundNodeWarning, ruleItem.Remarks, ruleItem.OutboundTag, w)));
				if (!nodeValidatorResult2.Success)
				{
					validatorResult.Warnings.AddRange(nodeValidatorResult2.Errors.Select((string e) => string.Format(ResUI.MsgRoutingRuleOutboundNodeError, ruleItem.Remarks, ruleItem.OutboundTag, e)));
					ruleItem.OutboundTag = "proxy";
				}
				else
				{
					context.AllProxiesMap["remark:" + ruleItem.OutboundTag] = value;
				}
			}
		}
		return new CoreConfigContextBuilderResult(context, validatorResult);
	}

	public static async Task<CoreConfigContextBuilderAllResult> BuildAll(Config config, ProfileItem node)
	{
		CoreConfigContextBuilderResult mainResult = await Build(config, node);
		if (!mainResult.Success)
		{
			return new CoreConfigContextBuilderAllResult(mainResult, null);
		}
		CoreConfigContextBuilderResult coreConfigContextBuilderResult = await BuildPreSocksIfNeeded(mainResult.Context);
		if ((object)coreConfigContextBuilderResult == null)
		{
			return new CoreConfigContextBuilderAllResult(mainResult, null);
		}
		CoreConfigContextBuilderResult coreConfigContextBuilderResult2 = mainResult with { };
		CoreConfigContextBuilderResult coreConfigContextBuilderResult3 = coreConfigContextBuilderResult2;
		CoreConfigContext coreConfigContext = mainResult.Context with
		{
			IsTunEnabled = false
		};
		CoreConfigContext coreConfigContext2 = coreConfigContext;
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string protectDomain in mainResult.Context.ProtectDomainList)
		{
			hashSet.Add(protectDomain);
		}
		foreach (string protectDomain2 in coreConfigContextBuilderResult.Context.ProtectDomainList)
		{
			hashSet.Add(protectDomain2);
		}
		coreConfigContext2.ProtectDomainList = hashSet;
		coreConfigContextBuilderResult3.Context = coreConfigContext;
		return new CoreConfigContextBuilderAllResult(coreConfigContextBuilderResult2, coreConfigContextBuilderResult);
	}

	private static async Task<CoreConfigContextBuilderResult?> BuildPreSocksIfNeeded(CoreConfigContext nodeContext)
	{
		Config appConfig = nodeContext.AppConfig;
		ProfileItem node = nodeContext.Node;
		ECoreType coreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
		ProfileItem preSocksItem = ConfigHandler.GetPreSocksItem(appConfig, node, coreType);
		if (preSocksItem != null)
		{
			CoreConfigContextBuilderResult coreConfigContextBuilderResult = await Build(nodeContext.AppConfig, preSocksItem);
			CoreConfigContextBuilderResult coreConfigContextBuilderResult2 = coreConfigContextBuilderResult with { };
			CoreConfigContextBuilderResult coreConfigContextBuilderResult3 = coreConfigContextBuilderResult2;
			CoreConfigContext coreConfigContext = coreConfigContextBuilderResult.Context with { };
			CoreConfigContext coreConfigContext2 = coreConfigContext;
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string item in nodeContext.ProtectDomainList ?? new HashSet<string>())
			{
				hashSet.Add(item);
			}
			foreach (string item2 in coreConfigContextBuilderResult.Context.ProtectDomainList ?? new HashSet<string>())
			{
				hashSet.Add(item2);
			}
			coreConfigContext2.ProtectDomainList = hashSet;
			coreConfigContextBuilderResult3.Context = coreConfigContext;
			return coreConfigContextBuilderResult2;
		}
		return null;
	}

	public static async Task<(ProfileItem, NodeValidatorResult)> ResolveNodeAsync(CoreConfigContext context, ProfileItem node, bool includeSubChain = true)
	{
		if (node.IndexId.IsNullOrEmpty())
		{
			return (node, NodeValidatorResult.Empty());
		}
		if (includeSubChain)
		{
			var (profileItem, chainValidatorResult) = await BuildSubscriptionChainNodeAsync(node);
			if (profileItem != null)
			{
				context.AllProxiesMap[profileItem.IndexId] = profileItem;
				var (item, nodeValidatorResult) = await ResolveNodeAsync(context, profileItem, includeSubChain: false);
				nodeValidatorResult.Warnings.InsertRange(0, chainValidatorResult.Warnings);
				return (item, nodeValidatorResult);
			}
			if (chainValidatorResult.Warnings.Count > 0)
			{
				NodeValidatorResult nodeValidatorResult2 = await RegisterNodeAsync(context, node);
				nodeValidatorResult2.Warnings.InsertRange(0, chainValidatorResult.Warnings);
				return (node, nodeValidatorResult2);
			}
		}
		return (node, await RegisterNodeAsync(context, node));
	}

	private static async Task<(ProfileItem? ChainNode, NodeValidatorResult ValidatorResult)> BuildSubscriptionChainNodeAsync(ProfileItem node)
	{
		NodeValidatorResult result = NodeValidatorResult.Empty();
		if (node.Subid.IsNullOrEmpty() || node.ConfigType == EConfigType.Custom)
		{
			return (ChainNode: null, ValidatorResult: result);
		}
		SubItem subItem = await AppManager.Instance.GetSubItem(node.Subid);
		if (subItem == null)
		{
			return (ChainNode: null, ValidatorResult: result);
		}
		ProfileItem prevNode = null;
		ProfileItem nextNode = null;
		if (!subItem.PrevProfile.IsNullOrEmpty())
		{
			prevNode = await AppManager.Instance.GetProfileItemViaRemarks(subItem.PrevProfile);
			if (prevNode == null)
			{
				result.Warnings.Add(string.Format(ResUI.MsgSubscriptionPrevProfileNotFound, subItem.PrevProfile));
			}
		}
		if (!subItem.NextProfile.IsNullOrEmpty())
		{
			nextNode = await AppManager.Instance.GetProfileItemViaRemarks(subItem.NextProfile);
			if (nextNode == null)
			{
				result.Warnings.Add(string.Format(ResUI.MsgSubscriptionNextProfileNotFound, subItem.NextProfile));
			}
		}
		if (prevNode == null && nextNode == null)
		{
			return (ChainNode: null, ValidatorResult: result);
		}
		ProfileItem profileItem = new ProfileItem
		{
			IndexId = "inner-" + Utils.GetGuid(full: false),
			ConfigType = EConfigType.ProxyChain,
			CoreType = AppManager.Instance.GetCoreType(node, node.ConfigType),
			Remarks = node.Remarks
		};
		List<string> source = new List<string>(3)
		{
			prevNode?.IndexId,
			node.IndexId,
			nextNode?.IndexId
		};
		ProtocolExtraItem protocolExtra = profileItem.GetProtocolExtra() with
		{
			GroupType = profileItem.ConfigType.ToString(),
			ChildItems = string.Join(",", source.Where((string x) => !x.IsNullOrEmpty()))
		};
		profileItem.SetProtocolExtra(protocolExtra);
		return (ChainNode: profileItem, ValidatorResult: result);
	}

	private static async Task<NodeValidatorResult> RegisterNodeAsync(CoreConfigContext context, ProfileItem node)
	{
		if (node.ConfigType.IsGroupType())
		{
			return await RegisterGroupNodeAsync(context, node);
		}
		return RegisterSingleNodeAsync(context, node);
	}

	private static NodeValidatorResult RegisterSingleNodeAsync(CoreConfigContext context, ProfileItem node)
	{
		if (node.ConfigType.IsGroupType())
		{
			return NodeValidatorResult.Empty();
		}
		NodeValidatorResult nodeValidatorResult = NodeValidator.Validate(node, context.RunCoreType);
		List<string> errors = nodeValidatorResult.Errors;
		List<string> warnings = nodeValidatorResult.Warnings;
		int num = 0;
		string[] array = new string[errors.Count + warnings.Count];
		Span<string> span = CollectionsMarshal.AsSpan(errors);
		span.CopyTo(new Span<string>(array).Slice(num, span.Length));
		num += span.Length;
		Span<string> span2 = CollectionsMarshal.AsSpan(warnings);
		span2.CopyTo(new Span<string>(array).Slice(num, span2.Length));
		num += span2.Length;
		List<string> list = new List<string>(new _003C_003Ez__ReadOnlyArray<string>(array));
		if (list.Count > 0)
		{
			Logging.SaveLog(node.Remarks + ": " + string.Join("; ", list));
		}
		if (!nodeValidatorResult.Success)
		{
			return nodeValidatorResult;
		}
		context.AllProxiesMap[node.IndexId] = node;
		string address = node.Address;
		if (Utils.IsDomain(address))
		{
			context.ProtectDomainList.Add(address);
		}
		if (!node.EchConfigList.IsNullOrEmpty())
		{
			string text = node.Sni;
			if (node.StreamSecurity == "tls")
			{
				string echConfigList = node.EchConfigList;
				if (echConfigList != null && echConfigList.Contains("://"))
				{
					int num2 = node.EchConfigList.IndexOf('+');
					text = ((num2 > 0) ? node.EchConfigList.Substring(0, num2) : node.Sni);
				}
			}
			if (Utils.IsDomain(text))
			{
				context.ProtectDomainList.Add(text);
			}
		}
		string xhttpExtra = node.GetTransportExtra().XhttpExtra;
		if (!string.IsNullOrEmpty(xhttpExtra) && JsonUtils.ParseJson(xhttpExtra) is JsonObject jsonObject && jsonObject.TryGetPropertyValue("downloadSettings", out JsonNode jsonNode) && jsonNode is JsonObject jsonObject2 && jsonObject2.TryGetPropertyValue("address", out JsonNode jsonNode2) && jsonNode2 is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && !string.IsNullOrEmpty(value) && Utils.IsDomain(value))
		{
			context.ProtectDomainList.Add(value);
		}
		return nodeValidatorResult;
	}

	private static async Task<NodeValidatorResult> RegisterGroupNodeAsync(CoreConfigContext context, ProfileItem node)
	{
		if (!node.ConfigType.IsGroupType())
		{
			return NodeValidatorResult.Empty();
		}
		HashSet<string> ancestorsGroup = new HashSet<string> { node.IndexId };
		HashSet<string> globalVisitedGroup = new HashSet<string> { node.IndexId };
		return await TraverseGroupNodeAsync(context, node, globalVisitedGroup, ancestorsGroup);
	}

	private static async Task<NodeValidatorResult> TraverseGroupNodeAsync(CoreConfigContext context, ProfileItem node, HashSet<string> globalVisitedGroup, HashSet<string> ancestorsGroup)
	{
		List<ProfileItem> item = (await GroupProfileManager.GetChildProfileItems(node)).Item1;
		List<string> childIndexIdList = new List<string>();
		NodeValidatorResult childNodeValidatorResult = NodeValidatorResult.Empty();
		foreach (ProfileItem childNode in item)
		{
			if (ancestorsGroup.Contains(childNode.IndexId))
			{
				childNodeValidatorResult.Errors.Add(string.Format(ResUI.MsgGroupCycleDependency, node.Remarks, childNode.Remarks));
				continue;
			}
			if (globalVisitedGroup.Contains(childNode.IndexId))
			{
				childIndexIdList.Add(childNode.IndexId);
				continue;
			}
			if (!childNode.ConfigType.IsGroupType())
			{
				NodeValidatorResult nodeValidatorResult = RegisterSingleNodeAsync(context, childNode);
				childNodeValidatorResult.Warnings.AddRange(nodeValidatorResult.Warnings.Select((string w) => string.Format(ResUI.MsgGroupChildNodeWarning, node.Remarks, childNode.Remarks, w)));
				childNodeValidatorResult.Errors.AddRange(nodeValidatorResult.Errors.Select((string e) => string.Format(ResUI.MsgGroupChildNodeError, node.Remarks, childNode.Remarks, e)));
				if (nodeValidatorResult.Success)
				{
					globalVisitedGroup.Add(childNode.IndexId);
					childIndexIdList.Add(childNode.IndexId);
				}
				continue;
			}
			HashSet<string> ancestorsGroup2 = new HashSet<string>(ancestorsGroup) { childNode.IndexId };
			NodeValidatorResult nodeValidatorResult2 = await TraverseGroupNodeAsync(context, childNode, globalVisitedGroup, ancestorsGroup2);
			childNodeValidatorResult.Warnings.AddRange(nodeValidatorResult2.Warnings.Select((string w) => string.Format(ResUI.MsgGroupChildGroupNodeWarning, node.Remarks, childNode.Remarks, w)));
			childNodeValidatorResult.Errors.AddRange(nodeValidatorResult2.Errors.Select((string e) => string.Format(ResUI.MsgGroupChildGroupNodeError, node.Remarks, childNode.Remarks, e)));
			if (nodeValidatorResult2.Success)
			{
				globalVisitedGroup.Add(childNode.IndexId);
				childIndexIdList.Add(childNode.IndexId);
			}
		}
		if (childIndexIdList.Count == 0)
		{
			childNodeValidatorResult.Errors.Add(string.Format(ResUI.MsgGroupNoValidChildNode, node.Remarks));
			return childNodeValidatorResult;
		}
		childNodeValidatorResult.Warnings.AddRange(childNodeValidatorResult.Errors);
		childNodeValidatorResult.Errors.Clear();
		node.SetProtocolExtra(node.GetProtocolExtra() with
		{
			ChildItems = Utils.List2String(childIndexIdList)
		});
		context.AllProxiesMap[node.IndexId] = node;
		return childNodeValidatorResult;
	}
}

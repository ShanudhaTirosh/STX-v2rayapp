using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Helper;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Entities;

namespace ServiceLib.Manager;

public static class StxRoutingRepair
{
	private const string Everything = "global";

	private static readonly string[] Bypassing = new string[2] { "white", "black" };

	public static async Task ApplyAsync(Config config)
	{
		_ = 4;
		try
		{
			List<RoutingItem> items = await AppManager.Instance.RoutingItems();
			if (items == null || items.Count == 0)
			{
				return;
			}
			await RenameAsync(items);
			RoutingItem active = items.FirstOrDefault((RoutingItem t) => t.IsActive);
			if (active == null || !RoutesAroundTheTunnel(active.RuleSet))
			{
				return;
			}
			RoutingItem wanted = items.FirstOrDefault((RoutingItem t) => CarriesEverything(t.RuleSet));
			if (wanted == null)
			{
				wanted = new RoutingItem
				{
					Remarks = "V4-Everything through the tunnel",
					Url = string.Empty,
					Sort = checked(items.Count + 1)
				};
				if (await ConfigHandler.AddBatchRoutingRules(wanted, Seed("global")) != 0)
				{
					return;
				}
				await AppManager.Instance.RoutingItems();
			}
			await ConfigHandler.SetDefaultRouting(config, wanted);
			Logging.SaveLog("routing: " + active.Remarks + " sent China and Apple outside the tunnel -> " + wanted.Remarks);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("routing repair", ex);
		}
	}

	private static async Task RenameAsync(List<RoutingItem> items)
	{
		Dictionary<string, string> names = new Dictionary<string, string>
		{
			["V4-绕过大陆(Whitelist)"] = "V4-Bypass mainland China",
			["V4-黑名单(Blacklist)"] = "V4-Blocked sites only",
			["V4-全局(Global)"] = "V4-Everything through the tunnel"
		};
		foreach (RoutingItem item in items.Where((RoutingItem t) => names.ContainsKey(t.Remarks ?? string.Empty)))
		{
			item.Remarks = names[item.Remarks];
			await SQLiteHelper.Instance.UpdateAsync(item);
		}
	}

	public static bool RoutesAroundTheTunnel(string? ruleSet)
	{
		return Bypassing.Any((string seed) => IsUntouched(ruleSet, seed));
	}

	public static bool CarriesEverything(string? ruleSet)
	{
		return IsUntouched(ruleSet, "global");
	}

	private static bool IsUntouched(string? ruleSet, string seed)
	{
		string text = Signature(ruleSet);
		if (text != null && text.Length > 0)
		{
			return text == Signature(Seed(seed));
		}
		return false;
	}

	private static string Seed(string name)
	{
		return EmbedUtils.GetEmbedText("ServiceLib.Sample.custom_routing_" + name);
	}

	private static string Signature(string? ruleSet)
	{
		if (ruleSet.IsNullOrEmpty())
		{
			return string.Empty;
		}
		List<RulesItem> list = JsonUtils.Deserialize<List<RulesItem>>(ruleSet);
		if (list == null || list.Count == 0)
		{
			return string.Empty;
		}
		return string.Join("|", list.Select((RulesItem r) =>
		{
			string[] buffer = new string[]
			{
				r.OutboundTag ?? "-",
				r.Port ?? "-",
				r.Network ?? "-",
				Join(r.Ip),
				Join(r.Domain),
				Join(r.Protocol),
				Join(r.Process)
			};
			return string.Join(",", buffer);
		}));
		static string Join(List<string>? values)
		{
			if (values != null)
			{
				return string.Join("+", values);
			}
			return "-";
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Models.Entities;

namespace ServiceLib.Manager;

public class GroupProfileManager
{
	public static async Task<bool> HasCycle(ProfileItem item)
	{
		return await HasCycle(item.IndexId, item.GetProtocolExtra());
	}

	public static async Task<bool> HasCycle(string? indexId, ProtocolExtraItem? extraInfo)
	{
		return await HasCycle(indexId, extraInfo, new HashSet<string>(), new HashSet<string>());
	}

	private static async Task<bool> HasCycle(string? indexId, ProtocolExtraItem? extraInfo, HashSet<string> visited, HashSet<string> stack)
	{
		if (indexId.IsNullOrEmpty() || extraInfo == null)
		{
			return false;
		}
		if (stack.Contains(indexId))
		{
			return true;
		}
		if (visited.Contains(indexId))
		{
			return false;
		}
		visited.Add(indexId);
		stack.Add(indexId);
		try
		{
			if (extraInfo.GroupType.IsNullOrEmpty())
			{
				return false;
			}
			if (extraInfo.ChildItems.IsNullOrEmpty())
			{
				return false;
			}
			List<string> list = Utils.String2List(extraInfo.ChildItems)?.Where((string p) => !string.IsNullOrEmpty(p)).ToList();
			if (list == null)
			{
				return false;
			}
			foreach (ProfileItem item in await AppManager.Instance.GetProfileItemsByIndexIds(list))
			{
				if (await HasCycle(item.IndexId, item?.GetProtocolExtra(), visited, stack))
				{
					return true;
				}
			}
			return false;
		}
		finally
		{
			stack.Remove(indexId);
		}
	}

	public static async Task<(List<ProfileItem> Items, ProtocolExtraItem? Extra)> GetChildProfileItems(ProfileItem profileItem)
	{
		ProtocolExtraItem protocolExtra = profileItem?.GetProtocolExtra();
		return (Items: await GetChildProfileItemsByProtocolExtra(protocolExtra), Extra: protocolExtra);
	}

	public static async Task<List<ProfileItem>> GetChildProfileItemsByProtocolExtra(ProtocolExtraItem? protocolExtra)
	{
		if (protocolExtra == null)
		{
			return new List<ProfileItem>();
		}
		List<ProfileItem> items = new List<ProfileItem>();
		List<ProfileItem> list = items;
		list.AddRange(await GetSubChildProfileItems(protocolExtra));
		list = items;
		list.AddRange(await GetSelectedChildProfileItems(protocolExtra));
		return items;
	}

	private static async Task<List<ProfileItem>> GetSelectedChildProfileItems(ProtocolExtraItem? extra)
	{
		if (extra == null || extra.ChildItems.IsNullOrEmpty())
		{
			return new List<ProfileItem>();
		}
		List<string> list = Utils.String2List(extra.ChildItems)?.Where((string p) => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
		if (list.Count == 0)
		{
			return new List<ProfileItem>();
		}
		return await AppManager.Instance.GetProfileItemsOrderedByIndexIds(list);
	}

	private static async Task<List<ProfileItem>> GetSubChildProfileItems(ProtocolExtraItem? extra)
	{
		if (extra == null || extra.SubChildItems.IsNullOrEmpty())
		{
			return new List<ProfileItem>();
		}
		return (await AppManager.Instance.ProfileItems(extra.SubChildItems ?? string.Empty))?.Where((ProfileItem p) => p != null && p.IsValid() && !p.ConfigType.IsComplexType() && (extra.Filter.IsNullOrEmpty() || Regex.IsMatch(p.Remarks, extra.Filter))).ToList() ?? new List<ProfileItem>();
	}

	public static async Task<Dictionary<string, ProfileItem>> GetAllChildProfileItems(ProfileItem profileItem)
	{
		Dictionary<string, ProfileItem> itemMap = new Dictionary<string, ProfileItem>();
		HashSet<string> visited = new HashSet<string>();
		await CollectChildItems(profileItem, itemMap, visited);
		return itemMap;
	}

	private static async Task CollectChildItems(ProfileItem profileItem, Dictionary<string, ProfileItem> itemMap, HashSet<string> visited)
	{
		List<ProfileItem> item = (await GetChildProfileItems(profileItem)).Item1;
		foreach (ProfileItem item2 in item.Where((ProfileItem child) => visited.Add(child.IndexId)))
		{
			itemMap[item2.IndexId] = item2;
			if (item2.ConfigType.IsGroupType())
			{
				await CollectChildItems(item2, itemMap, visited);
			}
		}
	}
}

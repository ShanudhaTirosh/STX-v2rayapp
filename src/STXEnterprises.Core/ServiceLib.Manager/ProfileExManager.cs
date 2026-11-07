using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Helper;
using ServiceLib.Models.Entities;

namespace ServiceLib.Manager;

public class ProfileExManager
{
	private static readonly Lazy<ProfileExManager> _instance = new Lazy<ProfileExManager>(() => new ProfileExManager());

	private ConcurrentBag<ProfileExItem> _lstProfileEx = new ConcurrentBag<ProfileExItem>();

	private readonly Queue<string> _queIndexIds = new Queue<string>();

	private static readonly string _tag = "ProfileExHandler";

	public static ProfileExManager Instance => _instance.Value;

	public async Task Init()
	{
		await InitData();
	}

	public async Task<ConcurrentBag<ProfileExItem>> GetProfileExs()
	{
		return await Task.FromResult(_lstProfileEx);
	}

	private async Task InitData()
	{
		await SQLiteHelper.Instance.ExecuteAsync("delete from ProfileExItem where indexId not in ( select indexId from ProfileItem )");
		_lstProfileEx = new ConcurrentBag<ProfileExItem>(await SQLiteHelper.Instance.TableAsync<ProfileExItem>().ToListAsync());
	}

	private void IndexIdEnqueue(string indexId)
	{
		if (indexId.IsNotEmpty() && !_queIndexIds.Contains(indexId))
		{
			_queIndexIds.Enqueue(indexId);
		}
	}

	private async Task SaveQueueIndexIds()
	{
		int cnt = _queIndexIds.Count;
		if (cnt <= 0)
		{
			return;
		}
		List<ProfileExItem> source = await SQLiteHelper.Instance.TableAsync<ProfileExItem>().ToListAsync();
		List<ProfileExItem> list = new List<ProfileExItem>();
		List<ProfileExItem> lstUpdates = new List<ProfileExItem>();
		for (int i = 0; i < cnt; i = checked(i + 1))
		{
			string id = _queIndexIds.Dequeue();
			ProfileExItem profileExItem = source.FirstOrDefault((ProfileExItem t) => t.IndexId == id);
			ProfileExItem profileExItem2 = _lstProfileEx?.FirstOrDefault((ProfileExItem t) => t.IndexId == id);
			if (profileExItem2 != null)
			{
				if (profileExItem != null)
				{
					lstUpdates.Add(profileExItem2);
				}
				else
				{
					list.Add(profileExItem2);
				}
			}
		}
		try
		{
			if (list.Count > 0)
			{
				await SQLiteHelper.Instance.InsertAllAsync(list);
			}
			if (lstUpdates.Count > 0)
			{
				await SQLiteHelper.Instance.UpdateAllAsync(lstUpdates);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	private ProfileExItem AddProfileEx(string indexId)
	{
		ProfileExItem profileExItem = new ProfileExItem
		{
			IndexId = indexId,
			Delay = 0,
			Speed = 0m,
			Sort = 0,
			Message = string.Empty
		};
		_lstProfileEx.Add(profileExItem);
		IndexIdEnqueue(indexId);
		return profileExItem;
	}

	private ProfileExItem GetProfileExItem(string? indexId)
	{
		return _lstProfileEx.FirstOrDefault((ProfileExItem t) => t.IndexId == indexId) ?? AddProfileEx(indexId);
	}

	public async Task ClearAll()
	{
		await SQLiteHelper.Instance.ExecuteAsync("delete from ProfileExItem ");
		_lstProfileEx = new ConcurrentBag<ProfileExItem>();
	}

	public async Task SaveTo()
	{
		try
		{
			await SaveQueueIndexIds();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public void SetTestDelay(string indexId, int delay)
	{
		GetProfileExItem(indexId).Delay = delay;
		IndexIdEnqueue(indexId);
	}

	public void SetTestSpeed(string indexId, decimal speed)
	{
		GetProfileExItem(indexId).Speed = speed;
		IndexIdEnqueue(indexId);
	}

	public void SetTestMessage(string indexId, string message)
	{
		GetProfileExItem(indexId).Message = message;
		IndexIdEnqueue(indexId);
	}

	public void SetTestIpInfo(string indexId, string ipInfo)
	{
		GetProfileExItem(indexId).IpInfo = ipInfo;
		IndexIdEnqueue(indexId);
	}

	public void SetSort(string indexId, int sort)
	{
		GetProfileExItem(indexId).Sort = sort;
		IndexIdEnqueue(indexId);
	}

	public int GetSort(string indexId)
	{
		return _lstProfileEx.FirstOrDefault((ProfileExItem t) => t.IndexId == indexId)?.Sort ?? 0;
	}

	public int GetMaxSort()
	{
		if (_lstProfileEx.Count <= 0)
		{
			return 0;
		}
		return _lstProfileEx.Max((ProfileExItem t) => t?.Sort ?? 0);
	}
}

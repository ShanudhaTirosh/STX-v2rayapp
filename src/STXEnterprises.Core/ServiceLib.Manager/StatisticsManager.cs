using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Helper;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Services.Statistics;

namespace ServiceLib.Manager;

public class StatisticsManager
{
	private static readonly Lazy<StatisticsManager> instance = new Lazy<StatisticsManager>(() => new StatisticsManager());

	private Config _config;

	private ServerStatItem? _serverStatItem;

	private List<ServerStatItem> _lstServerStat;

	private Func<ServerSpeedItem, Task>? _updateFunc;

	private StatisticsXrayService? _statisticsXray;

	private static readonly string _tag = "StatisticsHandler";

	public static StatisticsManager Instance => instance.Value;

	public List<ServerStatItem> ServerStat => _lstServerStat;

	public async Task Init(Config config, Func<ServerSpeedItem, Task> updateFunc)
	{
		_config = config;
		_updateFunc = updateFunc;
		if (config.GuiItem.EnableStatistics || _config.GuiItem.DisplayRealTimeSpeed)
		{
			await InitData();
			_statisticsXray = new StatisticsXrayService(config, UpdateServerStatHandler);
		}
	}

	public void Close()
	{
		try
		{
			_statisticsXray?.Close();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public async Task ClearAllServerStatistics()
	{
		await SQLiteHelper.Instance.ExecuteAsync("delete from ServerStatItem ");
		_serverStatItem = null;
		_lstServerStat = new List<ServerStatItem>();
	}

	public async Task SaveTo()
	{
		try
		{
			if (_lstServerStat != null)
			{
				await SQLiteHelper.Instance.UpdateAllAsync(_lstServerStat);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public async Task CloneServerStatItem(string indexId, string toIndexId)
	{
		if (_lstServerStat != null && !(indexId == toIndexId))
		{
			ServerStatItem serverStatItem = _lstServerStat.FirstOrDefault((ServerStatItem t) => t.IndexId == indexId);
			if (serverStatItem != null)
			{
				ServerStatItem toStat = JsonUtils.DeepCopy(serverStatItem);
				toStat.IndexId = toIndexId;
				await SQLiteHelper.Instance.ReplaceAsync(toStat);
				_lstServerStat.Add(toStat);
			}
		}
	}

	private async Task InitData()
	{
		await SQLiteHelper.Instance.ExecuteAsync("delete from ServerStatItem where indexId not in ( select indexId from ProfileItem )");
		long ticks = DateTime.Now.Date.Ticks;
		await SQLiteHelper.Instance.ExecuteAsync($"update ServerStatItem set todayUp = 0,todayDown=0,dateNow={ticks} where dateNow<>{ticks}");
		_lstServerStat = await SQLiteHelper.Instance.TableAsync<ServerStatItem>().ToListAsync();
	}

	private async Task UpdateServerStatHandler(ServerSpeedItem server)
	{
		await UpdateServerStat(server);
	}

	private async Task UpdateServerStat(ServerSpeedItem server)
	{
		await GetServerStatItem(_config.IndexId);
		checked
		{
			if (_serverStatItem != null)
			{
				if (server.ProxyUp != 0L || server.ProxyDown != 0L)
				{
					_serverStatItem.TodayUp += server.ProxyUp;
					_serverStatItem.TodayDown += server.ProxyDown;
					_serverStatItem.TotalUp += server.ProxyUp;
					_serverStatItem.TotalDown += server.ProxyDown;
				}
				server.IndexId = _config.IndexId;
				server.TodayUp = _serverStatItem.TodayUp;
				server.TodayDown = _serverStatItem.TodayDown;
				server.TotalUp = _serverStatItem.TotalUp;
				server.TotalDown = _serverStatItem.TotalDown;
				await (_updateFunc?.Invoke(server));
			}
		}
	}

	private async Task GetServerStatItem(string indexId)
	{
		long ticks = DateTime.Now.Date.Ticks;
		if (_serverStatItem != null && _serverStatItem.IndexId != indexId)
		{
			_serverStatItem = null;
		}
		if (_serverStatItem == null)
		{
			_serverStatItem = _lstServerStat.FirstOrDefault((ServerStatItem t) => t.IndexId == indexId);
			if (_serverStatItem == null)
			{
				_serverStatItem = new ServerStatItem
				{
					IndexId = indexId,
					TotalUp = 0L,
					TotalDown = 0L,
					TodayUp = 0L,
					TodayDown = 0L,
					DateNow = ticks
				};
				await SQLiteHelper.Instance.ReplaceAsync(_serverStatItem);
				_lstServerStat.Add(_serverStatItem);
			}
		}
		if (_serverStatItem.DateNow != ticks)
		{
			_serverStatItem.TodayUp = 0L;
			_serverStatItem.TodayDown = 0L;
			_serverStatItem.DateNow = ticks;
		}
	}
}

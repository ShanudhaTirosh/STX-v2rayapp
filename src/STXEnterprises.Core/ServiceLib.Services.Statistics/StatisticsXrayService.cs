using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Helper;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;

namespace ServiceLib.Services.Statistics;

public class StatisticsXrayService
{
	private const long linkBase = 1024L;

	private ServerSpeedItem _serverSpeedItem = new ServerSpeedItem();

	private readonly Config _config;

	private bool _exitFlag;

	private readonly Func<ServerSpeedItem, Task>? _updateFunc;

	private string Url => $"{"http://"}{"127.0.0.1"}:{AppManager.Instance.StatePort}/debug/vars";

	public StatisticsXrayService(Config config, Func<ServerSpeedItem, Task> updateFunc)
	{
		_config = config;
		_updateFunc = updateFunc;
		_exitFlag = false;
		Task.Run((Func<Task?>)Run);
	}

	public void Close()
	{
		_exitFlag = true;
	}

	private async Task Run()
	{
		while (!_exitFlag)
		{
			await Task.Delay(1000);
			try
			{
				if (AppManager.Instance.RunningCoreType == ECoreType.Xray)
				{
					string text = await HttpClientHelper.Instance.TryGetAsync(Url);
					if (text != null)
					{
						ServerSpeedItem arg = ParseOutput(text) ?? new ServerSpeedItem();
						await (_updateFunc?.Invoke(arg));
					}
				}
			}
			catch
			{
			}
		}
	}

	private ServerSpeedItem? ParseOutput(string result)
	{
		try
		{
			V2rayMetricsVars v2rayMetricsVars = JsonUtils.Deserialize<V2rayMetricsVars>(result);
			if (v2rayMetricsVars?.stats?.outbound == null)
			{
				return null;
			}
			ServerSpeedItem serverSpeedItem = new ServerSpeedItem();
			foreach (string item in v2rayMetricsVars.stats.outbound.Keys.Cast<string>())
			{
				object obj = v2rayMetricsVars.stats.outbound[item];
				if (obj != null)
				{
					V2rayMetricsVarsLink v2rayMetricsVarsLink = JsonUtils.Deserialize<V2rayMetricsVarsLink>(obj.ToString());
					if (item.StartsWith("proxy"))
					{
						checked
						{
							serverSpeedItem.ProxyUp += unchecked(v2rayMetricsVarsLink.uplink / 1024);
							serverSpeedItem.ProxyDown += unchecked(v2rayMetricsVarsLink.downlink / 1024);
						}
					}
					else if (item == "direct")
					{
						serverSpeedItem.DirectUp = v2rayMetricsVarsLink.uplink / 1024;
						serverSpeedItem.DirectDown = v2rayMetricsVarsLink.downlink / 1024;
					}
				}
			}
			if (serverSpeedItem.DirectDown < _serverSpeedItem.DirectDown || serverSpeedItem.ProxyDown < _serverSpeedItem.ProxyDown)
			{
				_serverSpeedItem = new ServerSpeedItem();
				return null;
			}
			ServerSpeedItem result2 = checked(new ServerSpeedItem
			{
				ProxyUp = serverSpeedItem.ProxyUp - _serverSpeedItem.ProxyUp,
				ProxyDown = serverSpeedItem.ProxyDown - _serverSpeedItem.ProxyDown,
				DirectUp = serverSpeedItem.DirectUp - _serverSpeedItem.DirectUp,
				DirectDown = serverSpeedItem.DirectDown - _serverSpeedItem.DirectDown
			});
			_serverSpeedItem = serverSpeedItem;
			return result2;
		}
		catch
		{
		}
		return null;
	}
}

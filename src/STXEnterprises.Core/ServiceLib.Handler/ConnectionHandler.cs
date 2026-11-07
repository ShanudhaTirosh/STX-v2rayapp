using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Dto;
using ServiceLib.Resx;
using ServiceLib.Services;

namespace ServiceLib.Handler;

public static class ConnectionHandler
{
	private static readonly string _tag = "ConnectionHandler";

	public static async Task<string> RunAvailabilityCheck()
	{
		int time = await GetRealPingTimeInfo();
		string text = ((time <= 0) ? "none" : (await GetIPInfo()));
		string arg = text;
		return string.Format(ResUI.TestMeOutput, time, arg);
	}

	private static async Task<string?> GetIPInfo()
	{
		return (await GetIPInfo(await GetWebProxy()))?.ToString() ?? "none";
	}

	private static async Task<int> GetRealPingTimeInfo()
	{
		int responseTime = -1;
		try
		{
			WebProxy webProxy = await GetWebProxy();
			for (int i = 0; i < 2; i = checked(i + 1))
			{
				responseTime = await GetRealPingTime(webProxy, 10);
				if (responseTime > 0)
				{
					break;
				}
				await Task.Delay(500);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return -1;
		}
		return responseTime;
	}

	private static async Task<WebProxy?> GetWebProxy()
	{
		int localPort = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
		return new WebProxy($"socks5://{"127.0.0.1"}:{localPort}");
	}

	public static async Task<int> GetRealPingTime(IWebProxy? webProxy, int downloadTimeout)
	{
		string url = AppManager.Instance.Config.SpeedTestItem.SpeedPingTestUrl;
		int responseTime = -1;
		checked
		{
			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource();
				cts.CancelAfter(TimeSpan.FromSeconds(downloadTimeout));
				using HttpClient client = new HttpClient(new SocketsHttpHandler
				{
					Proxy = webProxy,
					UseProxy = (webProxy != null)
				});
				List<int> oneTime = new List<int>();
				for (int i = 0; i < 2; i++)
				{
					Stopwatch timer = Stopwatch.StartNew();
					await client.GetAsync(url, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
					timer.Stop();
					oneTime.Add((int)timer.Elapsed.TotalMilliseconds);
					await Task.Delay(100);
				}
				responseTime = (from x in oneTime
					where x > 0
					orderby x
					select x).FirstOrDefault();
			}
			catch
			{
			}
			return responseTime;
		}
	}

	public static async Task<IpInfoResult?> GetIPInfo(IWebProxy? webProxy)
	{
		try
		{
			string iPAPIUrl = AppManager.Instance.Config.SpeedTestItem.IPAPIUrl;
			if (iPAPIUrl.IsNullOrEmpty())
			{
				return null;
			}
			string text = await new DownloadService().TryDownloadString(iPAPIUrl, webProxy, "");
			if (text == null)
			{
				return null;
			}
			IPAPIInfo iPAPIInfo = JsonUtils.Deserialize<IPAPIInfo>(text);
			if (iPAPIInfo == null)
			{
				return null;
			}
			string ip = iPAPIInfo.ip ?? iPAPIInfo.clientIp ?? iPAPIInfo.ip_addr ?? iPAPIInfo.query;
			string country = iPAPIInfo.country_code ?? iPAPIInfo.country ?? iPAPIInfo.countryCode ?? iPAPIInfo.location?.country_code ?? "unknown";
			return new IpInfoResult(country, ip);
		}
		catch
		{
			return null;
		}
	}
}

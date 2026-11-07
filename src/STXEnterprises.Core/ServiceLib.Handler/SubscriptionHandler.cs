using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;
using ServiceLib.Services;

namespace ServiceLib.Handler;

public static class SubscriptionHandler
{
	public static async Task UpdateProcess(Config config, string subId, bool blProxy, Func<bool, string, Task> updateFunc)
	{
		await (updateFunc?.Invoke(arg1: false, ResUI.MsgUpdateSubscriptionStart));
		List<SubItem> list = await AppManager.Instance.SubItems();
		if (list == null || list.Count <= 0)
		{
			await (updateFunc?.Invoke(arg1: false, ResUI.MsgNoValidSubscription));
			return;
		}
		int successCount = 0;
		foreach (SubItem item in list)
		{
			try
			{
				if (!IsValidSubscription(item, subId))
				{
					continue;
				}
				string hashCode = item.Remarks + "->";
				if (!item.Enabled)
				{
					await (updateFunc?.Invoke(arg1: false, hashCode + ResUI.MsgSkipSubscriptionUpdate));
					continue;
				}
				DownloadService downloadHandle = CreateDownloadHandler(hashCode, updateFunc);
				await (updateFunc?.Invoke(arg1: false, hashCode + ResUI.MsgStartGettingSubscriptions));
				string result = await DownloadAllSubscriptions(config, item, blProxy, downloadHandle);
				if (await ProcessDownloadResult(config, item.Id, result, hashCode, updateFunc))
				{
					successCount = checked(successCount + 1);
				}
				await (updateFunc?.Invoke(arg1: false, "-------------------------------------------------------"));
			}
			catch (Exception ex)
			{
				string text = item.Remarks + "->";
				Logging.SaveLog("UpdateSubscription", ex);
				await (updateFunc?.Invoke(arg1: false, text + ResUI.MsgFailedImportSubscription + ": " + ex.Message));
				await (updateFunc?.Invoke(arg1: false, "-------------------------------------------------------"));
			}
		}
		await (updateFunc?.Invoke(successCount > 0, ResUI.MsgUpdateSubscriptionEnd ?? ""));
	}

	private static bool IsValidSubscription(SubItem item, string subId)
	{
		string value = item.Id.TrimEx();
		string text = item.Url.TrimEx();
		if (value.IsNullOrEmpty() || text.IsNullOrEmpty())
		{
			return false;
		}
		if (subId.IsNotEmpty() && item.Id != subId)
		{
			return false;
		}
		if (!text.StartsWith("https://") && !text.StartsWith("http://"))
		{
			return false;
		}
		return true;
	}

	private static DownloadService CreateDownloadHandler(string hashCode, Func<bool, string, Task> updateFunc)
	{
		DownloadService downloadService = new DownloadService();
		downloadService.Error += (object sender2, ErrorEventArgs args) =>
		{
			updateFunc?.Invoke(arg1: false, hashCode + args.GetException().Message);
		};
		return downloadService;
	}

	private static async Task<string> DownloadSubscriptionContent(DownloadService downloadHandle, string url, bool blProxy, string userAgent)
	{
		string text = await downloadHandle.TryDownloadString(url, blProxy, userAgent);
		if (blProxy && text.IsNullOrEmpty())
		{
			text = await downloadHandle.TryDownloadString(url, blProxy: false, userAgent);
		}
		return text ?? string.Empty;
	}

	private static async Task<string> DownloadAllSubscriptions(Config config, SubItem item, bool blProxy, DownloadService downloadHandle)
	{
		string text = await DownloadMainSubscription(config, item, blProxy, downloadHandle);
		if (item.ConvertTarget.IsNullOrEmpty() && item.MoreUrl.TrimEx().IsNotEmpty())
		{
			text = await DownloadAdditionalSubscriptions(item, text, blProxy, downloadHandle);
		}
		return text;
	}

	private static async Task<string> DownloadMainSubscription(Config config, SubItem item, bool blProxy, DownloadService downloadHandle)
	{
		string text = Utils.GetPunycode(item.Url.TrimEx());
		if (item.ConvertTarget.IsNotEmpty())
		{
			text = string.Format(config.ConstItem.SubConvertUrl.IsNullOrEmpty() ? Global.SubConvertUrls.FirstOrDefault() : config.ConstItem.SubConvertUrl, Utils.UrlEncode(text));
			if (!text.Contains("target="))
			{
				text += $"&target={item.ConvertTarget}";
			}
			if (!text.Contains("config="))
			{
				text += $"&config={Global.SubConvertConfig.FirstOrDefault()}";
			}
		}
		return await DownloadSubscriptionContent(downloadHandle, text, blProxy, item.UserAgent);
	}

	private static async Task<string> DownloadAdditionalSubscriptions(SubItem item, string mainResult, bool blProxy, DownloadService downloadHandle)
	{
		string result = mainResult;
		if (result.IsNotEmpty() && Utils.IsBase64String(result))
		{
			result = Utils.Base64Decode(result);
		}
		string[] array = item.MoreUrl.TrimEx().Split(",") ?? Array.Empty<string>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string punycode = Utils.GetPunycode(array2[i]);
			if (!punycode.IsNullOrEmpty())
			{
				string text = await DownloadSubscriptionContent(downloadHandle, punycode, blProxy, item.UserAgent);
				if (text.IsNotEmpty())
				{
					result = ((!Utils.IsBase64String(text)) ? (result + Environment.NewLine + text) : (result + Environment.NewLine + Utils.Base64Decode(text)));
				}
			}
		}
		return result;
	}

	private static async Task<bool> ProcessDownloadResult(Config config, string id, string result, string hashCode, Func<bool, string, Task> updateFunc)
	{
		if (result.IsNullOrEmpty())
		{
			await (updateFunc?.Invoke(arg1: false, hashCode + ResUI.MsgSubscriptionDecodingFailed));
			return false;
		}
		await (updateFunc?.Invoke(arg1: false, hashCode + ResUI.MsgGetSubscriptionSuccessfully));
		if (result.Length < 99)
		{
			await (updateFunc?.Invoke(arg1: false, hashCode + result));
		}
		await (updateFunc?.Invoke(arg1: false, hashCode + ResUI.MsgStartParsingSubscription));
		int ret = await ConfigHandler.AddBatchServers(config, result, id, isSub: true);
		if (ret <= 0)
		{
			Logging.SaveLog("FailedImportSubscription");
			Logging.SaveLog(result);
		}
		await (updateFunc?.Invoke(arg1: false, (ret > 0) ? (hashCode + ResUI.MsgUpdateSubscriptionEnd) : (hashCode + ResUI.MsgFailedImportSubscription)));
		return ret > 0;
	}
}

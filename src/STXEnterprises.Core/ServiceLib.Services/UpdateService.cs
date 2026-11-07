using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Services;

public class UpdateService
{
	private readonly Config? _config;

	private readonly Func<bool, string, Task>? _updateFunc;

	private readonly int _timeout;

	private static readonly string _tag = "UpdateService";

	public UpdateService(Config config, Func<bool, string, Task> updateFunc)
	{
		_config = config;
		_updateFunc = updateFunc;
		_timeout = 30;
	}

	public async Task CheckUpdateGuiN(bool preRelease)
	{
		_ = string.Empty;
		string fileName = string.Empty;
		DownloadService downloadHandle = new DownloadService();
		downloadHandle.UpdateCompleted += (object? sender2, UpdateResult args) =>
		{
			if (args.Success)
			{
				UpdateFunc(notify: false, ResUI.MsgDownloadV2rayCoreSuccessfully);
				UpdateFunc(notify: true, Utils.UrlEncode(fileName));
			}
			else
			{
				UpdateFunc(notify: false, args.Msg);
			}
		};
		downloadHandle.Error += (object sender2, ErrorEventArgs args) =>
		{
			UpdateFunc(notify: false, args.GetException().Message);
		};
		await UpdateFunc(notify: false, string.Format(ResUI.MsgStartUpdating, ECoreType.app));
		UpdateResult result = await CheckUpdateAsync(downloadHandle, ECoreType.app, preRelease);
		if (result.Success)
		{
			await UpdateFunc(notify: false, string.Format(ResUI.MsgParsingSuccessfully, ECoreType.app));
			await UpdateFunc(notify: false, result.Msg);
			string url = result.Url.ToString();
			fileName = Utils.GetTempPath(Utils.GetGuid());
			await downloadHandle.DownloadFileAsync(url, fileName, blProxy: true, _timeout);
		}
		else
		{
			await UpdateFunc(notify: false, result.Msg);
		}
	}

	public async Task CheckUpdateCore(ECoreType type, bool preRelease)
	{
		_ = string.Empty;
		string fileName = string.Empty;
		DownloadService downloadHandle = new DownloadService();
		downloadHandle.UpdateCompleted += (object? sender2, UpdateResult args) =>
		{
			if (args.Success)
			{
				UpdateFunc(notify: false, ResUI.MsgDownloadV2rayCoreSuccessfully);
				UpdateFunc(notify: false, ResUI.MsgUnpacking);
				try
				{
					UpdateFunc(notify: true, fileName);
					return;
				}
				catch (Exception ex)
				{
					UpdateFunc(notify: false, ex.Message);
					return;
				}
			}
			UpdateFunc(notify: false, args.Msg);
		};
		downloadHandle.Error += (object sender2, ErrorEventArgs args) =>
		{
			UpdateFunc(notify: false, args.GetException().Message);
		};
		await UpdateFunc(notify: false, string.Format(ResUI.MsgStartUpdating, type));
		UpdateResult result = await CheckUpdateAsync(downloadHandle, type, preRelease);
		if (result.Success)
		{
			await UpdateFunc(notify: false, string.Format(ResUI.MsgParsingSuccessfully, type));
			await UpdateFunc(notify: false, result.Msg);
			string text = result.Url.ToString();
			string text2 = (text.Contains(".tar.gz") ? ".tar.gz" : Path.GetExtension(text));
			fileName = Utils.GetTempPath(Utils.GetGuid() + text2);
			await downloadHandle.DownloadFileAsync(text, fileName, blProxy: true, _timeout);
		}
		else if (!result.Msg.IsNullOrEmpty())
		{
			await UpdateFunc(notify: false, result.Msg);
		}
	}

	public async Task<UpdateResult> CheckHasUpdateOnly(ECoreType type, bool preRelease)
	{
		if (!CoreInfoManager.Instance.IsCheckUpdateSupported(type))
		{
			return new UpdateResult(success: false, ResUI.MsgNotSupport);
		}
		DownloadService downloadHandle = new DownloadService();
		bool checkPreRelease = CoreInfoManager.Instance.GetCheckPreRelease(type, preRelease);
		return await CheckUpdateAsync(downloadHandle, type, checkPreRelease);
	}

	public async Task<List<string>> CheckHasUpdateOnlyAll(bool preRelease)
	{
		List<string> msgs = new List<string>();
		foreach (ECoreType type in CoreInfoManager.Instance.GetCheckUpdateCoreTypes())
		{
			if (_config.CheckUpdateItem.SelectedCoreTypes?.Contains(type.ToString()) ?? true)
			{
				UpdateResult updateResult = await CheckHasUpdateOnly(type, preRelease);
				if (updateResult.Success && updateResult.Version != null)
				{
					string text = string.Format(ResUI.MsgCheckUpdateHasNewVersion, type, updateResult.Version);
					msgs.Add(text);
					AppManager.Instance.SetLastCheckUpdateResult(type, text);
				}
				else
				{
					AppManager.Instance.SetLastCheckUpdateResult(type, updateResult.Msg);
				}
			}
		}
		return msgs;
	}

	public async Task UpdateGeoFileAll()
	{
		await UpdateGeoFiles();
		await UpdateOtherFiles();
		await UpdateSrsFileAll();
		await UpdateFunc(notify: true, string.Format(ResUI.MsgDownloadGeoFileSuccessfully, "geo"));
	}

	private async Task<UpdateResult> CheckUpdateAsync(DownloadService downloadHandle, ECoreType type, bool preRelease)
	{
		try
		{
			UpdateResult updateResult = await GetRemoteVersion(downloadHandle, type, preRelease);
			if (!updateResult.Success || (object)updateResult.Version == null)
			{
				return updateResult;
			}
			return await ParseDownloadUrl(type, updateResult);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			await UpdateFunc(notify: false, ex.Message);
			return new UpdateResult(success: false, ex.Message);
		}
	}

	private async Task<UpdateResult> GetRemoteVersion(DownloadService downloadHandle, ECoreType type, bool preRelease)
	{
		CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(type);
		_ = string.Empty;
		string version;
		if (preRelease)
		{
			string url = coreInfo?.ReleaseApiUrl;
			string text = await downloadHandle.TryDownloadString(url, blProxy: true, "STX Enterprises");
			if (text.IsNullOrEmpty())
			{
				return new UpdateResult(success: false, "");
			}
			List<GitHubRelease> list = JsonUtils.Deserialize<List<GitHubRelease>>(text);
			version = ((!preRelease) ? ((list != null) ? list.First((GitHubRelease r) => !r.Prerelease) : null) : ((list != null) ? list.First() : null))?.TagName;
		}
		else
		{
			string url2 = Path.Combine(coreInfo.Url, "latest");
			string text2 = await downloadHandle.UrlRedirectAsync(url2, blProxy: true);
			if (text2 == null)
			{
				return new UpdateResult(success: false, "");
			}
			version = text2?.Split("/tag/").LastOrDefault();
		}
		return new UpdateResult(success: true, new SemanticVersion(version));
	}

	private async Task<SemanticVersion> GetCoreVersion(ECoreType type)
	{
		try
		{
			CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(type);
			string text = string.Empty;
			foreach (string coreEx in coreInfo.CoreExes)
			{
				string binPath = Utils.GetBinPath(Utils.GetExeName(coreEx), coreInfo.CoreType.ToString());
				if (File.Exists(binPath))
				{
					text = binPath;
					break;
				}
			}
			if (!File.Exists(text))
			{
				string.Format(ResUI.NotFoundCore, "", "", "");
				return new SemanticVersion("");
			}
			string input = (await Utils.GetCliWrapOutput(text, coreInfo.VersionArg)) ?? "";
			string version = string.Empty;
			switch (type)
			{
			case ECoreType.v2fly:
			case ECoreType.Xray:
			case ECoreType.v2fly_v5:
				version = Regex.Match(input, coreInfo.Match + " ([0-9.]+) \\(").Groups[1].Value;
				break;
			case ECoreType.mihomo:
				version = Regex.Match(input, "v[0-9.]+").Groups[0].Value;
				break;
			case ECoreType.sing_box:
				version = Regex.Match(input, "([0-9.]+)").Groups[1].Value;
				break;
			}
			return new SemanticVersion(version);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			await UpdateFunc(notify: false, ex.Message);
			return new SemanticVersion("");
		}
	}

	private async Task<UpdateResult> ParseDownloadUrl(ECoreType type, UpdateResult result)
	{
		try
		{
			SemanticVersion version = result.Version ?? new SemanticVersion(0, 0, 0);
			CoreInfo coreInfo = CoreInfoManager.Instance.GetCoreInfo(type);
			string coreUrl = (await GetUrlFromCore(coreInfo)) ?? string.Empty;
			SemanticVersion semanticVersion;
			string msg;
			string url;
			switch (type)
			{
			case ECoreType.v2fly:
			case ECoreType.Xray:
			case ECoreType.v2fly_v5:
				semanticVersion = await GetCoreVersion(type);
				msg = string.Format(ResUI.IsLatestCore, type, semanticVersion.ToVersionString("v"));
				url = string.Format(coreUrl, version.ToVersionString("v"));
				break;
			case ECoreType.mihomo:
				semanticVersion = await GetCoreVersion(type);
				msg = string.Format(ResUI.IsLatestCore, type, semanticVersion);
				url = string.Format(coreUrl, version.ToVersionString("v"));
				break;
			case ECoreType.sing_box:
				semanticVersion = await GetCoreVersion(type);
				msg = string.Format(ResUI.IsLatestCore, type, semanticVersion.ToVersionString("v"));
				url = string.Format(coreUrl, version.ToVersionString("v"), version);
				break;
			case ECoreType.app:
				semanticVersion = new SemanticVersion(Utils.GetVersionInfo());
				msg = string.Format(ResUI.IsLatestN, type, semanticVersion);
				url = string.Format(coreUrl, version);
				break;
			default:
				throw new ArgumentException("Type");
			}
			if (semanticVersion >= version && version != new SemanticVersion(0, 0, 0))
			{
				return new UpdateResult(success: false, msg);
			}
			result.Url = url;
			return result;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			await UpdateFunc(notify: false, ex.Message);
			return new UpdateResult(success: false, ex.Message);
		}
	}

	private async Task<string?> GetUrlFromCore(CoreInfo? coreInfo)
	{
		if (Utils.IsWindows())
		{
			string text = RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.Arm64 => coreInfo?.DownloadUrlWinArm64, 
				Architecture.X64 => coreInfo?.DownloadUrlWin64, 
				_ => null, 
			};
			if (coreInfo == null || coreInfo.CoreType != ECoreType.app)
			{
				return text;
			}
			if (File.Exists(Path.Combine(Utils.GetBaseDirectory(), "libHarfBuzzSharp.dll")))
			{
				return text?.Replace(".zip", "-desktop.zip");
			}
			return text;
		}
		if (Utils.IsLinux())
		{
			return RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.Arm64 => coreInfo?.DownloadUrlLinuxArm64, 
				Architecture.LoongArch64 => coreInfo?.DownloadUrlLinuxLoong64, 
				Architecture.X64 => coreInfo?.DownloadUrlLinux64, 
				_ => null, 
			};
		}
		if (Utils.IsMacOS())
		{
			return RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.Arm64 => coreInfo?.DownloadUrlOSXArm64, 
				Architecture.X64 => coreInfo?.DownloadUrlOSX64, 
				_ => null, 
			};
		}
		return await Task.FromResult("");
	}

	private async Task UpdateGeoFiles()
	{
		string geoUrl = (string.IsNullOrEmpty(_config?.ConstItem.GeoSourceUrl) ? "https://github.com/Loyalsoldier/v2ray-rules-dat/releases/latest/download/{0}.dat" : _config.ConstItem.GeoSourceUrl);
		List<string> list = new List<string>(2) { "geosite", "geoip" };
		foreach (string item in list)
		{
			string text = item + ".dat";
			string binPath = Utils.GetBinPath(text ?? "");
			string url = string.Format(geoUrl, item);
			await DownloadGeoFile(url, text, binPath);
		}
	}

	private async Task UpdateOtherFiles()
	{
		if (_config.ConstItem.GeoSourceUrl.IsNotEmpty())
		{
			return;
		}
		foreach (string otherGeoUrl in Global.OtherGeoUrls)
		{
			string fileName = Path.GetFileName(otherGeoUrl);
			string binPath = Utils.GetBinPath(fileName ?? "");
			await DownloadGeoFile(otherGeoUrl, fileName, binPath);
		}
	}

	private async Task UpdateSrsFileAll()
	{
		List<string> geoipFiles = new List<string>();
		List<string> geoSiteFiles = new List<string>();
		foreach (RoutingItem item in await AppManager.Instance.RoutingItems())
		{
			foreach (RulesItem item2 in JsonUtils.Deserialize<List<RulesItem>>(item.RuleSet) ?? new List<RulesItem>())
			{
				AddPrefixedItems(item2.Ip, "geoip:", geoipFiles);
				AddPrefixedItems(item2.Domain, "geosite:", geoSiteFiles);
			}
		}
		DNSItem dNSItem = await AppManager.Instance.GetDNSItem(ECoreType.sing_box);
		if (dNSItem != null)
		{
			ExtractDnsRuleSets(dNSItem.NormalDNS, geoipFiles, geoSiteFiles);
			ExtractDnsRuleSets(dNSItem.TunDNS, geoipFiles, geoSiteFiles);
		}
		geoSiteFiles.AddRange(new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "google", "cn", "geolocation-cn", "category-ads-all" }));
		string binPath = Utils.GetBinPath("srss");
		if (!Directory.Exists(binPath))
		{
			Directory.CreateDirectory(binPath);
		}
		foreach (string item3 in geoipFiles.Distinct())
		{
			await UpdateSrsFile("geoip", item3);
		}
		foreach (string item4 in geoSiteFiles.Distinct())
		{
			await UpdateSrsFile("geosite", item4);
		}
	}

	private void AddPrefixedItems(List<string>? items, string prefix, List<string> output)
	{
		if (items == null)
		{
			return;
		}
		foreach (string item in items)
		{
			if (item.StartsWith(prefix))
			{
				output.Add(item.Substring(prefix.Length));
			}
		}
	}

	private void ExtractDnsRuleSets(string? dnsJson, List<string> geoipFiles, List<string> geoSiteFiles)
	{
		if (string.IsNullOrEmpty(dnsJson))
		{
			return;
		}
		try
		{
			Dns4Sbox dns4Sbox = JsonUtils.Deserialize<Dns4Sbox>(dnsJson);
			if (dns4Sbox?.rules == null)
			{
				return;
			}
			foreach (Rule4Sbox rule in dns4Sbox.rules)
			{
				ExtractSrsRuleSets(rule, geoipFiles, geoSiteFiles);
			}
		}
		catch
		{
		}
	}

	private void ExtractSrsRuleSets(Rule4Sbox? rule, List<string> geoipFiles, List<string> geoSiteFiles)
	{
		if (rule == null)
		{
			return;
		}
		AddPrefixedItems(rule.rule_set, "geosite-", geoSiteFiles);
		AddPrefixedItems(rule.rule_set, "geoip-", geoipFiles);
		if (rule.rules == null)
		{
			return;
		}
		foreach (Rule4Sbox rule2 in rule.rules)
		{
			ExtractSrsRuleSets(rule2, geoipFiles, geoSiteFiles);
		}
	}

	private async Task UpdateSrsFile(string type, string srsName)
	{
		string? format = (string.IsNullOrEmpty(_config.ConstItem.SrsSourceUrl) ? "" : _config.ConstItem.SrsSourceUrl);
		string text = type + "-" + srsName + ".srs";
		string targetPath = Path.Combine(Utils.GetBinPath("srss"), text);
		string url = string.Format(format, type, type + "-" + srsName, srsName);
		await DownloadGeoFile(url, text, targetPath);
	}

	private async Task DownloadGeoFile(string url, string fileName, string targetPath)
	{
		string tmpFileName = Utils.GetTempPath(Utils.GetGuid());
		DownloadService downloadService = new DownloadService();
		downloadService.UpdateCompleted += (object? sender2, UpdateResult args) =>
		{
			if (args.Success)
			{
				UpdateFunc(notify: false, string.Format(ResUI.MsgDownloadGeoFileSuccessfully, fileName));
				try
				{
					if (File.Exists(tmpFileName))
					{
						File.Copy(tmpFileName, targetPath, overwrite: true);
						File.Delete(tmpFileName);
					}
					return;
				}
				catch (Exception ex)
				{
					UpdateFunc(notify: false, ex.Message);
					return;
				}
			}
			UpdateFunc(notify: false, args.Msg);
		};
		downloadService.Error += (object sender2, ErrorEventArgs args) =>
		{
			UpdateFunc(notify: false, args.GetException().Message);
		};
		await downloadService.DownloadFileAsync(url, tmpFileName, blProxy: true, _timeout);
	}

	private async Task UpdateFunc(bool notify, string msg)
	{
		await (_updateFunc?.Invoke(notify, msg));
	}
}

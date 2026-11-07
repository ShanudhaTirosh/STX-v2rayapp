using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Handler.Fmt;
using ServiceLib.Helper;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;
using ServiceLib.Services;

namespace ServiceLib.Handler;

public static class ConfigHandler
{
	private static readonly string _configRes = "guiNConfig.json";

	private static readonly string _tag = "ConfigHandler";

	private static readonly Dictionary<string, string> PolicyGroupRegionFilters = new Dictionary<string, string>
	{
		{ "JP", "日本|\\b[Jj][Pp]\\b|\ud83c\uddef\ud83c\uddf5|[Jj]apan" },
		{ "US", "美国|\\b[Uu][Ss]\\b|\ud83c\uddfa\ud83c\uddf8|[Uu]nited [Ss]tates|\\b[Uu][Ss][Aa]\\b" },
		{ "HK", "香港|\\b[Hh][Kk]\\b|\ud83c\udded\ud83c\uddf0|[Hh]ong ?[Kk]ong" },
		{ "TW", "台湾|台灣|\\b[Tt][Ww]\\b|\ud83c\uddf9\ud83c\uddfc|[Tt]aiwan" },
		{ "KR", "韩国|\\b[Kk][Rr]\\b|\ud83c\uddf0\ud83c\uddf7|[Kk]orea" },
		{ "SG", "新加坡|\\b[Ss][Gg]\\b|\ud83c\uddf8\ud83c\uddec|[Ss]ingapore" },
		{ "DE", "德国|\\b[Dd][Ee]\\b|\ud83c\udde9\ud83c\uddea|[Gg]ermany" },
		{ "FR", "法国|\\b[Ff][Rr]\\b|\ud83c\uddeb\ud83c\uddf7|[Ff]rance" },
		{ "GB", "英国|\\b[Gg][Bb]\\b|\ud83c\uddec\ud83c\udde7|[Uu]nited [Kk]ingdom|[Bb]ritain" },
		{ "CA", "加拿大|\ud83c\udde8\ud83c\udde6|[Cc]anada" },
		{ "AU", "澳大利亚|\\b[Aa][Uu]\\b|\ud83c\udde6\ud83c\uddfa|[Aa]ustralia" },
		{ "RU", "俄罗斯|\\b[Rr][Uu]\\b|\ud83c\uddf7\ud83c\uddfa|[Rr]ussia" },
		{ "BR", "巴西|\\b[Bb][Rr]\\b|\ud83c\udde7\ud83c\uddf7|[Bb]razil" },
		{ "IN", "印度|\ud83c\uddee\ud83c\uddf3|[Ii]ndia" },
		{ "VN", "越南|\\b[Vv][Nn]\\b|\ud83c\uddfb\ud83c\uddf3|[Vv]ietnam" },
		{ "ID", "印度尼西亚|\\b[Ii][Dd]\\b|\ud83c\uddee\ud83c\udde9|[Ii]ndonesia" },
		{ "MX", "墨西哥|\\b[Mm][Xx]\\b|\ud83c\uddf2\ud83c\uddfd|[Mm]exico" }
	};

	public static Config? LoadConfig()
	{
		Config config = null;
		string text = EmbedUtils.LoadResource(Utils.GetConfigPath(_configRes));
		if (text.IsNotEmpty())
		{
			config = JsonUtils.Deserialize<Config>(text);
		}
		else if (File.Exists(Utils.GetConfigPath(_configRes)))
		{
			Logging.SaveLog("LoadConfig Exception");
			return null;
		}
		if (config == null)
		{
			config = new Config();
		}
		Config config2 = config;
		if (config2.CoreBasicItem == null)
		{
			Config config3 = config2;
			CoreBasicItem coreBasicItem = new CoreBasicItem
			{
				LogEnabled = false,
				Loglevel = "warning",
				MuxEnabled = false
			};
			CoreBasicItem coreBasicItem2 = coreBasicItem;
			config3.CoreBasicItem = coreBasicItem;
		}
		if (config.Inbound == null)
		{
			config.Inbound = new List<InItem>();
			InItem item = new InItem
			{
				Protocol = EInboundProtocol.socks.ToString(),
				LocalPort = 10808,
				UdpEnabled = true,
				SniffingEnabled = true,
				RouteOnly = false
			};
			config.Inbound.Add(item);
		}
		else if (config.Inbound.Count > 0)
		{
			config.Inbound.First().Protocol = EInboundProtocol.socks.ToString();
		}
		config2 = config;
		if (config2.RoutingBasicItem == null)
		{
			RoutingBasicItem routingBasicItem = (config2.RoutingBasicItem = new RoutingBasicItem());
		}
		if (config.RoutingBasicItem.DomainStrategy.IsNullOrEmpty())
		{
			config.RoutingBasicItem.DomainStrategy = Global.DomainStrategies.First();
		}
		config2 = config;
		if (config2.KcpItem == null)
		{
			Config config4 = config2;
			KcpItem kcpItem = new KcpItem
			{
				Mtu = 1350,
				Tti = 50,
				UplinkCapacity = 12,
				DownlinkCapacity = 100,
				CwndMultiplier = 1,
				MaxSendingWindow = 2097152
			};
			KcpItem kcpItem2 = kcpItem;
			config4.KcpItem = kcpItem;
		}
		config.KcpItem.CwndMultiplier = ((config.KcpItem.CwndMultiplier <= 0) ? 1 : config.KcpItem.CwndMultiplier);
		config.KcpItem.MaxSendingWindow = ((config.KcpItem.MaxSendingWindow <= 0) ? 2097152 : config.KcpItem.MaxSendingWindow);
		config2 = config;
		if (config2.GrpcItem == null)
		{
			Config config5 = config2;
			GrpcItem grpcItem = new GrpcItem
			{
				IdleTimeout = 60,
				HealthCheckTimeout = 20,
				PermitWithoutStream = false,
				InitialWindowsSize = 0
			};
			GrpcItem grpcItem2 = grpcItem;
			config5.GrpcItem = grpcItem;
		}
		config2 = config;
		if (config2.TunModeItem == null)
		{
			Config config6 = config2;
			TunModeItem tunModeItem = new TunModeItem
			{
				EnableTun = false,
				Mtu = 9000,
				IcmpRouting = Global.TunIcmpRoutingPolicies.First(),
				EnableLegacyProtect = false
			};
			TunModeItem tunModeItem2 = tunModeItem;
			config6.TunModeItem = tunModeItem;
		}
		config2 = config;
		if (config2.GuiItem == null)
		{
			GUIItem gUIItem = (config2.GuiItem = new GUIItem());
		}
		config2 = config;
		if (config2.MsgUIItem == null)
		{
			MsgUIItem msgUIItem = (config2.MsgUIItem = new MsgUIItem());
		}
		config2 = config;
		UIItem uIItem;
		if (config2.UiItem == null)
		{
			uIItem = (config2.UiItem = new UIItem());
		}
		uIItem = config.UiItem;
		if (uIItem.MainColumnItem == null)
		{
			List<ColumnItem> list = (uIItem.MainColumnItem = new List<ColumnItem>());
		}
		uIItem = config.UiItem;
		if (uIItem.WindowSizeItem == null)
		{
			List<WindowSizeItem> list3 = (uIItem.WindowSizeItem = new List<WindowSizeItem>());
		}
		if (config.UiItem.CurrentLanguage.IsNullOrEmpty())
		{
			config.UiItem.CurrentLanguage = (Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.CurrentCultureIgnoreCase) ? Global.Languages.First() : Global.Languages[2]);
		}
		config2 = config;
		if (config2.ConstItem == null)
		{
			ConstItem constItem = (config2.ConstItem = new ConstItem());
		}
		config2 = config;
		SimpleDNSItem simpleDNSItem;
		if (config2.SimpleDNSItem == null)
		{
			simpleDNSItem = (config2.SimpleDNSItem = InitBuiltinSimpleDNS());
		}
		simpleDNSItem = config.SimpleDNSItem;
		bool? globalFakeIp = simpleDNSItem.GlobalFakeIp;
		bool valueOrDefault = globalFakeIp == true;
		if (!globalFakeIp.HasValue)
		{
			valueOrDefault = true;
			SimpleDNSItem simpleDNSItem3 = simpleDNSItem;
			bool? globalFakeIp2 = valueOrDefault;
			simpleDNSItem3.GlobalFakeIp = globalFakeIp2;
		}
		simpleDNSItem = config.SimpleDNSItem;
		if (simpleDNSItem.BootstrapDNS == null)
		{
			string text2 = (simpleDNSItem.BootstrapDNS = Global.DomainPureIPDNSAddress.FirstOrDefault());
		}
		simpleDNSItem = config.SimpleDNSItem;
		globalFakeIp = simpleDNSItem.ServeStale;
		valueOrDefault = globalFakeIp == true;
		if (!globalFakeIp.HasValue)
		{
			valueOrDefault = false;
			SimpleDNSItem simpleDNSItem4 = simpleDNSItem;
			bool? globalFakeIp2 = valueOrDefault;
			simpleDNSItem4.ServeStale = globalFakeIp2;
		}
		simpleDNSItem = config.SimpleDNSItem;
		globalFakeIp = simpleDNSItem.ParallelQuery;
		valueOrDefault = globalFakeIp == true;
		if (!globalFakeIp.HasValue)
		{
			valueOrDefault = false;
			SimpleDNSItem simpleDNSItem5 = simpleDNSItem;
			bool? globalFakeIp2 = valueOrDefault;
			simpleDNSItem5.ParallelQuery = globalFakeIp2;
		}
		config2 = config;
		if (config2.SpeedTestItem == null)
		{
			SpeedTestItem speedTestItem = (config2.SpeedTestItem = new SpeedTestItem());
		}
		if (config.SpeedTestItem.SpeedTestTimeout < 10)
		{
			config.SpeedTestItem.SpeedTestTimeout = 10;
		}
		if (config.SpeedTestItem.SpeedTestUrl.IsNullOrEmpty())
		{
			config.SpeedTestItem.SpeedTestUrl = Global.SpeedTestUrls.First();
		}
		if (config.SpeedTestItem.SpeedPingTestUrl.IsNullOrEmpty())
		{
			config.SpeedTestItem.SpeedPingTestUrl = Global.SpeedPingTestUrls.First();
		}
		if (config.SpeedTestItem.MixedConcurrencyCount < 1)
		{
			config.SpeedTestItem.MixedConcurrencyCount = 5;
		}
		if (config.SpeedTestItem.UdpTestTarget.IsNullOrEmpty())
		{
			config.SpeedTestItem.UdpTestTarget = Global.UdpTestTargets.First();
		}
		config2 = config;
		if (config2.Mux4RayItem == null)
		{
			Config config7 = config2;
			Mux4RayItem mux4RayItem = new Mux4RayItem
			{
				Concurrency = 8,
				XudpConcurrency = 16,
				XudpProxyUDP443 = "reject"
			};
			Mux4RayItem mux4RayItem2 = mux4RayItem;
			config7.Mux4RayItem = mux4RayItem;
		}
		config2 = config;
		if (config2.Mux4SboxItem == null)
		{
			Config config8 = config2;
			Mux4SboxItem mux4SboxItem = new Mux4SboxItem
			{
				Protocol = Global.SingboxMuxs.First(),
				MaxConnections = 8
			};
			Mux4SboxItem mux4SboxItem2 = mux4SboxItem;
			config8.Mux4SboxItem = mux4SboxItem;
		}
		config2 = config;
		if (config2.HysteriaItem == null)
		{
			Config config9 = config2;
			HysteriaItem hysteriaItem = new HysteriaItem
			{
				UpMbps = 100,
				DownMbps = 100
			};
			HysteriaItem hysteriaItem2 = hysteriaItem;
			config9.HysteriaItem = hysteriaItem;
		}
		config2 = config;
		ClashUIItem clashUIItem;
		if (config2.ClashUIItem == null)
		{
			clashUIItem = (config2.ClashUIItem = new ClashUIItem());
		}
		clashUIItem = config.ClashUIItem;
		if (clashUIItem.ConnectionsColumnItem == null)
		{
			List<ColumnItem> list = (clashUIItem.ConnectionsColumnItem = new List<ColumnItem>());
		}
		config2 = config;
		if (config2.SystemProxyItem == null)
		{
			SystemProxyItem systemProxyItem = (config2.SystemProxyItem = new SystemProxyItem());
		}
		config2 = config;
		if (config2.WebDavItem == null)
		{
			WebDavItem webDavItem = (config2.WebDavItem = new WebDavItem());
		}
		config2 = config;
		if (config2.CheckUpdateItem == null)
		{
			CheckUpdateItem checkUpdateItem = (config2.CheckUpdateItem = new CheckUpdateItem());
		}
		config2 = config;
		if (config2.Fragment4RayItem == null)
		{
			Config config10 = config2;
			Fragment4RayItem fragment4RayItem = new Fragment4RayItem
			{
				Packets = "tlshello",
				Length = "50-100",
				Interval = "10-20"
			};
			Fragment4RayItem fragment4RayItem2 = fragment4RayItem;
			config10.Fragment4RayItem = fragment4RayItem;
		}
		config2 = config;
		if (config2.GlobalHotkeys == null)
		{
			List<KeyEventItem> list6 = (config2.GlobalHotkeys = new List<KeyEventItem>());
		}
		if (config.SystemProxyItem.SystemProxyExceptions.IsNullOrEmpty())
		{
			config.SystemProxyItem.SystemProxyExceptions = (Utils.IsWindows() ? "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*" : "localhost,127.0.0.0/8,::1");
		}
		return config;
	}

	public static async Task<int> SaveConfig(Config config)
	{
		try
		{
			string resPath = Utils.GetConfigPath(_configRes);
			string tempPath = resPath + "_temp";
			string text = JsonUtils.Serialize(config, indented: true, nullValue: true);
			if (text.IsNullOrEmpty())
			{
				return -1;
			}
			await File.WriteAllTextAsync(tempPath, text);
			File.Move(tempPath, resPath, overwrite: true);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return -1;
		}
		return 0;
	}

	public static async Task<int> AddServer(Config config, ProfileItem profileItem)
	{
		ProfileItem profileItem2 = await AppManager.Instance.GetProfileItem(profileItem.IndexId);
		if (profileItem2 == null)
		{
			profileItem2 = profileItem;
		}
		else
		{
			profileItem2.CoreType = profileItem.CoreType;
			profileItem2.Remarks = profileItem.Remarks;
			profileItem2.Address = profileItem.Address;
			profileItem2.Port = profileItem.Port;
			profileItem2.Username = profileItem.Username;
			profileItem2.Password = profileItem.Password;
			profileItem2.Network = profileItem.Network;
			profileItem2.StreamSecurity = profileItem.StreamSecurity;
			profileItem2.Sni = profileItem.Sni;
			profileItem2.AllowInsecure = profileItem.AllowInsecure;
			profileItem2.Fingerprint = profileItem.Fingerprint;
			profileItem2.Alpn = profileItem.Alpn;
			profileItem2.PublicKey = profileItem.PublicKey;
			profileItem2.ShortId = profileItem.ShortId;
			profileItem2.SpiderX = profileItem.SpiderX;
			profileItem2.Mldsa65Verify = profileItem.Mldsa65Verify;
			profileItem2.MuxEnabled = profileItem.MuxEnabled;
			profileItem2.Cert = profileItem.Cert;
			profileItem2.CertSha = profileItem.CertSha;
			profileItem2.EchConfigList = profileItem.EchConfigList;
			profileItem2.Finalmask = profileItem.Finalmask;
			profileItem2.ProtoExtra = profileItem.ProtoExtra;
			profileItem2.TransportExtra = profileItem.TransportExtra;
		}
		return profileItem2.ConfigType switch
		{
			EConfigType.VMess => await AddVMessServer(config, profileItem2), 
			EConfigType.Shadowsocks => await AddShadowsocksServer(config, profileItem2), 
			EConfigType.SOCKS => await AddSocksServer(config, profileItem2), 
			EConfigType.HTTP => await AddHttpServer(config, profileItem2), 
			EConfigType.Trojan => await AddTrojanServer(config, profileItem2), 
			EConfigType.VLESS => await AddVlessServer(config, profileItem2), 
			EConfigType.Hysteria2 => await AddHysteria2Server(config, profileItem2), 
			EConfigType.TUIC => await AddTuicServer(config, profileItem2), 
			EConfigType.WireGuard => await AddWireguardServer(config, profileItem2), 
			EConfigType.Anytls => await AddAnytlsServer(config, profileItem2), 
			EConfigType.Naive => await AddNaiveServer(config, profileItem2), 
			_ => -1, 
		};
	}

	public static async Task<int> AddVMessServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.VMess;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			VmessSecurity = profileItem.GetProtocolExtra().VmessSecurity?.TrimEx()
		});
		profileItem.Network = profileItem.Network.TrimEx();
		profileItem.StreamSecurity = profileItem.StreamSecurity.TrimEx();
		if (!Global.VmessSecurities.Contains(profileItem.GetProtocolExtra().VmessSecurity))
		{
			return -1;
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> RemoveServers(Config config, List<ProfileItem> indexes)
	{
		string subid = "TempRemoveSubId";
		foreach (ProfileItem index in indexes)
		{
			index.Subid = subid;
		}
		await SQLiteHelper.Instance.UpdateAllAsync(indexes);
		await RemoveServersViaSubid(config, subid, isSub: false);
		return 0;
	}

	public static async Task<int> CopyServer(Config config, List<ProfileItem> indexes)
	{
		foreach (ProfileItem index in indexes)
		{
			ProfileItem profileItem = await AppManager.Instance.GetProfileItem(index.IndexId);
			if (profileItem == null)
			{
				continue;
			}
			ProfileItem profileItem2 = JsonUtils.DeepCopy(profileItem);
			profileItem2.IndexId = string.Empty;
			profileItem2.Remarks = profileItem.Remarks + "-clone";
			if (profileItem2.ConfigType == EConfigType.Custom)
			{
				profileItem2.Address = Utils.GetConfigPath(profileItem2.Address);
				if (await AddCustomServer(config, profileItem2, blDelete: false) != 0)
				{
				}
			}
			else
			{
				await AddServerCommon(config, profileItem2);
			}
		}
		return 0;
	}

	public static async Task<int> SetDefaultServerIndex(Config config, string? indexId)
	{
		if (indexId.IsNullOrEmpty())
		{
			return -1;
		}
		config.IndexId = indexId;
		await SaveConfig(config);
		return 0;
	}

	public static async Task<int> SetDefaultServer(Config config, List<ProfileItemModel> lstProfile)
	{
		if (lstProfile.Exists((ProfileItemModel t) => t.IndexId == config.IndexId))
		{
			return 0;
		}
		if (await SQLiteHelper.Instance.TableAsync<ProfileItem>().FirstOrDefaultAsync((ProfileItem t) => t.IndexId == config.IndexId) != null)
		{
			return 0;
		}
		if (lstProfile.Count > 0)
		{
			return await SetDefaultServerIndex(config, lstProfile.FirstOrDefault((ProfileItemModel t) => t.Port > 0)?.IndexId);
		}
		ProfileItem profileItem = await SQLiteHelper.Instance.TableAsync<ProfileItem>().FirstOrDefaultAsync((ProfileItem t) => t.Port > 0);
		return await SetDefaultServerIndex(config, profileItem?.IndexId);
	}

	public static async Task<ProfileItem?> GetDefaultServer(Config config)
	{
		ProfileItem profileItem = await AppManager.Instance.GetProfileItem(config.IndexId);
		if (profileItem == null)
		{
			ProfileItem item2 = await SQLiteHelper.Instance.TableAsync<ProfileItem>().FirstOrDefaultAsync();
			await SetDefaultServerIndex(config, item2?.IndexId);
			return item2;
		}
		return profileItem;
	}

	public static async Task<int> MoveServer(Config config, List<ProfileItem> lstProfile, int index, EMove eMove, int pos = -1)
	{
		int count = lstProfile.Count;
		checked
		{
			if (index < 0 || index > lstProfile.Count - 1)
			{
				return -1;
			}
			for (int i = 0; i < lstProfile.Count; i++)
			{
				ProfileExManager.Instance.SetSort(lstProfile[i].IndexId, (i + 1) * 10);
			}
			int sort = 0;
			switch (eMove)
			{
			case EMove.Top:
				if (index == 0)
				{
					return 0;
				}
				sort = ProfileExManager.Instance.GetSort(lstProfile.First().IndexId) - 1;
				break;
			case EMove.Up:
				if (index == 0)
				{
					return 0;
				}
				sort = ProfileExManager.Instance.GetSort(lstProfile[index - 1].IndexId) - 1;
				break;
			case EMove.Down:
				if (index == count - 1)
				{
					return 0;
				}
				sort = ProfileExManager.Instance.GetSort(lstProfile[index + 1].IndexId) + 1;
				break;
			case EMove.Bottom:
				if (index == count - 1)
				{
					return 0;
				}
				sort = ProfileExManager.Instance.GetSort(lstProfile[unchecked(lstProfile.Count - 1)].IndexId) + 1;
				break;
			case EMove.Position:
				sort = pos * 10 + 1;
				break;
			}
			ProfileExManager.Instance.SetSort(lstProfile[index].IndexId, sort);
			return await Task.FromResult(0);
		}
	}

	public static async Task<int> AddCustomServer(Config config, ProfileItem profileItem, bool blDelete)
	{
		string address = profileItem.Address;
		if (!File.Exists(address))
		{
			return -1;
		}
		string extension = Path.GetExtension(address);
		string text = Utils.GetGuid() + extension;
		try
		{
			File.Copy(address, Utils.GetConfigPath(text));
			if (blDelete)
			{
				File.Delete(address);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return -1;
		}
		profileItem.Address = text;
		profileItem.ConfigType = EConfigType.Custom;
		if (profileItem.Remarks.IsNullOrEmpty())
		{
			profileItem.Remarks = "import custom@" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
		}
		await AddServerCommon(config, profileItem);
		return 0;
	}

	public static async Task<int> EditCustomServer(Config config, ProfileItem profileItem)
	{
		ProfileItem profileItem2 = await AppManager.Instance.GetProfileItem(profileItem.IndexId);
		if (profileItem2 == null)
		{
			profileItem2 = profileItem;
		}
		else
		{
			profileItem2.Remarks = profileItem.Remarks;
			profileItem2.Address = profileItem.Address;
			profileItem2.CoreType = profileItem.CoreType;
			profileItem2.DisplayLog = profileItem.DisplayLog;
			profileItem2.PreSocksPort = profileItem.PreSocksPort;
		}
		if (await SQLiteHelper.Instance.UpdateAsync(profileItem2) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<int> AddShadowsocksServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.Shadowsocks;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			SsMethod = profileItem.GetProtocolExtra().SsMethod?.TrimEx()
		});
		if (!AppManager.Instance.GetShadowsocksSecurities(profileItem).Contains(profileItem.GetProtocolExtra().SsMethod))
		{
			return -1;
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddSocksServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.SOCKS;
		profileItem.Address = profileItem.Address.TrimEx();
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddHttpServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.HTTP;
		profileItem.Address = profileItem.Address.TrimEx();
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddTrojanServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.Trojan;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		if (profileItem.StreamSecurity.IsNullOrEmpty())
		{
			profileItem.StreamSecurity = "tls";
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddHysteria2Server(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.Hysteria2;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.Fingerprint = string.Empty;
		profileItem.Alpn = string.Empty;
		profileItem.Network = string.Empty;
		if (profileItem.StreamSecurity.IsNullOrEmpty())
		{
			profileItem.StreamSecurity = "tls";
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			SalamanderPass = profileItem.GetProtocolExtra().SalamanderPass?.TrimEx(),
			HopInterval = profileItem.GetProtocolExtra().HopInterval?.TrimEx()
		});
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddTuicServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.TUIC;
		profileItem.CoreType = ECoreType.sing_box;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Username = profileItem.Username.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.Network = string.Empty;
		profileItem.Fingerprint = string.Empty;
		string text = profileItem.GetProtocolExtra().CongestionControl;
		if (!Global.TuicCongestionControls.Contains(text))
		{
			text = Global.TuicCongestionControls.FirstOrDefault();
		}
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			CongestionControl = text
		});
		if (profileItem.StreamSecurity.IsNullOrEmpty())
		{
			profileItem.StreamSecurity = "tls";
		}
		if (profileItem.Alpn.IsNullOrEmpty())
		{
			profileItem.Alpn = "h3";
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddWireguardServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.WireGuard;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		string text = profileItem.GetProtocolExtra().WgReserved?.TrimEx();
		if (!text.IsNullOrEmpty() && !text.Contains(','))
		{
			try
			{
				byte[] array = Convert.FromBase64String(text);
				byte[] array2 = new byte[3];
				Array.Copy(array, array2, Math.Min(array.Length, 3));
				text = string.Join(", ", array2);
			}
			catch
			{
			}
		}
		ProtocolExtraItem protocolExtraItem = profileItem.GetProtocolExtra() with
		{
			WgPublicKey = profileItem.GetProtocolExtra().WgPublicKey?.TrimEx(),
			WgPresharedKey = profileItem.GetProtocolExtra().WgPresharedKey?.TrimEx(),
			WgInterfaceAddress = profileItem.GetProtocolExtra().WgInterfaceAddress?.TrimEx(),
			WgReserved = text
		};
		int? wgMtu = profileItem.GetProtocolExtra().WgMtu;
		bool flag = ((!wgMtu.HasValue || wgMtu.GetValueOrDefault() <= 0) ? true : false);
		protocolExtraItem.WgMtu = (flag ? new int?(Global.TunMtus.First()) : profileItem.GetProtocolExtra().WgMtu);
		profileItem.SetProtocolExtra(protocolExtraItem);
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddAnytlsServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.Anytls;
		profileItem.CoreType = ECoreType.sing_box;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.Network = string.Empty;
		if (profileItem.StreamSecurity.IsNullOrEmpty())
		{
			profileItem.StreamSecurity = "tls";
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> AddNaiveServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.Naive;
		profileItem.CoreType = ECoreType.sing_box;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Username = profileItem.Username.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.Fingerprint = string.Empty;
		profileItem.Alpn = string.Empty;
		profileItem.Network = string.Empty;
		profileItem.AllowInsecure = "false";
		if (profileItem.StreamSecurity.IsNullOrEmpty())
		{
			profileItem.StreamSecurity = "tls";
		}
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<int> SortServers(Config config, string subId, string colName, bool asc)
	{
		List<ProfileItemModel> lstModel = await AppManager.Instance.ProfileModels(subId, "");
		if (lstModel.Count <= 0)
		{
			return -1;
		}
		List<ServerStatItem> lstServerStat = (config.GuiItem.EnableStatistics ? StatisticsManager.Instance.ServerStat : null) ?? new List<ServerStatItem>();
		ConcurrentBag<ProfileExItem> inner = await ProfileExManager.Instance.GetProfileExs();
		List<ProfileItemModel> list = (from t in lstModel
			join t2 in lstServerStat on t.IndexId equals t2.IndexId into t2b
			from t22 in t2b.DefaultIfEmpty()
			join t3 in inner on t.IndexId equals t3.IndexId into t3b
			from t33 in t3b.DefaultIfEmpty()
			select new ProfileItemModel
			{
				IndexId = t.IndexId,
				ConfigType = t.ConfigType,
				Remarks = t.Remarks,
				Address = t.Address,
				Port = t.Port,
				Network = t.Network,
				StreamSecurity = t.StreamSecurity,
				Delay = (t33?.Delay ?? 0),
				Speed = (t33?.Speed ?? 0m),
				Sort = (t33?.Sort ?? 0),
				IpInfo = (t33?.IpInfo ?? string.Empty),
				TodayDown = (t22?.TodayDown ?? 0).ToString("D16"),
				TodayUp = (t22?.TodayUp ?? 0).ToString("D16"),
				TotalDown = (t22?.TotalDown ?? 0).ToString("D16"),
				TotalUp = (t22?.TotalUp ?? 0).ToString("D16")
			}).ToList();
		Enum.TryParse<EServerColName>(colName, ignoreCase: true, out var result);
		list = ((!asc) ? (result switch
		{
			EServerColName.ConfigType => list.OrderByDescending((ProfileItemModel t) => t.ConfigType).ToList(), 
			EServerColName.Remarks => list.OrderByDescending((ProfileItemModel t) => t.Remarks).ToList(), 
			EServerColName.Address => list.OrderByDescending((ProfileItemModel t) => t.Address).ToList(), 
			EServerColName.Port => list.OrderByDescending((ProfileItemModel t) => t.Port).ToList(), 
			EServerColName.Network => list.OrderByDescending((ProfileItemModel t) => t.Network).ToList(), 
			EServerColName.StreamSecurity => list.OrderByDescending((ProfileItemModel t) => t.StreamSecurity).ToList(), 
			EServerColName.DelayVal => list.OrderByDescending((ProfileItemModel t) => t.Delay).ToList(), 
			EServerColName.SpeedVal => list.OrderByDescending((ProfileItemModel t) => t.Speed).ToList(), 
			EServerColName.IpInfo => list.OrderByDescending((ProfileItemModel t) => t.IpInfo).ToList(), 
			EServerColName.SubRemarks => list.OrderByDescending((ProfileItemModel t) => t.Subid).ToList(), 
			EServerColName.TodayDown => list.OrderByDescending((ProfileItemModel t) => t.TodayDown).ToList(), 
			EServerColName.TodayUp => list.OrderByDescending((ProfileItemModel t) => t.TodayUp).ToList(), 
			EServerColName.TotalDown => list.OrderByDescending((ProfileItemModel t) => t.TotalDown).ToList(), 
			EServerColName.TotalUp => list.OrderByDescending((ProfileItemModel t) => t.TotalUp).ToList(), 
			_ => list, 
		}) : (result switch
		{
			EServerColName.ConfigType => list.OrderBy((ProfileItemModel t) => t.ConfigType).ToList(), 
			EServerColName.Remarks => list.OrderBy((ProfileItemModel t) => t.Remarks).ToList(), 
			EServerColName.Address => list.OrderBy((ProfileItemModel t) => t.Address).ToList(), 
			EServerColName.Port => list.OrderBy((ProfileItemModel t) => t.Port).ToList(), 
			EServerColName.Network => list.OrderBy((ProfileItemModel t) => t.Network).ToList(), 
			EServerColName.StreamSecurity => list.OrderBy((ProfileItemModel t) => t.StreamSecurity).ToList(), 
			EServerColName.DelayVal => list.OrderBy((ProfileItemModel t) => t.Delay).ToList(), 
			EServerColName.SpeedVal => list.OrderBy((ProfileItemModel t) => t.Speed).ToList(), 
			EServerColName.IpInfo => list.OrderBy((ProfileItemModel t) => t.IpInfo).ToList(), 
			EServerColName.SubRemarks => list.OrderBy((ProfileItemModel t) => t.Subid).ToList(), 
			EServerColName.TodayDown => list.OrderBy((ProfileItemModel t) => t.TodayDown).ToList(), 
			EServerColName.TodayUp => list.OrderBy((ProfileItemModel t) => t.TodayUp).ToList(), 
			EServerColName.TotalDown => list.OrderBy((ProfileItemModel t) => t.TotalDown).ToList(), 
			EServerColName.TotalUp => list.OrderBy((ProfileItemModel t) => t.TotalUp).ToList(), 
			_ => list, 
		}));
		checked
		{
			for (int num = 0; num < list.Count; num++)
			{
				ProfileExManager.Instance.SetSort(list[num].IndexId, (num + 1) * 10);
			}
			switch (result)
			{
			case EServerColName.DelayVal:
			{
				int sort2 = list.Max((ProfileItemModel t) => t.Sort) + 10;
				foreach (ProfileItemModel item in list.Where((ProfileItemModel item) => item.Delay <= 0))
				{
					ProfileExManager.Instance.SetSort(item.IndexId, sort2);
				}
				break;
			}
			case EServerColName.SpeedVal:
			{
				int sort = list.Max((ProfileItemModel t) => t.Sort) + 10;
				foreach (ProfileItemModel item2 in list.Where((ProfileItemModel item) => item.Speed <= 0m))
				{
					ProfileExManager.Instance.SetSort(item2.IndexId, sort);
				}
				break;
			}
			}
			return 0;
		}
	}

	public static async Task<int> AddVlessServer(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigType = EConfigType.VLESS;
		profileItem.Address = profileItem.Address.TrimEx();
		profileItem.Password = profileItem.Password.TrimEx();
		profileItem.Network = profileItem.Network.TrimEx();
		profileItem.StreamSecurity = profileItem.StreamSecurity.TrimEx();
		string text = profileItem.GetProtocolExtra().VlessEncryption?.TrimEx();
		string text2 = profileItem.GetProtocolExtra().Flow?.TrimEx() ?? string.Empty;
		profileItem.SetProtocolExtra(profileItem.GetProtocolExtra() with
		{
			VlessEncryption = (text.IsNullOrEmpty() ? "none" : text),
			Flow = (Global.Flows.Contains(text2) ? text2 : Global.Flows.First())
		});
		if (profileItem.Password.IsNullOrEmpty())
		{
			return -1;
		}
		await AddServerCommon(config, profileItem, toFile);
		return 0;
	}

	public static async Task<Tuple<int, int>> DedupServerList(Config config, string subId)
	{
		List<ProfileItem> lstProfile = await AppManager.Instance.ProfileItems(subId);
		if (lstProfile == null)
		{
			return new Tuple<int, int>(0, 0);
		}
		List<ProfileItem> lstKeep = new List<ProfileItem>();
		List<ProfileItem> list = new List<ProfileItem>();
		if (!config.GuiItem.KeepOlderDedupl)
		{
			lstProfile.Reverse();
		}
		foreach (ProfileItem item in lstProfile)
		{
			if (item.IsComplex())
			{
				lstKeep.Add(item);
			}
			else if (lstKeep.Exists((ProfileItem i) => CompareProfileItem(i, item, remarks: false)))
			{
				list.Add(item);
			}
			else
			{
				lstKeep.Add(item);
			}
		}
		await RemoveServers(config, list);
		return new Tuple<int, int>(lstProfile.Count, lstKeep.Count);
	}

	public static async Task<int> AddServerCommon(Config config, ProfileItem profileItem, bool toFile = true)
	{
		profileItem.ConfigVersion = 4;
		if (profileItem.StreamSecurity.IsNotEmpty())
		{
			string streamSecurity = profileItem.StreamSecurity;
			if (!(streamSecurity == "tls") && !(streamSecurity == "reality"))
			{
				profileItem.StreamSecurity = string.Empty;
			}
			else if (profileItem.Fingerprint.IsNullOrEmpty() && profileItem.StreamSecurity == "reality")
			{
				profileItem.Fingerprint = config.CoreBasicItem.DefFingerprint;
			}
		}
		if (profileItem.Network.IsNotEmpty() && !Global.Networks.Contains(profileItem.Network))
		{
			profileItem.Network = "raw";
		}
		int num = -1;
		if (profileItem.IndexId.IsNullOrEmpty())
		{
			profileItem.IndexId = Utils.GetGuid(full: false);
			num = ProfileExManager.Instance.GetMaxSort();
		}
		if (!toFile && num < 0)
		{
			num = ProfileExManager.Instance.GetMaxSort();
		}
		if (num > 0)
		{
			ProfileExManager.Instance.SetSort(profileItem.IndexId, checked(num + 1));
		}
		if (toFile)
		{
			profileItem.SetProtocolExtra(profileItem.GetProtocolExtra());
			await SQLiteHelper.Instance.ReplaceAsync(profileItem);
		}
		return 0;
	}

	private static bool CompareProfileItem(ProfileItem? o, ProfileItem? n, bool remarks)
	{
		if (o == null || n == null)
		{
			return false;
		}
		ProtocolExtraItem protocolExtra = o.GetProtocolExtra();
		ProtocolExtraItem protocolExtra2 = n.GetProtocolExtra();
		TransportExtraItem transportExtra = o.GetTransportExtra();
		TransportExtraItem transportExtra2 = n.GetTransportExtra();
		if (o.ConfigType == n.ConfigType && AreEqual(o.Address, n.Address) && o.Port == n.Port && AreEqual(o.Password, n.Password) && AreEqual(o.Username, n.Username) && AreEqual(protocolExtra.VlessEncryption, protocolExtra2.VlessEncryption) && AreEqual(protocolExtra.SsMethod, protocolExtra2.SsMethod) && AreEqual(protocolExtra.VmessSecurity, protocolExtra2.VmessSecurity) && AreEqual(o.Network, n.Network) && AreEqual(transportExtra.RawHeaderType, transportExtra2.RawHeaderType) && AreEqual(transportExtra.Host, transportExtra2.Host) && AreEqual(transportExtra.Path, transportExtra2.Path) && AreEqual(transportExtra.XhttpMode, transportExtra2.XhttpMode) && AreEqual(transportExtra.XhttpExtra, transportExtra2.XhttpExtra) && AreEqual(transportExtra.GrpcAuthority, transportExtra2.GrpcAuthority) && AreEqual(transportExtra.GrpcServiceName, transportExtra2.GrpcServiceName) && AreEqual(transportExtra.GrpcMode, transportExtra2.GrpcMode) && AreEqual(transportExtra.KcpHeaderType, transportExtra2.KcpHeaderType) && AreEqual(transportExtra.KcpSeed, transportExtra2.KcpSeed) && (o.ConfigType == EConfigType.Trojan || o.StreamSecurity == n.StreamSecurity) && AreEqual(protocolExtra.Flow, protocolExtra2.Flow) && AreEqual(protocolExtra.SalamanderPass, protocolExtra2.SalamanderPass) && AreEqual(o.Sni, n.Sni) && AreEqual(o.Alpn, n.Alpn) && AreEqual(o.Fingerprint, n.Fingerprint) && AreEqual(o.PublicKey, n.PublicKey) && AreEqual(o.ShortId, n.ShortId) && AreEqual(o.Finalmask, n.Finalmask))
		{
			if (remarks)
			{
				return o.Remarks == n.Remarks;
			}
			return true;
		}
		return false;
		static bool AreEqual(string? a, string? b)
		{
			if (!string.Equals(a, b))
			{
				if (string.IsNullOrEmpty(a))
				{
					return string.IsNullOrEmpty(b);
				}
				return false;
			}
			return true;
		}
	}

	private static ProfileItem? FindMatchedProfileItem(IEnumerable<ProfileItem>? source, ProfileItem? target)
	{
		if (source == null || target == null)
		{
			return null;
		}
		ProfileItem profileItem = source.FirstOrDefault((ProfileItem t) => CompareProfileItem(t, target, remarks: true));
		if (profileItem != null)
		{
			return profileItem;
		}
		if (target.Remarks.IsNotEmpty())
		{
			profileItem = source.FirstOrDefault((ProfileItem t) => t.Remarks == target.Remarks);
			if (profileItem != null)
			{
				return profileItem;
			}
		}
		if (target.Address.IsNotEmpty() && target.Port > 0 && target.Password.IsNotEmpty())
		{
			profileItem = source.FirstOrDefault((ProfileItem t) => IsSameText(t.Address, target.Address) && t.Port == target.Port && IsSameText(t.Password, target.Password));
			if (profileItem != null)
			{
				return profileItem;
			}
		}
		if (target.Address.IsNotEmpty() && target.Port > 0)
		{
			profileItem = source.FirstOrDefault((ProfileItem t) => IsSameText(t.Address, target.Address) && t.Port == target.Port);
			if (profileItem != null)
			{
				return profileItem;
			}
		}
		if (target.Address.IsNotEmpty())
		{
			profileItem = source.FirstOrDefault((ProfileItem t) => IsSameText(t.Address, target.Address));
			if (profileItem != null)
			{
				return profileItem;
			}
		}
		return null;
		static bool IsSameText(string? left, string? right)
		{
			if (left.IsNullOrEmpty() || right.IsNullOrEmpty())
			{
				return false;
			}
			return string.Equals(left.TrimEx(), right.TrimEx(), StringComparison.OrdinalIgnoreCase);
		}
	}

	private static async Task<int> RemoveProfileItem(Config config, string indexId)
	{
		_ = 1;
		try
		{
			ProfileItem profileItem = await AppManager.Instance.GetProfileItem(indexId);
			if (profileItem == null)
			{
				return 0;
			}
			if (profileItem.ConfigType == EConfigType.Custom)
			{
				File.Delete(Utils.GetConfigPath(profileItem.Address));
			}
			await SQLiteHelper.Instance.DeleteAsync(profileItem);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return 0;
	}

	public static async Task<RetResult> AddGroupAllServer(Config config, SubItem? subItem)
	{
		RetResult result = new RetResult();
		string text = subItem?.Id;
		if (text.IsNullOrEmpty())
		{
			result.Success = false;
			return result;
		}
		string indexId = Utils.GetGuid(full: false);
		string remarks = subItem.Remarks + " - " + ResUI.TbConfigTypePolicyGroup;
		ProfileItem profileItem = new ProfileItem
		{
			IndexId = indexId,
			CoreType = ECoreType.Xray,
			ConfigType = EConfigType.PolicyGroup,
			Remarks = remarks,
			IsSub = false
		};
		if (!text.IsNullOrEmpty())
		{
			profileItem.Subid = text;
		}
		ProtocolExtraItem protocolExtra = new ProtocolExtraItem
		{
			MultipleLoad = EMultipleLoad.LeastPing,
			GroupType = profileItem.ConfigType.ToString(),
			SubChildItems = text,
			Filter = "^(?!.*(?:剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset)).*$"
		};
		profileItem.SetProtocolExtra(protocolExtra);
		result.Success = await AddServerCommon(config, profileItem) == 0;
		result.Data = indexId;
		return result;
	}

	private static string CombineWithDefaultAllFilter(string regionPattern)
	{
		return $"^(?!.*(?:{"剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset"})).*(?:{regionPattern}).*$";
	}

	public static async Task<RetResult> AddGroupRegionServer(Config config, SubItem? subItem)
	{
		RetResult result = new RetResult();
		string subId = subItem?.Id;
		if (subId.IsNullOrEmpty())
		{
			result.Success = false;
			return result;
		}
		List<ProfileItem> childProfiles = await AppManager.Instance.ProfileItems(subId);
		List<string> indexIdList = new List<string>();
		foreach (KeyValuePair<string, string> policyGroupRegionFilter in PolicyGroupRegionFilters)
		{
			string indexId = Utils.GetGuid(full: false);
			string remarks = $"{subItem.Remarks} - {ResUI.TbConfigTypePolicyGroup} - {policyGroupRegionFilter.Key}";
			ProfileItem profileItem = new ProfileItem
			{
				IndexId = indexId,
				CoreType = ECoreType.Xray,
				ConfigType = EConfigType.PolicyGroup,
				Remarks = remarks,
				IsSub = false
			};
			if (!subId.IsNullOrEmpty())
			{
				profileItem.Subid = subId;
			}
			ProtocolExtraItem extraItem = new ProtocolExtraItem
			{
				MultipleLoad = EMultipleLoad.LeastPing,
				GroupType = profileItem.ConfigType.ToString(),
				SubChildItems = subId,
				Filter = CombineWithDefaultAllFilter(policyGroupRegionFilter.Value)
			};
			profileItem.SetProtocolExtra(extraItem);
			if ((childProfiles?.Where((ProfileItem p) => p != null && p.IsValid() && !p.ConfigType.IsComplexType() && (extraItem.Filter.IsNullOrEmpty() || Regex.IsMatch(p.Remarks, extraItem.Filter))).ToList() ?? new List<ProfileItem>()).Count != 0 && await AddServerCommon(config, profileItem) == 0)
			{
				indexIdList.Add(indexId);
			}
		}
		result.Success = indexIdList.Count > 0;
		result.Data = indexIdList;
		return result;
	}

	public static ProfileItem? GetPreSocksItem(Config config, ProfileItem node, ECoreType coreType)
	{
		ProfileItem result = null;
		bool flag = config.TunModeItem.EnableLegacyProtect || Utils.IsNonWindows();
		if ((node.ConfigType != EConfigType.Custom && coreType != ECoreType.sing_box && config.TunModeItem.EnableTun) & flag)
		{
			result = new ProfileItem
			{
				CoreType = ECoreType.sing_box,
				ConfigType = EConfigType.SOCKS,
				Address = "127.0.0.1",
				Port = AppManager.Instance.GetLocalPort(EInboundProtocol.socks)
			};
		}
		else if (node.ConfigType == EConfigType.Custom)
		{
			int? preSocksPort = node.PreSocksPort;
			if (preSocksPort.HasValue)
			{
				int valueOrDefault = preSocksPort.GetValueOrDefault();
				if (valueOrDefault > 0 && valueOrDefault <= 65535)
				{
					ECoreType value = (config.TunModeItem.EnableTun ? ECoreType.sing_box : ECoreType.Xray);
					result = new ProfileItem
					{
						CoreType = value,
						ConfigType = EConfigType.SOCKS,
						Address = "127.0.0.1",
						Port = node.PreSocksPort.Value
					};
				}
			}
		}
		return result;
	}

	public static async Task<int> RemoveInvalidServerResult(Config config, string subid)
	{
		List<ProfileItemModel> lstModel = await AppManager.Instance.ProfileModels(subid, "");
		lstModel.RemoveAll((ProfileItemModel t) => t.ConfigType.IsComplexType());
		if (lstModel != null && lstModel.Count <= 0)
		{
			return -1;
		}
		List<ProfileItemModel> lstProfile = (from t in lstModel
			join t2 in await ProfileExManager.Instance.GetProfileExs() on t.IndexId equals t2.IndexId
			where t2.Delay == -1
			select t).ToList();
		await RemoveServers(config, JsonUtils.Deserialize<List<ProfileItem>>(JsonUtils.Serialize(lstProfile)));
		return lstProfile.Count;
	}

	private static async Task<int> AddBatchServersCommon(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		string subFilter = string.Empty;
		if (isSub && subid.IsNotEmpty())
		{
			subFilter = (await AppManager.Instance.GetSubItem(subid))?.Filter ?? "";
		}
		int countServers = 0;
		List<ProfileItem> lstAdd = new List<ProfileItem>();
		IEnumerable<string> enumerable = from t in strData.Split(Environment.NewLine.ToCharArray())
			where !t.IsNullOrEmpty()
			select t;
		if (isSub)
		{
			enumerable = enumerable.Distinct();
		}
		checked
		{
			foreach (string item in enumerable)
			{
				if (!isSub && (item.StartsWith("https://") || item.StartsWith("http://")))
				{
					if (await AddSubItem(config, item) == 0)
					{
						countServers++;
					}
					continue;
				}
				ProfileItem profileItem = FmtHandler.ResolveConfig(item, out string _);
				if (profileItem != null && (!isSub || !subid.IsNotEmpty() || !subFilter.IsNotEmpty() || Regex.IsMatch(profileItem.Remarks, subFilter)))
				{
					profileItem.Subid = subid;
					profileItem.IsSub = isSub;
					if (profileItem.ConfigType switch
					{
						EConfigType.VMess => await AddVMessServer(config, profileItem, toFile: false), 
						EConfigType.Shadowsocks => await AddShadowsocksServer(config, profileItem, toFile: false), 
						EConfigType.SOCKS => await AddSocksServer(config, profileItem, toFile: false), 
						EConfigType.Trojan => await AddTrojanServer(config, profileItem, toFile: false), 
						EConfigType.VLESS => await AddVlessServer(config, profileItem, toFile: false), 
						EConfigType.Hysteria2 => await AddHysteria2Server(config, profileItem, toFile: false), 
						EConfigType.TUIC => await AddTuicServer(config, profileItem, toFile: false), 
						EConfigType.WireGuard => await AddWireguardServer(config, profileItem, toFile: false), 
						EConfigType.Anytls => await AddAnytlsServer(config, profileItem, toFile: false), 
						EConfigType.Naive => await AddNaiveServer(config, profileItem, toFile: false), 
						_ => -1, 
					} == 0)
					{
						countServers++;
						lstAdd.Add(profileItem);
					}
				}
			}
			if (lstAdd.Count > 0)
			{
				await SQLiteHelper.Instance.InsertAllAsync(lstAdd);
			}
			await SaveConfig(config);
			return countServers;
		}
	}

	private static async Task<int> AddBatchServers4Custom(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		SubItem subItem = await AppManager.Instance.GetSubItem(subid);
		string subRemarks = subItem?.Remarks;
		int? preSocksPort = subItem?.PreSocksPort;
		List<ProfileItem> list = null;
		if (list == null || list.Count <= 0)
		{
			list = SingboxFmt.ResolveFullArray(strData, subRemarks);
		}
		if (list == null || list.Count <= 0)
		{
			list = V2rayFmt.ResolveFullArray(strData, subRemarks);
		}
		if (list != null && list.Count > 0)
		{
			int count = 0;
			foreach (ProfileItem item in list)
			{
				item.Subid = subid;
				item.IsSub = isSub;
				item.PreSocksPort = preSocksPort;
				if (await AddCustomServer(config, item, blDelete: true) == 0)
				{
					count = checked(count + 1);
				}
			}
			if (count > 0)
			{
				return count;
			}
		}
		ProfileItem profileItem = null;
		if (profileItem == null)
		{
			profileItem = SingboxFmt.ResolveFull(strData, subRemarks);
		}
		if (profileItem == null)
		{
			profileItem = V2rayFmt.ResolveFull(strData, subRemarks);
		}
		if (profileItem == null && HtmlPageFmt.IsHtmlPage(strData))
		{
			return -1;
		}
		if (profileItem == null)
		{
			profileItem = ClashFmt.ResolveFull(strData, subRemarks);
		}
		if (profileItem == null)
		{
			profileItem = Hysteria2Fmt.ResolveFull2(strData, subRemarks);
		}
		if (profileItem == null || profileItem.Address.IsNullOrEmpty())
		{
			return -1;
		}
		profileItem.Subid = subid;
		profileItem.IsSub = isSub;
		profileItem.PreSocksPort = preSocksPort;
		if (await AddCustomServer(config, profileItem, blDelete: true) == 0)
		{
			return 1;
		}
		return -1;
	}

	private static async Task<int> AddBatchServers4SsSIP008(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		List<ProfileItem> list = ShadowsocksFmt.ResolveSip008(strData);
		if (list != null && list.Count > 0)
		{
			int counter = 0;
			foreach (ProfileItem item in list)
			{
				item.Subid = subid;
				item.IsSub = isSub;
				if (await AddShadowsocksServer(config, item) == 0)
				{
					counter = checked(counter + 1);
				}
			}
			await SaveConfig(config);
			return counter;
		}
		return -1;
	}

	private static async Task<int> AddBatchServers4Wireguard(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		if (!strData.Contains("[Interface]", StringComparison.OrdinalIgnoreCase) || !strData.Contains("[Peer]", StringComparison.OrdinalIgnoreCase))
		{
			return -1;
		}
		List<ProfileItem> list = WireguardFmt.ResolveConfig(strData);
		if (list != null && list.Count > 0)
		{
			int counter = 0;
			foreach (ProfileItem item in list)
			{
				item.Subid = subid;
				item.IsSub = isSub;
				if (await AddWireguardServer(config, item) == 0)
				{
					counter = checked(counter + 1);
				}
			}
			await SaveConfig(config);
			return counter;
		}
		return -1;
	}

	private static async Task<int> AddBatchServers4InnerUri(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		List<ProfileItem> list = InnerFmt.Resolve(strData, subid);
		if (list != null && list.Count > 0)
		{
			int counter = 0;
			List<ProfileItem> lstAdd = new List<ProfileItem>();
			foreach (ProfileItem profileItem in list)
			{
				profileItem.Subid = subid;
				profileItem.IsSub = isSub;
				int num;
				switch (profileItem.ConfigType)
				{
				case EConfigType.VMess:
					num = await AddVMessServer(config, profileItem, toFile: false);
					break;
				case EConfigType.Shadowsocks:
					num = await AddShadowsocksServer(config, profileItem, toFile: false);
					break;
				case EConfigType.HTTP:
					num = await AddHttpServer(config, profileItem, toFile: false);
					break;
				case EConfigType.SOCKS:
					num = await AddSocksServer(config, profileItem, toFile: false);
					break;
				case EConfigType.Trojan:
					num = await AddTrojanServer(config, profileItem, toFile: false);
					break;
				case EConfigType.VLESS:
					num = await AddVlessServer(config, profileItem, toFile: false);
					break;
				case EConfigType.Hysteria2:
					num = await AddHysteria2Server(config, profileItem, toFile: false);
					break;
				case EConfigType.TUIC:
					num = await AddTuicServer(config, profileItem, toFile: false);
					break;
				case EConfigType.WireGuard:
					num = await AddWireguardServer(config, profileItem, toFile: false);
					break;
				case EConfigType.Anytls:
					num = await AddAnytlsServer(config, profileItem, toFile: false);
					break;
				case EConfigType.Naive:
					num = await AddNaiveServer(config, profileItem, toFile: false);
					break;
				case EConfigType.PolicyGroup:
				case EConfigType.ProxyChain:
					num = await AddServerCommon(config, profileItem, toFile: false);
					break;
				default:
					num = -1;
					break;
				}
				if (num == 0)
				{
					counter = checked(counter + 1);
					lstAdd.Add(profileItem);
				}
			}
			if (lstAdd.Count > 0)
			{
				await SQLiteHelper.Instance.InsertAllAsync(lstAdd);
			}
			await SaveConfig(config);
			return counter;
		}
		return -1;
	}

	public static async Task<int> AddBatchServers(Config config, string strData, string subid, bool isSub)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		List<ProfileItem> lstOriSub = null;
		ProfileItem activeProfile = null;
		if (isSub && subid.IsNotEmpty())
		{
			lstOriSub = await AppManager.Instance.ProfileItems(subid);
			activeProfile = lstOriSub?.FirstOrDefault((ProfileItem t) => t.IndexId == config.IndexId);
			await RemoveServersViaSubid(config, subid, isSub: true);
		}
		int counter = 0;
		if (Utils.IsBase64String(strData))
		{
			counter = await AddBatchServersCommon(config, Utils.Base64Decode(strData), subid, isSub);
		}
		if (counter < 1)
		{
			counter = await AddBatchServersCommon(config, strData, subid, isSub);
		}
		if (counter < 1)
		{
			counter = await AddBatchServersCommon(config, Utils.Base64Decode(strData), subid, isSub);
		}
		if (counter < 1)
		{
			counter = await AddBatchServers4SsSIP008(config, strData, subid, isSub);
		}
		if (counter < 1)
		{
			counter = await AddBatchServers4Wireguard(config, strData, subid, isSub);
		}
		int num = 0;
		if (Utils.IsBase64String(strData))
		{
			num = await AddBatchServers4InnerUri(config, Utils.Base64Decode(strData), subid, isSub);
		}
		if (num < 1)
		{
			num = await AddBatchServers4InnerUri(config, strData, subid, isSub);
		}
		if (num < 1)
		{
			num = await AddBatchServers4InnerUri(config, Utils.Base64Decode(strData), subid, isSub);
		}
		if (num > 0)
		{
			counter = ((counter <= 0) ? num : checked(counter + num));
		}
		if (counter < 1)
		{
			counter = await AddBatchServers4Custom(config, strData, subid, isSub);
		}
		if (activeProfile != null)
		{
			ProfileItem profileItem = FindMatchedProfileItem(await AppManager.Instance.ProfileItems(subid), activeProfile);
			if (profileItem != null)
			{
				await SetDefaultServerIndex(config, profileItem.IndexId);
			}
		}
		if (lstOriSub != null)
		{
			foreach (ProfileItem item in await AppManager.Instance.ProfileItems(subid))
			{
				ProfileItem profileItem2 = FindMatchedProfileItem(lstOriSub, item);
				if (profileItem2 != null)
				{
					await StatisticsManager.Instance.CloneServerStatItem(profileItem2.IndexId, item.IndexId);
				}
			}
		}
		return counter;
	}

	public static async Task<int> AddSubItem(Config config, string url)
	{
		if (await SQLiteHelper.Instance.TableAsync<SubItem>().CountAsync((SubItem e) => e.Url == url) > 0)
		{
			return 0;
		}
		SubItem subItem = new SubItem
		{
			Id = string.Empty,
			Url = url
		};
		Uri uri = Utils.TryUri(url);
		if (uri == null)
		{
			return -1;
		}
		if (url.StartsWith("http://") && !Utils.IsPrivateNetwork(uri.IdnHost))
		{
			NoticeManager.Instance.Enqueue(ResUI.InsecureUrlProtocol);
		}
		NameValueCollection nameValueCollection = Utils.ParseQueryString(uri.Query);
		subItem.Remarks = nameValueCollection["remarks"] ?? "import_sub";
		return await AddSubItem(config, subItem);
	}

	public static async Task<int> AddSubItem(Config config, SubItem subItem)
	{
		SubItem item = await AppManager.Instance.GetSubItem(subItem.Id);
		if (item == null)
		{
			item = subItem;
		}
		else
		{
			item.Remarks = subItem.Remarks;
			item.Url = subItem.Url;
			item.MoreUrl = subItem.MoreUrl;
			item.Enabled = subItem.Enabled;
			item.AutoUpdateInterval = subItem.AutoUpdateInterval;
			item.UserAgent = subItem.UserAgent;
			item.Sort = subItem.Sort;
			item.Filter = subItem.Filter;
			item.UpdateTime = subItem.UpdateTime;
			item.ConvertTarget = subItem.ConvertTarget;
			item.PrevProfile = subItem.PrevProfile;
			item.NextProfile = subItem.NextProfile;
			item.PreSocksPort = subItem.PreSocksPort;
			item.Memo = subItem.Memo;
		}
		if (item.Id.IsNullOrEmpty())
		{
			item.Id = Utils.GetGuid(full: false);
			if (item.Sort <= 0)
			{
				int maxSort = 0;
				if (await SQLiteHelper.Instance.TableAsync<SubItem>().CountAsync() > 0)
				{
					maxSort = (await AppManager.Instance.SubItems()).LastOrDefault()?.Sort ?? 0;
				}
				item.Sort = checked(maxSort + 1);
			}
		}
		if (await SQLiteHelper.Instance.ReplaceAsync(item) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<int> RemoveServersViaSubid(Config config, string subid, bool isSub)
	{
		if (subid.IsNullOrEmpty())
		{
			return -1;
		}
		List<ProfileItem> customProfile = await (from t in SQLiteHelper.Instance.TableAsync<ProfileItem>()
			where t.Subid == subid && (int)t.ConfigType == 2
			select t).ToListAsync();
		if (!isSub)
		{
			await SQLiteHelper.Instance.ExecuteAsync("delete from ProfileItem where subid = '" + subid + "'");
		}
		else
		{
			await SQLiteHelper.Instance.ExecuteAsync("delete from ProfileItem where isSub = 1 and subid = '" + subid + "'");
		}
		foreach (ProfileItem item in customProfile)
		{
			File.Delete(Utils.GetConfigPath(item.Address));
		}
		return 0;
	}

	public static async Task<int> DeleteSubItem(Config config, string id)
	{
		SubItem item = await AppManager.Instance.GetSubItem(id);
		if (item == null)
		{
			return 0;
		}
		await SQLiteHelper.Instance.DeleteAsync(item);
		await RemoveServersViaSubid(config, id, isSub: false);
		if (item.Id == config.SubIndexId)
		{
			config.SubIndexId = (await AppManager.Instance.SubItems()).LastOrDefault()?.Id;
		}
		return 0;
	}

	public static async Task<int> MoveToGroup(Config config, List<ProfileItem> lstProfile, string subid)
	{
		foreach (ProfileItem item in lstProfile)
		{
			item.Subid = subid;
		}
		await SQLiteHelper.Instance.UpdateAllAsync(lstProfile);
		return 0;
	}

	public static async Task<int> SaveRoutingItem(Config config, RoutingItem item)
	{
		if (item.Id.IsNullOrEmpty())
		{
			item.Id = Utils.GetGuid(full: false);
		}
		if (await SQLiteHelper.Instance.ReplaceAsync(item) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<int> AddBatchRoutingRules(RoutingItem routingItem, string strData)
	{
		if (strData.IsNullOrEmpty())
		{
			return -1;
		}
		List<RulesItem> list = JsonUtils.Deserialize<List<RulesItem>>(strData);
		if (list == null)
		{
			return -1;
		}
		foreach (RulesItem item in list)
		{
			item.Id = Utils.GetGuid(full: false);
		}
		routingItem.RuleNum = list.Count;
		routingItem.RuleSet = JsonUtils.Serialize(list, indented: false);
		if (routingItem.Id.IsNullOrEmpty())
		{
			routingItem.Id = Utils.GetGuid(full: false);
		}
		if (await SQLiteHelper.Instance.ReplaceAsync(routingItem) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<int> MoveRoutingRule(List<RulesItem> rules, int index, EMove eMove, int pos = -1)
	{
		int count = rules.Count;
		checked
		{
			if (index < 0 || index > rules.Count - 1)
			{
				return -1;
			}
			switch (eMove)
			{
			case EMove.Top:
			{
				if (index == 0)
				{
					return 0;
				}
				RulesItem item5 = JsonUtils.DeepCopy(rules[index]);
				rules.RemoveAt(index);
				rules.Insert(0, item5);
				break;
			}
			case EMove.Up:
			{
				if (index == 0)
				{
					return 0;
				}
				RulesItem item4 = JsonUtils.DeepCopy(rules[index]);
				rules.RemoveAt(index);
				rules.Insert(index - 1, item4);
				break;
			}
			case EMove.Down:
			{
				if (index == count - 1)
				{
					return 0;
				}
				RulesItem item6 = JsonUtils.DeepCopy(rules[index]);
				rules.RemoveAt(index);
				rules.Insert(index + 1, item6);
				break;
			}
			case EMove.Bottom:
			{
				if (index == count - 1)
				{
					return 0;
				}
				RulesItem item3 = JsonUtils.DeepCopy(rules[index]);
				rules.RemoveAt(index);
				rules.Add(item3);
				break;
			}
			case EMove.Position:
			{
				RulesItem item = rules[index];
				RulesItem item2 = JsonUtils.DeepCopy(rules[index]);
				rules.Insert(pos, item2);
				rules.Remove(item);
				break;
			}
			}
			return await Task.FromResult(0);
		}
	}

	public static async Task<int> SetDefaultRouting(Config config, RoutingItem routingItem)
	{
		List<RoutingItem> list = await AppManager.Instance.RoutingItems();
		if (list.Any((RoutingItem t) => t.Id == routingItem.Id && t.IsActive))
		{
			return -1;
		}
		foreach (RoutingItem item in list)
		{
			if (item.Id == routingItem.Id)
			{
				item.IsActive = true;
			}
			else
			{
				item.IsActive = false;
			}
		}
		await SQLiteHelper.Instance.UpdateAllAsync(list);
		return 0;
	}

	public static async Task<RoutingItem> GetDefaultRouting(Config config)
	{
		RoutingItem routingItem = await SQLiteHelper.Instance.TableAsync<RoutingItem>().FirstOrDefaultAsync((RoutingItem it) => it.IsActive == true);
		if (routingItem == null)
		{
			RoutingItem item2 = await SQLiteHelper.Instance.TableAsync<RoutingItem>().FirstOrDefaultAsync();
			await SetDefaultRouting(config, item2);
			return item2;
		}
		return routingItem;
	}

	public static async Task<int> InitRouting(Config config, bool blImportAdvancedRules = false)
	{
		if (!config.ConstItem.RouteRulesTemplateSourceUrl.IsNullOrEmpty())
		{
			await InitExternalRouting(config, blImportAdvancedRules);
		}
		else
		{
			await InitBuiltinRouting(config, blImportAdvancedRules);
		}
		return 0;
	}

	public static async Task<int> InitExternalRouting(Config config, bool blImportAdvancedRules = false)
	{
		DownloadService downloadHandle = new DownloadService();
		string text = await downloadHandle.TryDownloadString(config.ConstItem.RouteRulesTemplateSourceUrl, blProxy: true, "");
		if (text.IsNullOrEmpty())
		{
			return await InitBuiltinRouting(config, blImportAdvancedRules);
		}
		RoutingTemplate template = JsonUtils.Deserialize<RoutingTemplate>(text);
		if (template == null)
		{
			return await InitBuiltinRouting(config, blImportAdvancedRules);
		}
		List<RoutingItem> list = await AppManager.Instance.RoutingItems();
		int maxSort = list.Count;
		if (!blImportAdvancedRules && list.Where((RoutingItem t) => t.Remarks.StartsWith(template.Version)).ToList().Count > 0)
		{
			return 0;
		}
		checked
		{
			for (int i = 0; i < template.RoutingItems.Length; i++)
			{
				RoutingItem item = template.RoutingItems[i];
				if (item.Url.IsNullOrEmpty() && item.RuleSet.IsNullOrEmpty())
				{
					continue;
				}
				string text2 = (item.RuleSet.IsNullOrEmpty() ? (await downloadHandle.TryDownloadString(item.Url, blProxy: true, "")) : item.RuleSet);
				string text3 = text2;
				if (!text3.IsNullOrEmpty())
				{
					item.Remarks = template.Version + "-" + item.Remarks;
					item.Enabled = true;
					maxSort = (item.Sort = maxSort + 1);
					item.Url = string.Empty;
					await AddBatchRoutingRules(item, text3);
					if (!blImportAdvancedRules && i == 0)
					{
						await SetDefaultRouting(config, item);
					}
				}
			}
			return 0;
		}
	}

	public static async Task<int> InitBuiltinRouting(Config config, bool blImportAdvancedRules = false)
	{
		string ver = "V4-";
		List<RoutingItem> list = await AppManager.Instance.RoutingItems();
		RoutingItem routingItem = list?.FirstOrDefault((RoutingItem t) => t.Locked);
		if (routingItem != null)
		{
			await RemoveRoutingItem(routingItem);
			list = await AppManager.Instance.RoutingItems();
		}
		if (!blImportAdvancedRules && Enumerable.Count(list, (RoutingItem u) => u.Remarks.StartsWith(ver)) > 0)
		{
			if (config.RoutingBasicItem.RoutingIndexId.IsNotEmpty())
			{
				RoutingItem routingItem2 = list.FirstOrDefault((RoutingItem t) => t.Id == config.RoutingBasicItem.RoutingIndexId);
				if (routingItem2 != null)
				{
					await SetDefaultRouting(config, routingItem2);
				}
				config.RoutingBasicItem.RoutingIndexId = string.Empty;
			}
			return 0;
		}
		int maxSort = list.Count;
		checked
		{
			await AddBatchRoutingRules(new RoutingItem
			{
				Remarks = ver + "Bypass mainland China",
				Url = string.Empty,
				Sort = maxSort + 1
			}, EmbedUtils.GetEmbedText("ServiceLib.Sample.custom_routing_white"));
			await AddBatchRoutingRules(new RoutingItem
			{
				Remarks = ver + "Blocked sites only",
				Url = string.Empty,
				Sort = maxSort + 2
			}, EmbedUtils.GetEmbedText("ServiceLib.Sample.custom_routing_black"));
			RoutingItem item1 = new RoutingItem
			{
				Remarks = ver + "Everything through the tunnel",
				Url = string.Empty,
				Sort = maxSort + 3
			};
			await AddBatchRoutingRules(item1, EmbedUtils.GetEmbedText("ServiceLib.Sample.custom_routing_global"));
			if (!blImportAdvancedRules)
			{
				await SetDefaultRouting(config, item1);
			}
			return 0;
		}
	}

	public static async Task RemoveRoutingItem(RoutingItem routingItem)
	{
		await SQLiteHelper.Instance.DeleteAsync(routingItem);
	}

	public static async Task<int> InitBuiltinDNS(Config config)
	{
		List<DNSItem> items = await AppManager.Instance.DNSItems();
		bool flag = false;
		foreach (DNSItem item3 in items)
		{
			if (item3.NormalDNS.IsNullOrEmpty() && item3.Enabled)
			{
				item3.Enabled = false;
				flag = true;
			}
		}
		if (flag)
		{
			await SQLiteHelper.Instance.UpdateAllAsync(items);
		}
		if (items.Count <= 0)
		{
			DNSItem item = new DNSItem
			{
				Remarks = "V2ray",
				CoreType = ECoreType.Xray,
				Enabled = false
			};
			await SaveDNSItems(config, item);
			DNSItem item2 = new DNSItem
			{
				Remarks = "sing-box",
				CoreType = ECoreType.sing_box,
				Enabled = false
			};
			await SaveDNSItems(config, item2);
		}
		return 0;
	}

	public static async Task<int> SaveDNSItems(Config config, DNSItem item)
	{
		if (item == null)
		{
			return -1;
		}
		if (item.Id.IsNullOrEmpty())
		{
			item.Id = Utils.GetGuid(full: false);
		}
		if (await SQLiteHelper.Instance.ReplaceAsync(item) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<DNSItem> GetExternalDNSItem(ECoreType type, string url)
	{
		DNSItem currentItem = await AppManager.Instance.GetDNSItem(type);
		DownloadService downloadHandle = new DownloadService();
		string text = await downloadHandle.TryDownloadString(url, blProxy: true, "");
		if (text.IsNullOrEmpty())
		{
			return currentItem;
		}
		DNSItem template = JsonUtils.Deserialize<DNSItem>(text);
		if (template == null)
		{
			return currentItem;
		}
		if (!template.NormalDNS.IsNullOrEmpty())
		{
			DNSItem dNSItem = template;
			dNSItem.NormalDNS = await downloadHandle.TryDownloadString(template.NormalDNS, blProxy: true, "");
		}
		if (!template.TunDNS.IsNullOrEmpty())
		{
			DNSItem dNSItem = template;
			dNSItem.TunDNS = await downloadHandle.TryDownloadString(template.TunDNS, blProxy: true, "");
		}
		template.Id = currentItem.Id;
		template.Enabled = currentItem.Enabled;
		template.Remarks = currentItem.Remarks;
		template.CoreType = type;
		return template;
	}

	public static SimpleDNSItem InitBuiltinSimpleDNS()
	{
		return new SimpleDNSItem
		{
			UseSystemHosts = false,
			AddCommonHosts = true,
			FakeIP = false,
			GlobalFakeIp = true,
			BlockBindingQuery = true,
			DirectDNS = Global.DomainDirectDNSAddress.FirstOrDefault(),
			RemoteDNS = Global.DomainRemoteDNSAddress.FirstOrDefault(),
			BootstrapDNS = Global.DomainPureIPDNSAddress.FirstOrDefault()
		};
	}

	public static async Task<SimpleDNSItem> GetExternalSimpleDNSItem(string url)
	{
		string text = await new DownloadService().TryDownloadString(url, blProxy: true, "");
		if (text.IsNullOrEmpty())
		{
			return null;
		}
		SimpleDNSItem simpleDNSItem = JsonUtils.Deserialize<SimpleDNSItem>(text);
		if (simpleDNSItem == null)
		{
			return null;
		}
		return simpleDNSItem;
	}

	public static async Task<int> InitBuiltinFullConfigTemplate(Config config)
	{
		if ((await AppManager.Instance.FullConfigTemplateItem()).Count <= 0)
		{
			FullConfigTemplateItem item = new FullConfigTemplateItem
			{
				Remarks = "V2ray",
				CoreType = ECoreType.Xray
			};
			await SaveFullConfigTemplate(config, item);
			FullConfigTemplateItem item2 = new FullConfigTemplateItem
			{
				Remarks = "sing-box",
				CoreType = ECoreType.sing_box
			};
			await SaveFullConfigTemplate(config, item2);
		}
		return 0;
	}

	public static async Task<int> SaveFullConfigTemplate(Config config, FullConfigTemplateItem item)
	{
		if (item == null)
		{
			return -1;
		}
		if (item.Id.IsNullOrEmpty())
		{
			item.Id = Utils.GetGuid(full: false);
		}
		if (await SQLiteHelper.Instance.ReplaceAsync(item) > 0)
		{
			return 0;
		}
		return -1;
	}

	public static async Task<bool> ApplyRegionalPreset(Config config, EPresetType type)
	{
		switch (type)
		{
		case EPresetType.Default:
			config.ConstItem.GeoSourceUrl = "";
			config.ConstItem.SrsSourceUrl = "";
			config.ConstItem.RouteRulesTemplateSourceUrl = "";
			await SQLiteHelper.Instance.DeleteAllAsync<DNSItem>();
			await InitBuiltinDNS(config);
			config.SimpleDNSItem = InitBuiltinSimpleDNS();
			break;
		case EPresetType.Russia:
		{
			config.ConstItem.GeoSourceUrl = Global.GeoFilesSources[1];
			config.ConstItem.SrsSourceUrl = Global.SingboxRulesetSources[1];
			config.ConstItem.RouteRulesTemplateSourceUrl = Global.RoutingRulesSources[1];
			DNSItem xrayDnsRussia = await GetExternalDNSItem(ECoreType.Xray, Global.DNSTemplateSources[1] + "v2ray.json");
			DNSItem singboxDnsRussia = await GetExternalDNSItem(ECoreType.sing_box, Global.DNSTemplateSources[1] + "sing_box.json");
			SimpleDNSItem simpleDNSItem2 = await GetExternalSimpleDNSItem(Global.DNSTemplateSources[1] + "simple_dns.json");
			if (simpleDNSItem2 == null)
			{
				xrayDnsRussia.Enabled = true;
				singboxDnsRussia.Enabled = true;
				config.SimpleDNSItem = InitBuiltinSimpleDNS();
			}
			else
			{
				config.SimpleDNSItem = simpleDNSItem2;
			}
			await SaveDNSItems(config, xrayDnsRussia);
			await SaveDNSItems(config, singboxDnsRussia);
			break;
		}
		case EPresetType.Iran:
		{
			config.ConstItem.GeoSourceUrl = Global.GeoFilesSources[2];
			config.ConstItem.SrsSourceUrl = Global.SingboxRulesetSources[2];
			config.ConstItem.RouteRulesTemplateSourceUrl = Global.RoutingRulesSources[2];
			DNSItem xrayDnsIran = await GetExternalDNSItem(ECoreType.Xray, Global.DNSTemplateSources[2] + "v2ray.json");
			DNSItem singboxDnsIran = await GetExternalDNSItem(ECoreType.sing_box, Global.DNSTemplateSources[2] + "sing_box.json");
			SimpleDNSItem simpleDNSItem = await GetExternalSimpleDNSItem(Global.DNSTemplateSources[2] + "simple_dns.json");
			if (simpleDNSItem == null)
			{
				xrayDnsIran.Enabled = true;
				singboxDnsIran.Enabled = true;
				config.SimpleDNSItem = InitBuiltinSimpleDNS();
			}
			else
			{
				config.SimpleDNSItem = simpleDNSItem;
			}
			await SaveDNSItems(config, xrayDnsIran);
			await SaveDNSItems(config, singboxDnsIran);
			break;
		}
		}
		return true;
	}

	public static WindowSizeItem? GetWindowSizeItem(Config config, string typeName)
	{
		WindowSizeItem windowSizeItem = config?.UiItem?.WindowSizeItem?.FirstOrDefault((WindowSizeItem t) => t.TypeName == typeName);
		if (windowSizeItem == null || windowSizeItem.Width <= 0 || windowSizeItem.Height <= 0)
		{
			return null;
		}
		return windowSizeItem;
	}

	public static int SaveWindowSizeItem(Config config, string typeName, double width, double height)
	{
		WindowSizeItem windowSizeItem = config?.UiItem?.WindowSizeItem?.FirstOrDefault((WindowSizeItem t) => t.TypeName == typeName);
		if (windowSizeItem == null)
		{
			windowSizeItem = new WindowSizeItem
			{
				TypeName = typeName
			};
			config.UiItem.WindowSizeItem.Add(windowSizeItem);
		}
		checked
		{
			windowSizeItem.Width = (int)width;
			windowSizeItem.Height = (int)height;
			return 0;
		}
	}

	public static int SaveMainGirdHeight(Config config, double height1, double height2)
	{
		UIItem obj = config.UiItem ?? new UIItem();
		checked
		{
			obj.MainGirdHeight1 = (int)(height1 + 0.1);
			obj.MainGirdHeight2 = (int)(height2 + 0.1);
			return 0;
		}
	}
}

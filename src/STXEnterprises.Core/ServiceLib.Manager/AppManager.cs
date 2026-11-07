using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Events;
using ServiceLib.Handler;
using ServiceLib.Handler.SysProxy;
using ServiceLib.Helper;
using ServiceLib.Models.Configs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;

namespace ServiceLib.Manager;

public sealed class AppManager
{
	private static readonly Lazy<AppManager> _instance = new Lazy<AppManager>(() => new AppManager());

	private Config _config;

	private int? _statePort;

	private int? _statePort2;

	public static AppManager Instance => _instance.Value;

	public Config Config => _config;

	public int StatePort
	{
		get
		{
			int valueOrDefault = _statePort.GetValueOrDefault();
			if (!_statePort.HasValue)
			{
				valueOrDefault = Utils.GetFreePort(GetLocalPort(EInboundProtocol.api));
				_statePort = valueOrDefault;
			}
			return _statePort.Value;
		}
	}

	public int StatePort2
	{
		get
		{
			int valueOrDefault = _statePort2.GetValueOrDefault();
			if (!_statePort2.HasValue)
			{
				valueOrDefault = Utils.GetFreePort(GetLocalPort(EInboundProtocol.api2));
				_statePort2 = valueOrDefault;
			}
			return checked(_statePort2.Value + (_config.TunModeItem.EnableTun ? 1 : 0));
		}
	}

	public string LinuxSudoPwd { get; set; }

	public bool ShowInTaskbar { get; set; }

	public ECoreType RunningCoreType { get; set; }

	public Dictionary<ECoreType, string> LastCheckUpdateResults { get; set; } = new Dictionary<ECoreType, string>();

	public bool IsRunningCore(ECoreType type)
	{
		if (type != ECoreType.Xray)
		{
			if (type == ECoreType.sing_box)
			{
				ECoreType runningCoreType = RunningCoreType;
				if ((runningCoreType == ECoreType.mihomo || runningCoreType == ECoreType.sing_box) ? true : false)
				{
					goto IL_0043;
				}
			}
		}
		else
		{
			ECoreType runningCoreType = RunningCoreType;
			if (((uint)(runningCoreType - 1) <= 1u || runningCoreType == ECoreType.v2fly_v5) ? true : false)
			{
				goto IL_0043;
			}
		}
		return false;
		IL_0043:
		return true;
	}

	public void SetLastCheckUpdateResult(ECoreType coreType, string result)
	{
		LastCheckUpdateResults[coreType] = result;
	}

	public bool InitApp()
	{
		if (!Utils.HasWritePermission())
		{
			Environment.SetEnvironmentVariable("STXENTERPRISES_LOCAL_APPLICATION_DATA", "1", EnvironmentVariableTarget.Process);
		}
		Logging.Setup();
		Config config = ConfigHandler.LoadConfig();
		if (config == null)
		{
			return false;
		}
		_config = config;
		Thread.CurrentThread.CurrentUICulture = new CultureInfo(_config.UiItem.CurrentLanguage);
		if (Utils.IsWindows() && Environment.OSVersion.Version.Major < 10)
		{
			Environment.SetEnvironmentVariable("DOTNET_EnableWriteXorExecute", "0", EnvironmentVariableTarget.User);
		}
		SQLiteHelper.Instance.CreateTable<SubItem>();
		SQLiteHelper.Instance.CreateTable<ProfileItem>();
		SQLiteHelper.Instance.CreateTable<ServerStatItem>();
		SQLiteHelper.Instance.CreateTable<RoutingItem>();
		SQLiteHelper.Instance.CreateTable<ProfileExItem>();
		SQLiteHelper.Instance.CreateTable<DNSItem>();
		SQLiteHelper.Instance.CreateTable<FullConfigTemplateItem>();
		SQLiteHelper.Instance.CreateTable<ProfileGroupItem>();
		return true;
	}

	public bool InitComponents()
	{
		Logging.SaveLog("STX Enterprises start up | " + Utils.GetRuntimeInfo());
		Logging.LoggingEnabled(_config.GuiItem.EnableLog);
		_ = StatePort;
		_ = StatePort2;
		Task.Run(async () =>
		{
			await MigrateProfileExtra();
		}).Wait();
		return true;
	}

	public bool Reset()
	{
		_statePort = null;
		_statePort2 = null;
		return true;
	}

	public async Task AppExitAsync(bool needShutdown)
	{
		_ = 5;
		try
		{
			Logging.SaveLog("AppExitAsync Begin");
			await SysProxyHandler.UpdateSysProxy(_config, forceDisable: true);
			AppEvents.AppExitRequested.Publish();
			await Task.Delay(50);
			await ConfigHandler.SaveConfig(_config);
			await ProfileExManager.Instance.SaveTo();
			await StatisticsManager.Instance.SaveTo();
			await CoreManager.Instance.CoreStop();
			StatisticsManager.Instance.Close();
			Logging.SaveLog("AppExitAsync End");
		}
		catch
		{
		}
		finally
		{
			if (needShutdown)
			{
				Shutdown(byUser: false);
			}
		}
	}

	public void Shutdown(bool byUser)
	{
		AppEvents.ShutdownRequested.Publish(byUser);
	}

	public async Task RebootAsAdmin()
	{
		ProcUtils.RebootAsAdmin();
		await Instance.AppExitAsync(needShutdown: true);
	}

	public int GetLocalPort(EInboundProtocol protocol)
	{
		return (int)checked((_config.Inbound.FirstOrDefault((InItem t) => t.Protocol == "socks")?.LocalPort ?? 10808) + protocol);
	}

	public async Task<List<SubItem>?> SubItems()
	{
		return await (from t in SQLiteHelper.Instance.TableAsync<SubItem>()
			orderby t.Sort
			select t).ToListAsync();
	}

	public async Task<SubItem?> GetSubItem(string? subid)
	{
		return await SQLiteHelper.Instance.TableAsync<SubItem>().FirstOrDefaultAsync((SubItem t) => t.Id == subid);
	}

	public async Task<List<ProfileItem>?> ProfileItems(string subid)
	{
		if (subid.IsNullOrEmpty())
		{
			return await SQLiteHelper.Instance.TableAsync<ProfileItem>().ToListAsync();
		}
		return await (from t in SQLiteHelper.Instance.TableAsync<ProfileItem>()
			where t.Subid == subid
			select t).ToListAsync();
	}

	public async Task<List<string>?> ProfileItemIndexes(string subid)
	{
		return (await ProfileItems(subid))?.Select((ProfileItem t) => t.IndexId)?.ToList();
	}

	public async Task<List<ProfileItemModel>?> ProfileModels(string subid, string filter)
	{
		string text = "select a.IndexId\n                           ,a.ConfigType\n                           ,a.Remarks\n                           ,a.Address\n                           ,a.Port\n                           ,a.Network\n                           ,a.StreamSecurity\n                           ,a.Subid\n                           ,b.remarks as subRemarks\n                        from ProfileItem a\n                        left join SubItem b on a.subid = b.id\n                        where 1=1 ";
		if (subid.IsNotEmpty())
		{
			text = text + " and a.subid = '" + subid + "'";
		}
		if (filter.IsNotEmpty())
		{
			if (filter.Contains('\''))
			{
				filter = filter.Replace("'", "");
			}
			text += string.Format(" and (a.remarks like '%{0}%' or a.address like '%{0}%') ", filter);
		}
		return await SQLiteHelper.Instance.QueryAsync<ProfileItemModel>(text);
	}

	public async Task<ProfileItem?> GetProfileItem(string indexId)
	{
		if (indexId.IsNullOrEmpty())
		{
			return null;
		}
		return await SQLiteHelper.Instance.TableAsync<ProfileItem>().FirstOrDefaultAsync((ProfileItem it) => it.IndexId == indexId);
	}

	public async Task<List<ProfileItem>> GetProfileItemsByIndexIds(IEnumerable<string> indexIds)
	{
		List<string> ids = indexIds.Where((string id) => !id.IsNullOrEmpty()).Distinct().ToList();
		if (ids.Count == 0)
		{
			return new List<ProfileItem>();
		}
		return await (from it in SQLiteHelper.Instance.TableAsync<ProfileItem>()
			where ids.Contains(it.IndexId)
			select it).ToListAsync();
	}

	public async Task<Dictionary<string, ProfileItem>> GetProfileItemsByIndexIdsAsMap(IEnumerable<string> indexIds)
	{
		return (await GetProfileItemsByIndexIds(indexIds)).ToDictionary((ProfileItem it) => it.IndexId);
	}

	public async Task<List<ProfileItem>> GetProfileItemsOrderedByIndexIds(IEnumerable<string> indexIds)
	{
		List<string> idList = indexIds.Where((string id) => !id.IsNullOrEmpty()).Distinct().ToList();
		if (idList.Count == 0)
		{
			return new List<ProfileItem>();
		}
		Dictionary<string, ProfileItem> itemMap = (await (from it in SQLiteHelper.Instance.TableAsync<ProfileItem>()
			where idList.Contains(it.IndexId)
			select it).ToListAsync()).ToDictionary((ProfileItem it) => it.IndexId);
		return (from id in idList
			select itemMap.GetValueOrDefault(id) into item
			where item != null
			select item).ToList();
	}

	public async Task<ProfileItem?> GetProfileItemViaRemarks(string? remarks)
	{
		if (remarks.IsNullOrEmpty())
		{
			return null;
		}
		return await SQLiteHelper.Instance.TableAsync<ProfileItem>().FirstOrDefaultAsync((ProfileItem it) => it.Remarks == remarks);
	}

	public async Task<List<RoutingItem>?> RoutingItems()
	{
		return await (from t in SQLiteHelper.Instance.TableAsync<RoutingItem>()
			orderby t.Sort
			select t).ToListAsync();
	}

	public async Task<RoutingItem?> GetRoutingItem(string id)
	{
		return await SQLiteHelper.Instance.TableAsync<RoutingItem>().FirstOrDefaultAsync((RoutingItem it) => it.Id == id);
	}

	public async Task<List<DNSItem>?> DNSItems()
	{
		return await SQLiteHelper.Instance.TableAsync<DNSItem>().ToListAsync();
	}

	public async Task<DNSItem?> GetDNSItem(ECoreType eCoreType)
	{
		return await SQLiteHelper.Instance.TableAsync<DNSItem>().FirstOrDefaultAsync((DNSItem it) => (int)it.CoreType == (int)eCoreType);
	}

	public async Task<List<FullConfigTemplateItem>?> FullConfigTemplateItem()
	{
		return await SQLiteHelper.Instance.TableAsync<FullConfigTemplateItem>().ToListAsync();
	}

	public async Task<FullConfigTemplateItem?> GetFullConfigTemplateItem(ECoreType eCoreType)
	{
		return await SQLiteHelper.Instance.TableAsync<FullConfigTemplateItem>().FirstOrDefaultAsync((FullConfigTemplateItem it) => (int)it.CoreType == (int)eCoreType);
	}

	public async Task MigrateProfileExtra()
	{
		await MigrateProfileExtraGroupV2ToV3();
		await MigrateProfileExtraV2ToV3();
		await MigrateProfileTransportV3ToV4();
	}

	private async Task MigrateProfileExtraV2ToV3()
	{
		int offset = 0;
		while (true)
		{
			string sql = $"SELECT * FROM ProfileItem WHERE ConfigVersion < 3 AND ConfigType NOT IN ({101}, {102}) LIMIT {100} OFFSET {offset}";
			List<ProfileItem> batch = await SQLiteHelper.Instance.QueryAsync<ProfileItem>(sql);
			if (batch != null && batch.Count != 0)
			{
				int num = await MigrateProfileExtraV2ToV3Sub(batch);
				offset = checked(offset + (batch.Count - num));
				continue;
			}
			break;
		}
	}

	private async Task MigrateProfileTransportV3ToV4()
	{
		int offset = 0;
		checked
		{
			while (true)
			{
				string sql = $"SELECT * FROM ProfileItem WHERE ConfigVersion = 3 LIMIT {100} OFFSET {offset}";
				List<ProfileItem> batch = await SQLiteHelper.Instance.QueryAsync<ProfileItem>(sql);
				if (batch == null || batch.Count == 0)
				{
					break;
				}
				List<ProfileItem> list = new List<ProfileItem>();
				foreach (ProfileItem item in batch)
				{
					try
					{
						if (item.Network == "tcp")
						{
							item.Network = "raw";
						}
						TransportExtraItem transportExtra = item.GetTransportExtra();
						switch (item.GetNetwork())
						{
						case "raw":
							transportExtra = transportExtra with
							{
								RawHeaderType = item.HeaderType.NullIfEmpty(),
								Host = item.RequestHost.NullIfEmpty(),
								Path = item.Path.NullIfEmpty()
							};
							break;
						case "ws":
						case "httpupgrade":
							transportExtra = transportExtra with
							{
								Host = item.RequestHost.NullIfEmpty(),
								Path = item.Path.NullIfEmpty()
							};
							break;
						case "xhttp":
							transportExtra = transportExtra with
							{
								Host = item.RequestHost.NullIfEmpty(),
								Path = item.Path.NullIfEmpty(),
								XhttpMode = item.HeaderType.NullIfEmpty(),
								XhttpExtra = item.Extra.NullIfEmpty()
							};
							break;
						case "grpc":
							transportExtra = transportExtra with
							{
								GrpcAuthority = item.RequestHost.NullIfEmpty(),
								GrpcServiceName = item.Path.NullIfEmpty(),
								GrpcMode = item.HeaderType.NullIfEmpty()
							};
							break;
						case "kcp":
							transportExtra = transportExtra with
							{
								KcpHeaderType = item.HeaderType.NullIfEmpty(),
								KcpSeed = item.Path.NullIfEmpty()
							};
							break;
						default:
							item.Network = "raw";
							transportExtra = transportExtra with
							{
								RawHeaderType = item.HeaderType.NullIfEmpty(),
								Host = item.RequestHost.NullIfEmpty()
							};
							break;
						}
						item.SetTransportExtra(transportExtra);
						item.ConfigVersion = 4;
						list.Add(item);
					}
					catch (Exception value)
					{
						Logging.SaveLog($"MigrateProfileTransportV3ToV4 Error: {value}");
					}
				}
				if (list.Count > 0)
				{
					try
					{
						int num = await SQLiteHelper.Instance.UpdateAllAsync(list);
						offset += batch.Count - num;
					}
					catch (Exception value2)
					{
						Logging.SaveLog($"MigrateProfileTransportV3ToV4 update error: {value2}");
						offset += batch.Count;
					}
				}
				else
				{
					offset += batch.Count;
				}
			}
		}
	}

	private async Task<int> MigrateProfileExtraV2ToV3Sub(List<ProfileItem> batch)
	{
		List<ProfileItem> list = new List<ProfileItem>();
		foreach (ProfileItem item in batch)
		{
			try
			{
				ProtocolExtraItem protocolExtraItem = item.GetProtocolExtra();
				switch (item.ConfigType)
				{
				case EConfigType.Shadowsocks:
					protocolExtraItem = protocolExtraItem with
					{
						SsMethod = item.Security.NullIfEmpty()
					};
					break;
				case EConfigType.VMess:
					protocolExtraItem = protocolExtraItem with
					{
						AlterId = item.AlterId.ToString(),
						VmessSecurity = item.Security.NullIfEmpty()
					};
					break;
				case EConfigType.VLESS:
					protocolExtraItem = protocolExtraItem with
					{
						Flow = item.Flow.NullIfEmpty(),
						VlessEncryption = item.Security
					};
					break;
				case EConfigType.Hysteria2:
					protocolExtraItem = protocolExtraItem with
					{
						SalamanderPass = item.Path.NullIfEmpty(),
						Ports = item.Ports.NullIfEmpty(),
						UpMbps = _config.HysteriaItem.UpMbps,
						DownMbps = _config.HysteriaItem.DownMbps,
						HopInterval = _config.HysteriaItem.HopInterval.ToString()
					};
					break;
				case EConfigType.TUIC:
					protocolExtraItem = protocolExtraItem with
					{
						CongestionControl = item.HeaderType.NullIfEmpty()
					};
					item.Username = item.Id;
					item.Id = item.Security;
					item.Password = item.Security;
					break;
				case EConfigType.SOCKS:
				case EConfigType.HTTP:
					item.Username = item.Security;
					break;
				case EConfigType.WireGuard:
				{
					protocolExtraItem = protocolExtraItem with
					{
						WgPublicKey = item.PublicKey.NullIfEmpty(),
						WgInterfaceAddress = item.RequestHost.NullIfEmpty(),
						WgReserved = item.Path.NullIfEmpty(),
						WgMtu = (int.TryParse(item.ShortId, out var result) ? result : 1280)
					};
					break;
				}
				}
				item.SetProtocolExtra(protocolExtraItem);
				item.Password = item.Id;
				item.ConfigVersion = 3;
				list.Add(item);
			}
			catch (Exception value)
			{
				Logging.SaveLog($"MigrateProfileExtra Error: {value}");
			}
		}
		if (list.Count > 0)
		{
			try
			{
				return await SQLiteHelper.Instance.UpdateAllAsync(list);
			}
			catch (Exception value2)
			{
				Logging.SaveLog($"MigrateProfileExtraGroup update error: {value2}");
				return 0;
			}
		}
		return 0;
	}

	private async Task<bool> MigrateProfileExtraGroupV2ToV3()
	{
		ConcurrentDictionary<string, ProfileGroupItem> groupItems = new ConcurrentDictionary<string, ProfileGroupItem>((await SQLiteHelper.Instance.TableAsync<ProfileGroupItem>().ToListAsync()).Where((ProfileGroupItem t) => !string.IsNullOrEmpty(t.IndexId)).ToDictionary((ProfileGroupItem t) => t.IndexId));
		string sql = $"SELECT * FROM ProfileItem WHERE ConfigVersion < 3 AND ConfigType IN ({101}, {102})";
		List<ProfileItem> list = await SQLiteHelper.Instance.QueryAsync<ProfileItem>(sql);
		if (list == null || list.Count == 0)
		{
			Logging.SaveLog("MigrateProfileExtraGroup: No items to migrate.");
			return true;
		}
		Logging.SaveLog($"MigrateProfileExtraGroup: Found {list.Count} group items to migrate.");
		List<ProfileItem> updateProfileItems = new List<ProfileItem>();
		foreach (ProfileItem item in list)
		{
			try
			{
				ProtocolExtraItem protocolExtra = item.GetProtocolExtra();
				protocolExtra = protocolExtra with
				{
					GroupType = "ConfigType"
				};
				groupItems.TryGetValue(item.IndexId, out ProfileGroupItem value);
				if (value != null && !value.NotHasChild())
				{
					protocolExtra = protocolExtra with
					{
						ChildItems = value.ChildItems,
						SubChildItems = value.SubChildItems,
						Filter = value.Filter,
						MultipleLoad = value.MultipleLoad
					};
				}
				item.SetProtocolExtra(protocolExtra);
				item.ConfigVersion = 3;
				updateProfileItems.Add(item);
			}
			catch (Exception value2)
			{
				Logging.SaveLog($"MigrateProfileExtraGroup item error [{item.IndexId}]: {value2}");
			}
		}
		if (updateProfileItems.Count > 0)
		{
			try
			{
				int num = await SQLiteHelper.Instance.UpdateAllAsync(updateProfileItems);
				Logging.SaveLog($"MigrateProfileExtraGroup: Successfully updated {updateProfileItems.Count} items.");
				return updateProfileItems.Count == num;
			}
			catch (Exception value3)
			{
				Logging.SaveLog($"MigrateProfileExtraGroup update error: {value3}");
				return false;
			}
		}
		return true;
	}

	public List<string> GetShadowsocksSecurities(ProfileItem profileItem)
	{
		return GetCoreType(profileItem, EConfigType.Shadowsocks) switch
		{
			ECoreType.v2fly => Global.SsSecurities, 
			ECoreType.Xray => Global.SsSecuritiesInXray, 
			ECoreType.sing_box => Global.SsSecuritiesInSingbox, 
			_ => Global.SsSecuritiesInSingbox, 
		};
	}

	public ECoreType GetCoreType(ProfileItem profileItem, EConfigType eConfigType)
	{
		if (profileItem != null && profileItem.CoreType.HasValue)
		{
			return profileItem.CoreType.Value;
		}
		return (_config.CoreTypeItem?.FirstOrDefault((CoreTypeItem it) => it.ConfigType == eConfigType))?.CoreType ?? ECoreType.Xray;
	}
}

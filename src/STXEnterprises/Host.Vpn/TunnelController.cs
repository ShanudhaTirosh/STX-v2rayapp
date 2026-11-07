using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Handler;
using ServiceLib.Handler.Builder;
using ServiceLib.Handler.Fmt;
using ServiceLib.Handler.SysProxy;
using ServiceLib.Helper;
using ServiceLib.Manager;
using ServiceLib.Models.Configs;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Models.Dto;
using ServiceLib.Models.Entities;

namespace STXEnterprises.Host.Vpn;

public sealed class TunnelController
{
	public sealed record IncomingConfig(string Link, string? Title, string? Uuid, string? Status);

	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

	// Completed once InitAsync has finished. The web UI can call in before the cores are initialised; those calls wait here
	// instead of hitting a null config.
	private readonly TaskCompletionSource _ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	// Cancels a Connect that is still in progress (so Disconnect does not have to wait out 25 s of health checks).
	private CancellationTokenSource? _connectCts;

	// Cancels automatic reconnect attempts when the user disconnects or quits.
	private CancellationTokenSource? _recoverCts;

	private long _monitorSampleTick;

	private static readonly TimeSpan[] RecoverBackoff = { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(15) };

	/// <summary>Asked when a pinned server presents a different certificate. Returns true if the user accepts the new one.</summary>
	public Func<string, Task<bool>>? ConfirmCertificateChange { get; set; }

	private static string InstallDir => AppContext.BaseDirectory;

	private async Task EnsureReadyAsync()
	{
		try
		{
			await _ready.Task.WaitAsync(TimeSpan.FromSeconds(45));
		}
		catch (TimeoutException)
		{
			throw new TimeoutException("STX is still starting up. Try again in a moment.");
		}
	}

	private Config _config;

	private TunnelState _state;

	private string? _activeId;

	private string? _activeName;

	private string? _message;

	private long _upSpeed;

	private long _downSpeed;

	private long _sessionUp;

	private long _sessionDown;

	private long _connectedAt;

	public const int PingNoAnswer = -1;

	public const int PingNoAddress = -3;

	private static readonly TimeSpan WatchEvery = TimeSpan.FromSeconds(20L);

	private static readonly TimeSpan WatchGrace = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan WatchPatience = TimeSpan.FromSeconds(4L);

	private static readonly TimeSpan SettleAfterNetworkChange = TimeSpan.FromSeconds(6L);

	private const int FailuresBeforeActing = 3;

	private const int RepairsBeforeGivingUp = 1;

	private long _watchLastBytes;

	private CancellationTokenSource? _watchCts;

	private CancellationTokenSource? _watchWake;

	private Task? _watchTask;

	private readonly object _watchLock = new object();

	public bool Initialised { get; private set; }

	public bool TunAvailable => CoreAssets.HasSingBox;

	public string Mode
	{
		get
		{
			Config config = _config;
			if (config == null || !config.TunModeItem.EnableTun)
			{
				return "proxy";
			}
			return "tun";
		}
	}

	public int SocksPort => AppManager.Instance.GetLocalPort(EInboundProtocol.socks);

	public int HttpPort => AppManager.Instance.GetLocalPort(EInboundProtocol.mixed);

	public event Action<TunnelStatus>? StatusChanged;

	public event Action<string>? Notice;

	public async Task<string?> InitAsync()
	{
		if (!AppManager.Instance.InitApp())
		{
			string initProblem = "STX Enterprises couldn't read its settings. Reinstall to fix it.";
			_ready.TrySetException(new InvalidOperationException(initProblem));
			return initProblem;
		}
		AppManager.Instance.InitComponents();
		_config = AppManager.Instance.Config;
		await StxConfigTweaks.LoadAsync();
		await StxDnsRepair.ApplyAsync(_config);
		TrimCoreLogs();
		_config.CoreBasicItem.LogEnabled = true;
		_config.CoreBasicItem.Loglevel = "warning";
		_config.GuiItem.EnableStatistics = true;
		_config.GuiItem.DisplayRealTimeSpeed = true;
		_config.GuiItem.AutoUpdateInterval = 0;
		List<RoutingItem> list = await AppManager.Instance.RoutingItems();
		bool flag = ((list == null || list.Count == 0) ? true : false);
		bool flag2 = flag;
		_config.TunModeItem.EnableTun = TunAvailable;
		_config.Inbound.First().AllowLANConn = false;
		_config.Inbound.First().NewPort4LAN = false;
		_config.TunModeItem.EnableLegacyProtect = true;
		if (flag2)
		{
			_config.ConstItem.RouteRulesTemplateSourceUrl = string.Empty;
			await ConfigHandler.InitRouting(_config);
			Logging.SaveLog("Seeded the built-in routing rules for a new profile.");
		}
		await StxRoutingRepair.ApplyAsync(_config);
		await ConfigHandler.SaveConfig(_config);
		// A crash or kill while connected leaves Windows pointing at a proxy port nobody listens on. Undo that first.
		if (SysProxyHandler.RecoverStaleProxy(SocksPort))
		{
			Logging.SaveLog("startup: restored the system proxy left behind by an earlier session");
		}
		KillStrayCores();
		await CoreManager.Instance.Init(_config, OnCoreMessage);
		await StatisticsManager.Instance.Init(_config, OnStatistics);
		Initialised = true;
		_ready.TrySetResult();
		Publish();
		return null;
	}

	private static async Task<bool> ProxyAnsweringAsync(int port, TimeSpan within, CancellationToken ct = default)
	{
		DateTime deadline = DateTime.UtcNow + within;
		while (DateTime.UtcNow < deadline)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				using TcpClient probe = new TcpClient();
				Task attempt = probe.ConnectAsync(IPAddress.Loopback, port);
				if (await Task.WhenAny(attempt, Task.Delay(1000)) == attempt)
				{
					await attempt;
					return true;
				}
			}
			catch
			{
			}
			await Task.Delay(250, ct);
		}
		return false;
	}

	private static async Task<bool> NamesResolvingAsync(int port, TimeSpan within, CancellationToken ct = default)
	{
		List<Task<bool>> probes = new string[3] { "one.one.one.one", "www.cloudflare.com", "www.google.com" }.Select((string host) => SocksReachesAsync(port, host, 443, within, ct)).ToList();
		while (probes.Count > 0)
		{
			ct.ThrowIfCancellationRequested();
			Task<bool> task = await Task.WhenAny(probes);
			if (task.Result)
			{
				return true;
			}
			probes.Remove(task);
		}
		return false;
	}

	private static async Task<bool> SocksReachesAsync(int port, string host, int hostPort, TimeSpan within, CancellationToken outer = default)
	{
		_ = 7;
		checked
		{
			try
			{
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
				cts.CancelAfter(within);
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
				NetworkStream stream = client.GetStream();
				await stream.WriteAsync(new byte[3] { 5, 1, 0 }, cts.Token);
				byte[] greeting = new byte[2];
				await stream.ReadExactlyAsync(greeting, cts.Token);
				if (greeting[0] != 5 || greeting[1] != 0)
				{
					return false;
				}
				byte[] bytes = Encoding.ASCII.GetBytes(host);
				byte[] array = new byte[7 + bytes.Length];
				array[0] = 5;
				array[1] = 1;
				array[2] = 0;
				array[3] = 3;
				array[4] = (byte)bytes.Length;
				bytes.CopyTo(array, 5);
				array[5 + bytes.Length] = (byte)(hostPort >> 8);
				array[6 + bytes.Length] = (byte)(hostPort & 0xFF);
				await stream.WriteAsync(array, cts.Token);
				byte[] head = new byte[4];
				await stream.ReadExactlyAsync(head, cts.Token);
				if (head[0] != 5 || head[1] != 0)
				{
					return false;
				}
				int num = head[3] switch
				{
					1 => 4, 
					4 => 16, 
					3 => await ReadOneAsync(stream, cts.Token), 
					_ => -1, 
				};
				if (num < 0)
				{
					return false;
				}
				await stream.ReadExactlyAsync(new byte[num + 2], cts.Token);
				using SslStream tls = new SslStream(stream, leaveInnerStreamOpen: true, (object _, X509Certificate? _, X509Chain? _, SslPolicyErrors _) => true);
				await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = host
				}, cts.Token);
				return tls.IsAuthenticated;
			}
			catch
			{
				return false;
			}
		}
	}

	private static async Task<int> ReadOneAsync(NetworkStream stream, CancellationToken token)
	{
		byte[] one = new byte[1];
		await stream.ReadExactlyAsync(one, token);
		return one[0];
	}

	private static bool CoreRunning(string name)
	{
		return CoreProcesses.AnyRunning(name, InstallDir);
	}

	private static void KillStrayCores()
	{
		// Only processes whose executable is inside this install are touched. The previous version fell back to
		// "taskkill /IM xray.exe /IM sing-box.exe", which also killed the cores of any other VPN client on the PC.
		CoreProcesses.KillOurs(new[] { "xray", "sing-box" }, InstallDir, (string m) => Logging.SaveLog("stray cores: " + m));
	}

	private Task OnCoreMessage(bool notify, string msg)
	{
		if (msg.IsNullOrEmpty())
		{
			return Task.CompletedTask;
		}
		Logging.SaveLog("core: " + msg.Trim());
		if (notify)
		{
			Notice?.Invoke(msg);
		}
		return Task.CompletedTask;
	}

	private Task OnStatistics(ServerSpeedItem item)
	{
		// The traffic monitor already feeds the same counters; when it produced a sample in the last 3 s, ignore this
		// source, otherwise every byte is counted twice and the speed display flickers between two values.
		if (Environment.TickCount64 - Interlocked.Read(ref _monitorSampleTick) < 3000)
		{
			return Task.CompletedTask;
		}
		checked
		{
			long num = item.ProxyUp * 1024;
			long num2 = item.ProxyDown * 1024;
			_upSpeed = num;
			_downSpeed = num2;
			if (_state == TunnelState.Connected)
			{
				_sessionUp += num;
				_sessionDown += num2;
			}
			Publish();
			return Task.CompletedTask;
		}
	}

	public async Task<List<ConfigSummary>> ListConfigsAsync()
	{
		await EnsureReadyAsync();
		List<ProfileItem> source = (await AppManager.Instance.ProfileItems("")) ?? new List<ProfileItem>();
		string defaultId = _config.IndexId;
		return (from t in source
			where t.ConfigType == EConfigType.VLESS || t.ConfigType == EConfigType.VMess || t.ConfigType == EConfigType.Trojan || t.ConfigType == EConfigType.Shadowsocks
			select new ConfigSummary(t.IndexId, t.Remarks, t.Address, t.Port, t.Password ?? "", t.IndexId == defaultId, StxConfigTweaks.IsEdited(t.Password))).ToList();
	}

	public async Task<string?> ExportConfigAsync(string indexId)
	{
		await EnsureReadyAsync();
		ProfileItem profileItem = await AppManager.Instance.GetProfileItem(indexId);
		return (profileItem == null) ? null : FmtHandler.GetShareUri(profileItem);
	}

	public async Task<int> SyncConfigsAsync(IReadOnlyList<IncomingConfig> incoming)
	{
		await EnsureReadyAsync();
		List<ProfileItem> source = (await AppManager.Instance.ProfileItems("")) ?? new List<ProfileItem>();
		Dictionary<string, ProfileItem> byUuid = source.Where((ProfileItem t) => t.Password.IsNotEmpty()).GroupBy((ProfileItem t) => t.Password, StringComparer.OrdinalIgnoreCase).ToDictionary((IGrouping<string, ProfileItem> g) => g.Key, (IGrouping<string, ProfileItem> g) => g.First(), StringComparer.OrdinalIgnoreCase);
		int synced = 0;
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<ProfileItem> doomed = new List<ProfileItem>();
		checked
		{
			foreach (IncomingConfig item in incoming)
			{
				if (item.Link.IsNullOrEmpty())
				{
					continue;
				}
				string text = (item.Uuid.IsNotEmpty() ? item.Uuid : UuidOfLink(item.Link)) ?? "";
				if (!item.Status.IsNullOrEmpty() && !(item.Status == "active"))
				{
					if (text.IsNotEmpty() && byUuid.TryGetValue(text, out var value))
					{
						doomed.Add(value);
					}
					continue;
				}
				seen.Add(text);
				if (text.IsNotEmpty() && byUuid.TryGetValue(text, out var value2))
				{
					ProfileItem profileItem = FmtHandler.ResolveConfig(Named(item), out string _);
					if (profileItem != null)
					{
						profileItem.IndexId = value2.IndexId;
						profileItem.Subid = value2.Subid;
						// A refreshed link usually carries no pin; keep the one we learned as long as it is still the same server.
						if (profileItem.CertSha.IsNullOrEmpty() && value2.CertSha.IsNotEmpty() && string.Equals(profileItem.Address, value2.Address, StringComparison.OrdinalIgnoreCase) && profileItem.Port == value2.Port)
						{
							profileItem.CertSha = value2.CertSha;
						}
						StxConfigTweaks.Apply(profileItem);
						await ConfigHandler.AddServerCommon(_config, profileItem);
					}
					synced++;
					continue;
				}
				ProfileItem profileItem2 = FmtHandler.ResolveConfig(Named(item), out string msg2);
				if (profileItem2 == null)
				{
					Logging.SaveLog("sync: could not read a config (" + msg2 + ")");
					continue;
				}
				StxConfigTweaks.Apply(profileItem2);
				if (await ConfigHandler.AddServer(_config, profileItem2) == 0)
				{
					synced++;
				}
			}
			if (doomed.Count > 0)
			{
				List<ProfileItem> second = doomed.Where((ProfileItem t) => t.IndexId == _activeId).ToList();
				List<ProfileItem> list = doomed.Except(second).ToList();
				if (list.Count > 0)
				{
					await ConfigHandler.RemoveServers(_config, list);
				}
			}
			return synced;
		}
	}

	private static string Named(IncomingConfig row)
	{
		if (row.Title.IsNullOrEmpty())
		{
			return row.Link;
		}
		return (row.Link.Contains('#') ? row.Link.Substring(0, row.Link.IndexOf('#')) : row.Link) + "#" + Uri.EscapeDataString(row.Title);
	}

	private static string UuidOfLink(string link)
	{
		string text = (link.Contains("://") ? link.Substring(checked(link.IndexOf("://", StringComparison.Ordinal) + 3)) : link);
		int num = text.IndexOf('@');
		if (num <= 0)
		{
			return "";
		}
		return text.Substring(0, num).Split('?')[0].Trim();
	}

	public async Task<string?> ImportLinkAsync(string text)
	{
		await EnsureReadyAsync();
		ProfileItem profileItem = FmtHandler.ResolveConfig(text.Trim(), out string msg);
		if (profileItem == null)
		{
			return msg.IsNotEmpty() ? msg : "That doesn't look like a valid vless:// link.";
		}
		StxConfigTweaks.Apply(profileItem);
		return (await ConfigHandler.AddServer(_config, profileItem) == 0) ? null : "Couldn't save that config.";
	}

	public async Task<string?> RemoveConfigAsync(string indexId)
	{
		await EnsureReadyAsync();
		ProfileItem item = await AppManager.Instance.GetProfileItem(indexId);
		if (item == null)
		{
			return "That config is already gone.";
		}
		if (indexId == _activeId)
		{
			await DisconnectAsync();
		}
		await ConfigHandler.RemoveServers(_config, new List<ProfileItem>(1) { item });
		return null;
	}

	public async Task<ConfigDetail?> ConfigDetailAsync(string indexId)
	{
		await EnsureReadyAsync();
		ProfileItem profileItem = await AppManager.Instance.GetProfileItem(indexId);
		if (profileItem == null)
		{
			return null;
		}
		TransportExtraItem transportExtra = profileItem.GetTransportExtra();
		return new ConfigDetail(profileItem.IndexId, profileItem.Remarks, profileItem.ConfigType.ToString(), profileItem.GetNetwork(), profileItem.StreamSecurity.IsNullOrEmpty() ? "none" : profileItem.StreamSecurity, profileItem.Address ?? string.Empty, profileItem.Port, profileItem.Sni ?? string.Empty, Utils.ToBool(profileItem.AllowInsecure), profileItem.Fingerprint ?? string.Empty, profileItem.Alpn ?? string.Empty, transportExtra.Host ?? string.Empty, transportExtra.Path ?? string.Empty, StxConfigTweaks.IsEdited(profileItem.Password));
	}

	public async Task<string?> SetConfigDetailAsync(string indexId, StxConfigFields patch, bool reset)
	{
		await EnsureReadyAsync();
		ProfileItem profile = await AppManager.Instance.GetProfileItem(indexId);
		if (profile == null)
		{
			return "That config is no longer on this computer.";
		}
		if (reset)
		{
			StxConfigFields stxConfigFields = await StxConfigTweaks.ResetAsync(profile.Password);
			if (stxConfigFields == null)
			{
				return null;
			}
			StxConfigTweaks.Overwrite(profile, stxConfigFields);
			goto IL_02a7;
		}
		if (patch.Port.HasValue)
		{
			int? port = patch.Port;
			if (port.HasValue)
			{
				int valueOrDefault = port.GetValueOrDefault();
				if (valueOrDefault > 0 && valueOrDefault < 65536)
				{
					goto IL_018b;
				}
			}
			return "A port has to be between 1 and 65535.";
		}
		goto IL_018b;
		IL_02a7:
		await ConfigHandler.AddServerCommon(_config, profile);
		if (_state == TunnelState.Connected && indexId == _activeId)
		{
			return await ReconnectAsync();
		}
		return null;
		IL_018b:
		if (patch.Address != null && patch.Address.TrimEx().Length == 0)
		{
			return "A config needs a server address.";
		}
		await StxConfigTweaks.SetAsync(profile.Password, patch, StxConfigTweaks.Snapshot(profile));
		StxConfigTweaks.Overwrite(profile, patch);
		goto IL_02a7;
	}

	private static string? LastCoreError()
	{
		try
		{
			FileInfo fileInfo = (from f in new DirectoryInfo(Utils.GetLogPath()).GetFiles("Verror_*.txt")
				orderby f.LastWriteTime descending
				select f).FirstOrDefault();
			if (fileInfo == null)
			{
				return null;
			}
			using FileStream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using StreamReader streamReader = new StreamReader(stream);
			string text = (from l in streamReader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
				select l.Trim() into l
				where l.Length > 0
				select l).ToList().LastOrDefault((string l) => !l.Contains("started", StringComparison.OrdinalIgnoreCase));
			if (text == null)
			{
				return null;
			}
			int num = text.IndexOf(']');
			string text2 = checked((num > 0 && num < text.Length - 1) ? text.Substring(num + 1).Trim() : text);
			return (text2.Length > 300) ? (text2.Substring(0, 300) + "…") : text2;
		}
		catch
		{
			return null;
		}
	}

	public async Task<int> PingAsync(string indexId)
	{
		await EnsureReadyAsync();
		ProfileItem profile = await AppManager.Instance.GetProfileItem(indexId);
		if (profile == null || profile.Address.IsNullOrEmpty() || profile.Port <= 0)
		{
			return -3;
		}

		// 1. Direct probe with TLS SNI handshake if applicable
		int latency = await ProbeDirectAsync(profile);
		if (latency >= 0)
		{
			return latency;
		}

		// 2. SOCKS fallback if currently connected to the VPN
		if (_state == TunnelState.Connected)
		{
			latency = await ProbeSocksAsync(profile);
			if (latency >= 0)
			{
				return latency;
			}
		}

		return -1;
	}

	private static async Task<int> ProbeDirectAsync(ProfileItem profile)
	{
		IPAddress? iPAddress = await ResolveForProbeAsync(profile.Address);
		if (iPAddress == null)
		{
			return -3;
		}
		try
		{
			using TcpClient client = new TcpClient();
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3.5));
			Stopwatch clock = Stopwatch.StartNew();
			await client.ConnectAsync(iPAddress, profile.Port, cts.Token);

			bool isTls = profile.Port == 443 ||
						(profile.StreamSecurity != null && profile.StreamSecurity.Equals("tls", StringComparison.OrdinalIgnoreCase)) ||
						(profile.Security != null && profile.Security.Equals("tls", StringComparison.OrdinalIgnoreCase));

			if (isTls)
			{
				string sni = !string.IsNullOrEmpty(profile.Sni) ? profile.Sni :
							 (!string.IsNullOrEmpty(profile.RequestHost) ? profile.RequestHost : profile.Address);

				using SslStream tls = new SslStream(client.GetStream(), leaveInnerStreamOpen: false, (object _, X509Certificate? _, X509Chain? _, SslPolicyErrors _) => true);
				await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = sni
				}, cts.Token);
			}

			clock.Stop();
			return checked((int)clock.ElapsedMilliseconds);
		}
		catch
		{
			return -1;
		}
	}

	private async Task<int> ProbeSocksAsync(ProfileItem profile)
	{
		try
		{
			int port = SocksPort;
			if (port <= 0) port = 10808;

			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(4L));
			Stopwatch clock = Stopwatch.StartNew();
			using TcpClient client = new TcpClient();
			await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
			NetworkStream stream = client.GetStream();
			await stream.WriteAsync(new byte[3] { 5, 1, 0 }, cts.Token);
			byte[] greeting = new byte[2];
			await stream.ReadExactlyAsync(greeting, cts.Token);
			if (greeting[0] != 5 || greeting[1] != 0)
			{
				return -1;
			}
			byte[] bytes = Encoding.ASCII.GetBytes(profile.Address);
			byte[] array = new byte[7 + bytes.Length];
			array[0] = 5;
			array[1] = 1;
			array[2] = 0;
			array[3] = 3;
			array[4] = (byte)bytes.Length;
			bytes.CopyTo(array, 5);
			array[5 + bytes.Length] = (byte)(profile.Port >> 8);
			array[6 + bytes.Length] = (byte)(profile.Port & 0xFF);
			await stream.WriteAsync(array, cts.Token);
			byte[] head = new byte[4];
			await stream.ReadExactlyAsync(head, cts.Token);
			if (head[0] != 5 || head[1] != 0)
			{
				return -1;
			}
			clock.Stop();
			return checked((int)clock.ElapsedMilliseconds);
		}
		catch
		{
			return -1;
		}
	}

	private static async Task<IPAddress?> ResolveForProbeAsync(string address)
	{
		if (IPAddress.TryParse(address, out IPAddress? address2))
		{
			return address2;
		}
		try
		{
			string? resolved = await StxServerAddress.ResolveAsync(address);
			if (!string.IsNullOrEmpty(resolved) && IPAddress.TryParse(resolved, out IPAddress? dohIp))
			{
				return dohIp;
			}
		}
		catch
		{
		}
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0));
			IPAddress[] source = await Dns.GetHostAddressesAsync(address, cts.Token);
			IPAddress? iPAddress = source.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork) ?? source.FirstOrDefault();
			if (iPAddress != null)
			{
				return iPAddress;
			}
		}
		catch
		{
		}
		string text = StxServerAddress.Get(address);
		if (!string.IsNullOrEmpty(text) && IPAddress.TryParse(text, out IPAddress? address3))
		{
			return address3;
		}
		return null;
	}

	private static void TrimCoreLogs()
	{
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Utils.GetLogPath());
			if (!directoryInfo.Exists)
			{
				return;
			}
			foreach (FileInfo item in directoryInfo.GetFiles("sbox_*.txt").Concat(directoryInfo.GetFiles("Verror_*.txt")))
			{
				if (item.Length > 67108864)
				{
					long length = item.Length;
					item.Delete();
					Logging.SaveLog($"log: dropped {item.Name}, it had reached {length / 1024 / 1024} MB");
				}
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("log trim: " + ex.Message);
		}
	}

	public async Task<long> ClearCacheAsync()
	{
		long freed = 0L;
		string[] array = new string[3]
		{
			Utils.GetBinConfigPath(),
			Utils.GetLogPath(),
			Utils.GetTempPath()
		};
		checked
		{
			foreach (string text in array)
			{
				freed += Sweep(text, text == Utils.GetLogPath());
			}
			string path = Path.Combine(HostPaths.WebViewUserData, "EBWebView", "Default");
			array = new string[5] { "Cache", "Code Cache", "GPUCache", "DawnGraphiteCache", "DawnWebGPUCache" };
			foreach (string path2 in array)
			{
				freed += Sweep(Path.Combine(path, path2), keepNewest: false);
			}
			await Task.CompletedTask;
			return freed;
		}
	}

	private static long Sweep(string dir, bool keepNewest)
	{
		if (!Directory.Exists(dir))
		{
			return 0L;
		}
		long num = 0L;
		try
		{
			List<FileInfo> list = (from f in new DirectoryInfo(dir).GetFiles("*", SearchOption.AllDirectories)
				orderby f.LastWriteTime descending
				select f).ToList();
			IEnumerable<FileInfo> enumerable2;
			if (!keepNewest)
			{
				IEnumerable<FileInfo> enumerable = list;
				enumerable2 = enumerable;
			}
			else
			{
				enumerable2 = list.Skip(1);
			}
			foreach (FileInfo item in enumerable2)
			{
				try
				{
					long length = item.Length;
					item.Delete();
					num = checked(num + length);
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("clear cache " + dir + ": " + ex.Message);
		}
		return num;
	}

	public Task<string?> ConnectAsync(string indexId)
	{
		return ConnectUserAsync(indexId);
	}

	private async Task<string?> ConnectUserAsync(string indexId)
	{
		CancelRecovery();
		return await ConnectGatedAsync(indexId, reconnect: false);
	}

	/// <summary>Takes the gate and connects. With <paramref name="reconnect"/> the current tunnel is torn down inside the same lock hold.</summary>
	private async Task<string?> ConnectGatedAsync(string indexId, bool reconnect)
	{
		await EnsureReadyAsync();
		CoreAssets.CoreCheck coreCheck = await CoreAssets.VerifyXrayAsync();
		if (!coreCheck.Ok)
		{
			return coreCheck.Problem;
		}
		await _gate.WaitAsync();
		using CancellationTokenSource cts = new CancellationTokenSource();
		_connectCts = cts;
		try
		{
			if (reconnect)
			{
				await DisconnectCoreAsync();
			}
			return await ConnectCoreAsync(indexId, cts.Token);
		}
		finally
		{
			if (ReferenceEquals(_connectCts, cts))
			{
				_connectCts = null;
			}
			_gate.Release();
		}
	}

	private async Task<string?> ConnectCoreAsync(string indexId, CancellationToken ct)
	{
		try
		{
			ProfileItem profile = await AppManager.Instance.GetProfileItem(indexId);
			if (profile == null)
			{
				return "That config is no longer on this machine. Sync and try again.";
			}
			SetState(TunnelState.Connecting, indexId, profile.Remarks, null);
			await ConfigHandler.SetDefaultServerIndex(_config, indexId);
			KillStrayCores();
			ct.ThrowIfCancellationRequested();
			bool wantTun = _config.TunModeItem.EnableTun;
			Logging.SaveLog($"connect: {profile.Remarks} | tun={wantTun} | legacyProtect={_config.TunModeItem.EnableLegacyProtect}");
			await StxServerAddress.PinAsync(profile.Address);
			UplinkChoice uplink = await StxUplink.ChooseAsync(profile.Address, profile.Port, profile.Sni);
			StxServerAddress.SetDirectProbe(Utils.IsIpAddress(profile.Address) ? profile.Address : StxServerAddress.Get(profile.Address), profile.Port);
			ct.ThrowIfCancellationRequested();

			bool pinnedNow = await LearnServerPinAsync(profile, uplink);
			StartOutcome outcome = await StartCoresAsync(profile, uplink, ct);
			if (outcome.Error != null && pinnedNow)
			{
				// The pin we just learned may itself be the problem (e.g. the server hands out different certificates).
				// Fall back once to the old behaviour rather than leave the customer without a connection.
				Logging.SaveLog("connect: failed right after learning the server certificate; retrying once without the pin");
				await ClearPinAsync(profile);
				outcome = await StartCoresAsync(profile, uplink, ct);
			}
			else if (outcome.Error != null && profile.CertSha.IsNotEmpty())
			{
				string? changed = await ExplainCertificateChangeAsync(profile, uplink);
				if (changed == "retry")
				{
					outcome = await StartCoresAsync(profile, uplink, ct);
				}
				else if (changed != null)
				{
					outcome = outcome with { Error = changed };
				}
			}
			if (outcome.Error != null)
			{
				SetState(TunnelState.Disconnected, null, null, outcome.Error);
				return outcome.Error;
			}

			bool fellBack = false;
			if (wantTun && !outcome.SingBoxUp)
			{
				Logging.SaveLog("connect: sing-box did not start; falling back to system proxy");
				_config.TunModeItem.EnableTun = false;
				await ConfigHandler.SaveConfig(_config);
				fellBack = true;
			}
			if (!_config.TunModeItem.EnableTun)
			{
				_config.SystemProxyItem.SysProxyType = ESysProxyType.ForcedChange;
				await SysProxyHandler.UpdateSysProxy(_config, forceDisable: false);
				Logging.SaveLog($"connect: system proxy set to 127.0.0.1:{HttpPort}");
			}
			ct.ThrowIfCancellationRequested();
			_sessionUp = 0L;
			_sessionDown = 0L;
			SetState(TunnelState.Connected, indexId, profile.Remarks, null);
			if (fellBack)
			{
				Notice?.Invoke("Connected, but only for browsers and apps that follow the Windows proxy — the whole-PC adapter wouldn't start. Settings has the details.");
			}
			else if ((object)uplink != null && uplink.IsTunnel)
			{
				Notice?.Invoke("STX Enterprises is going out through " + uplink.Name + ", another VPN on this computer — it was the only connection that could reach the server. Your package may not be zero-rated while that is on.");
			}
			return null;
		}
		catch (OperationCanceledException)
		{
			Logging.SaveLog("connect: cancelled");
			await TearDownAfterFailureAsync();
			SetState(TunnelState.Disconnected, null, null, null);
			return null;
		}
		catch (Exception ex)
		{
			Logging.SaveLog("connect", ex);
			await TearDownAfterFailureAsync();
			SetState(TunnelState.Disconnected, null, null, ex.Message);
			return ex.Message;
		}
	}

	private sealed record StartOutcome(string? Error, bool XrayUp, bool SingBoxUp);

	/// <summary>Builds the config, starts the cores and proves the tunnel carries traffic. On failure the cores are stopped again.</summary>
	private async Task<StartOutcome> StartCoresAsync(ProfileItem profile, UplinkChoice? uplink, CancellationToken ct)
	{
		CoreConfigContextBuilderAllResult built = await CoreConfigContextBuilder.BuildAll(_config, profile);
		if (!built.Success)
		{
			string text = built.CombinedValidatorResult.Errors.FirstOrDefault() ?? "This config could not be prepared.";
			Logging.SaveLog("connect: build failed — " + text);
			return new StartOutcome(text, false, false);
		}
		Logging.SaveLog($"connect: preCore={built.PreSocksResult?.Context.RunCoreType.ToString() ?? "none"} mainCore={built.MainResult.Context.RunCoreType} mainTun={built.MainResult.Context.IsTunEnabled}");
		await CoreManager.Instance.LoadCore(built.MainResult.Context, built.PreSocksResult?.Context);
		ct.ThrowIfCancellationRequested();
		bool answering = await ProxyAnsweringAsync(SocksPort, TimeSpan.FromSeconds(8L), ct);
		bool xrayUp = CoreRunning("xray");
		bool singBoxUp = CoreRunning("sing-box");
		Logging.SaveLog($"connect: after load — xray={xrayUp} singBox={singBoxUp}");
		Logging.SaveLog($"connect: socks {SocksPort} answering={answering}");
		if (!xrayUp && !singBoxUp)
		{
			string text = "The VPN core wouldn't start. " + (LastCoreError() ?? "Check the log under Settings.");
			Logging.SaveLog("connect: " + text);
			await CoreManager.Instance.CoreStop();
			KillStrayCores();
			return new StartOutcome(text, false, false);
		}
		if (!answering)
		{
			await CoreManager.Instance.CoreStop();
			KillStrayCores();
			string text = "The VPN core started but isn't accepting connections. " + (LastCoreError() ?? "Check the log under Settings.");
			Logging.SaveLog("connect: " + text + " — tore the tunnel down rather than black-hole the machine");
			return new StartOutcome(text, xrayUp, singBoxUp);
		}
		bool resolving = await NamesResolvingAsync(SocksPort, TimeSpan.FromSeconds(15L), ct);
		Logging.SaveLog($"connect: names resolving through the proxy={resolving}");
		if (!resolving)
		{
			await CoreManager.Instance.CoreStop();
			KillStrayCores();
			string text = "Connected to the server, but nothing on this computer could look up a web address through it. Left on, that would take your internet away rather than protect it. Try another config, or Clear cache in Settings.";
			Logging.SaveLog("connect: " + text);
			return new StartOutcome(text, xrayUp, singBoxUp);
		}
		return new StartOutcome(null, xrayUp, singBoxUp);
	}

	private static bool IsTlsWithoutCertCheck(ProfileItem p)
	{
		return StxCertPin.Decide(p.StreamSecurity, p.AllowInsecure, p.CertSha, p.Cert, StxInsecureDefault.Enabled) == StxCertPin.Need.LearnPin;
	}

	private static IPAddress? UplinkAddress(UplinkChoice? uplink)
	{
		return (uplink != null && IPAddress.TryParse(uplink.Address, out IPAddress? a)) ? a : null;
	}

	/// <summary>Trust-on-first-use: remembers the server's certificate so later connections reject impostors. Returns true if a pin was added now.</summary>
	private async Task<bool> LearnServerPinAsync(ProfileItem profile, UplinkChoice? uplink)
	{
		if (!StxCertPin.Enabled || !IsTlsWithoutCertCheck(profile))
		{
			return false;
		}
		IPAddress? target = await ResolveForProbeAsync(profile.Address);
		if (target == null)
		{
			return false;
		}
		string sni = !string.IsNullOrEmpty(profile.Sni) ? profile.Sni : (!string.IsNullOrEmpty(profile.RequestHost) ? profile.RequestHost : profile.Address);
		string? hash = await StxCertPin.FetchLeafSha256Async(target, profile.Port, sni, UplinkAddress(uplink), TimeSpan.FromSeconds(5));
		if (hash == null)
		{
			Logging.SaveLog("certpin: could not learn the server certificate; this connection keeps the old behaviour");
			return false;
		}
		profile.CertSha = hash;
		await ConfigHandler.AddServerCommon(_config, profile);
		Logging.SaveLog("certpin: pinned the server certificate for " + profile.Remarks);
		return true;
	}

	private async Task ClearPinAsync(ProfileItem profile)
	{
		profile.CertSha = string.Empty;
		await ConfigHandler.AddServerCommon(_config, profile);
	}

	/// <summary>A pinned server failed. If its certificate really changed, tell the user (and let them accept it) instead of failing silently.</summary>
	private async Task<string?> ExplainCertificateChangeAsync(ProfileItem profile, UplinkChoice? uplink)
	{
		IPAddress? target = await ResolveForProbeAsync(profile.Address);
		if (target == null)
		{
			return null;
		}
		string sni = !string.IsNullOrEmpty(profile.Sni) ? profile.Sni : profile.Address;
		string? now = await StxCertPin.FetchLeafSha256Async(target, profile.Port, sni, UplinkAddress(uplink), TimeSpan.FromSeconds(5));
		if (now == null || StxCertPin.SameHash(now, profile.CertSha))
		{
			return null;
		}
		Logging.SaveLog("certpin: the server certificate no longer matches the saved one");
		Func<string, Task<bool>>? ask = ConfirmCertificateChange;
		if (ask != null && await ask(profile.Remarks ?? profile.Address))
		{
			profile.CertSha = now;
			await ConfigHandler.AddServerCommon(_config, profile);
			return "retry";
		}
		return "This server's security certificate has changed since you first connected. If you did not expect that, someone may be intercepting the connection, so STX did not connect. If the server was renewed, sync your configs or reset this config in its details, then connect again.";
	}

	private async Task TearDownAfterFailureAsync()
	{
		try
		{
			_config.SystemProxyItem.SysProxyType = ESysProxyType.ForcedClear;
			await SysProxyHandler.UpdateSysProxy(_config, forceDisable: true);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("teardown: clear system proxy", ex);
		}
		try
		{
			await CoreManager.Instance.CoreStop();
		}
		catch (Exception ex)
		{
			Logging.SaveLog("teardown: stop cores", ex);
		}
		KillStrayCores();
	}

	public async Task DisconnectAsync()
	{
		CancelRecovery();
		Interlocked.Exchange(ref _connectCts, null)?.Cancel();
		await _gate.WaitAsync();
		try
		{
			await DisconnectCoreAsync();
		}
		finally
		{
			_gate.Release();
		}
	}

	private async Task DisconnectCoreAsync()
	{
		_config.SystemProxyItem.SysProxyType = ESysProxyType.ForcedClear;
		try
		{
			await SysProxyHandler.UpdateSysProxy(_config, forceDisable: true);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("clear system proxy", ex);
		}
		await CoreManager.Instance.CoreStop();
		_upSpeed = (_downSpeed = 0L);
		SetState(TunnelState.Disconnected, null, null, null);
	}

	/// <summary>Synchronous best-effort cleanup for app exit / Windows sign-out: proxy restored, cores stopped.</summary>
	public void EmergencyCleanup()
	{
		try
		{
			CancelRecovery();
			StopWatchdog();
			Task.Run(async () =>
			{
				await TearDownAfterFailureAsync();
			}).Wait(TimeSpan.FromSeconds(5));
		}
		catch (Exception ex)
		{
			Logging.SaveLog("emergency cleanup", ex);
		}
	}

	private void CancelRecovery()
	{
		try
		{
			Interlocked.Exchange(ref _recoverCts, null)?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public async Task<string?> ApplyTcpFastOpenAsync(bool enabled)
	{
		StxTcpFastOpen.Set(enabled);
		return (_state != TunnelState.Disconnected) ? (await ReconnectAsync()) : null;
	}

	public async Task<string?> ApplyAdBlockAsync(bool enabled)
	{
		if (StxAdBlock.Enabled == enabled)
		{
			return null;
		}
		StxAdBlock.Set(enabled);
		return await ReconnectAsync();
	}

	public async Task<string?> ApplySplitTunnelAsync(string mode, IReadOnlyList<string> apps)
	{
		string text = mode.ToLowerInvariant();
		ESplitTunnelMode eSplitTunnelMode = ((text == "exclude") ? ESplitTunnelMode.Exclude : ((text == "include") ? ESplitTunnelMode.Include : ESplitTunnelMode.All));
		StxSplitTunnel.Set(eSplitTunnelMode, apps);
		if (eSplitTunnelMode != ESplitTunnelMode.All && !_config.TunModeItem.EnableTun)
		{
			return "Per-app routing needs whole-PC coverage, which is off. Turn it on to use this.";
		}
		return await ReconnectAsync();
	}

	/// <summary>Rebuilds the tunnel for the active config. Does nothing unless currently connected.</summary>
	public async Task<string?> ReconnectAsync()
	{
		string? id = _activeId;
		if (_state != TunnelState.Connected || id == null)
		{
			return null;
		}
		return await ConnectGatedAsync(id, reconnect: true);
	}

	public async Task<string?> SetModeAsync(string mode)
	{
		bool flag = mode.Equals("tun", StringComparison.OrdinalIgnoreCase);
		if (flag && !TunAvailable)
		{
			return "Whole-device mode needs a component this copy doesn't have. Reinstall STX Enterprises.";
		}
		_config.TunModeItem.EnableTun = flag;
		await ConfigHandler.SaveConfig(_config);
		if (_state == TunnelState.Connected && _activeId != null)
		{
			return await ReconnectAsync();
		}
		Publish();
		return null;
	}

	/// <summary>
	/// Automatic recovery after the watchdog declared the tunnel dead: up to three attempts with growing pauses,
	/// each reported to the user. Stops at once if the user disconnects.
	/// </summary>
	private async Task RecoverAsync(string id)
	{
		CancellationTokenSource cts = new CancellationTokenSource();
		Interlocked.Exchange(ref _recoverCts, cts)?.Cancel();
		string? lastError = null;
		try
		{
			for (int attempt = 1; attempt <= RecoverBackoff.Length; attempt++)
			{
				cts.Token.ThrowIfCancellationRequested();
				Notice?.Invoke($"The connection dropped. Reconnecting… (attempt {attempt} of {RecoverBackoff.Length})");
				Logging.SaveLog($"watchdog: rebuilding the tunnel (attempt {attempt})");
				lastError = await ConnectGatedAsync(id, reconnect: true);
				if (lastError == null && _state == TunnelState.Connected)
				{
					Notice?.Invoke("Reconnected.");
					return;
				}
				cts.Token.ThrowIfCancellationRequested();
				await Task.Delay(RecoverBackoff[attempt - 1], cts.Token);
			}
			Logging.SaveLog("watchdog: gave up reconnecting");
			Notice?.Invoke("Couldn't reconnect" + (lastError != null ? ": " + lastError : ".") + " The VPN is off so this PC keeps its internet. Connect again, or pick another server.");
			SetState(TunnelState.Disconnected, null, null, lastError);
		}
		catch (OperationCanceledException)
		{
			Logging.SaveLog("watchdog: reconnect cancelled by the user");
		}
		catch (Exception ex)
		{
			Logging.SaveLog("watchdog: recover", ex);
		}
		finally
		{
			Interlocked.CompareExchange(ref _recoverCts, null, cts);
			cts.Dispose();
		}
	}

	public TunnelStatus Snapshot()
	{
		return new TunnelStatus(_state, _activeId, _activeName, Mode, _upSpeed, _downSpeed, _sessionUp, _sessionDown, _connectedAt, _message);
	}

	private void SetState(TunnelState state, string? id, string? name, string? message)
	{
		if (state == TunnelState.Connected && _state != TunnelState.Connected)
		{
			_connectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}
		else if (state != TunnelState.Connected)
		{
			_connectedAt = 0L;
		}
		bool flag = _state == TunnelState.Connected;
		_state = state;
		if (state == TunnelState.Connected && !flag)
		{
			StartWatchdog();
			StartTrafficMonitor();
		}
		else if ((state != TunnelState.Connected) & flag)
		{
			StopWatchdog();
			StopTrafficMonitor();
		}
		_activeId = ((state == TunnelState.Disconnected) ? null : id);
		_activeName = ((state == TunnelState.Disconnected) ? null : name);
		_message = message;
		Publish();
	}

	private void Publish()
	{
		StatusChanged?.Invoke(Snapshot());
	}

	private void StartWatchdog()
	{
		lock (_watchLock)
		{
			StopWatchdogLocked();
			_watchLastBytes = 0L;
			CancellationTokenSource cts = new CancellationTokenSource();
			_watchCts = cts;
			NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
			_watchTask = Task.Run(() => WatchAsync(cts.Token), CancellationToken.None);
		}
	}

	private void StopWatchdog()
	{
		lock (_watchLock)
		{
			StopWatchdogLocked();
		}
	}

	private void StopWatchdogLocked()
	{
		if (_watchCts != null)
		{
			NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
			try
			{
				_watchCts.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			_watchCts.Dispose();
			_watchCts = null;
			_watchTask = null;
		}
	}

	private CancellationTokenSource? _trafficCts;
	private Task? _trafficTask;

	private void StartTrafficMonitor()
	{
		lock (_watchLock)
		{
			StopTrafficMonitorLocked();
			_trafficCts = new CancellationTokenSource();
			CancellationToken token = _trafficCts.Token;
			_trafficTask = Task.Run(() => TrafficMonitorAsync(token), CancellationToken.None);
		}
	}

	private void StopTrafficMonitor()
	{
		lock (_watchLock)
		{
			StopTrafficMonitorLocked();
		}
	}

	private void StopTrafficMonitorLocked()
	{
		try
		{
			_trafficCts?.Cancel();
			_trafficCts?.Dispose();
			_trafficCts = null;
			_trafficTask = null;
			_upSpeed = 0L;
			_downSpeed = 0L;
			Publish();
		}
		catch
		{
		}
	}

	private async Task TrafficMonitorAsync(CancellationToken token)
	{
		long prevUp = 0L;
		long prevDown = 0L;
		bool hasInitialSample = false;
		NetworkInterface? tunIface = null;

		while (!token.IsCancellationRequested && _state == TunnelState.Connected)
		{
			try
			{
				await Task.Delay(1000, token);
				if (token.IsCancellationRequested || _state != TunnelState.Connected)
				{
					break;
				}

				long currentTotalUp = 0L;
				long currentTotalDown = 0L;
				bool sampled = false;

				// 1. Kernel NDIS network interface statistics (Wintun / sing-tun)
				try
				{
					if (tunIface == null || tunIface.OperationalStatus != OperationalStatus.Up)
					{
						tunIface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault((NetworkInterface i) =>
							(i.Name != null && (i.Name.StartsWith("STX", StringComparison.OrdinalIgnoreCase) || i.Name.Contains("wintun", StringComparison.OrdinalIgnoreCase))) ||
							(i.Description != null && (i.Description.Contains("sing-tun", StringComparison.OrdinalIgnoreCase) || i.Description.Contains("Wintun", StringComparison.OrdinalIgnoreCase) || i.Description.Contains("STX", StringComparison.OrdinalIgnoreCase))));
					}
					if (tunIface != null)
					{
						IPv4InterfaceStatistics stats = tunIface.GetIPv4Statistics();
						currentTotalDown = stats.BytesReceived;
						currentTotalUp = stats.BytesSent;
						sampled = true;
					}
				}
				catch
				{
				}

				// 2. Sing-box Clash API fallback
				if (!sampled && AppManager.Instance.RunningCoreType == ECoreType.sing_box)
				{
					try
					{
						int clashPort = AppManager.Instance.StatePort2;
						if (clashPort > 0)
						{
							string text = await HttpClientHelper.Instance.TryGetAsync($"http://127.0.0.1:{clashPort}/connections");
							if (!string.IsNullOrEmpty(text))
							{
								JsonNode? jsonNode = JsonNode.Parse(text);
								if (jsonNode != null)
								{
									currentTotalDown = jsonNode["downloadTotal"]?.GetValue<long>() ?? 0L;
									currentTotalUp = jsonNode["uploadTotal"]?.GetValue<long>() ?? 0L;
									sampled = true;
								}
							}
						}
					}
					catch
					{
					}
				}

				// 3. Xray debug/vars fallback
				if (!sampled && AppManager.Instance.RunningCoreType == ECoreType.Xray)
				{
					try
					{
						string text2 = await HttpClientHelper.Instance.TryGetAsync($"http://127.0.0.1:{AppManager.Instance.StatePort}/debug/vars");
						if (!string.IsNullOrEmpty(text2))
						{
							V2rayMetricsVars? vars = JsonUtils.Deserialize<V2rayMetricsVars>(text2);
							if (vars?.stats?.outbound != null)
							{
								long xUp = 0L;
								long xDown = 0L;
								foreach (string k in vars.stats.outbound.Keys.Cast<string>())
								{
									if (k.StartsWith("proxy"))
									{
										V2rayMetricsVarsLink? link = JsonUtils.Deserialize<V2rayMetricsVarsLink>(vars.stats.outbound[k]?.ToString() ?? "{}");
										if (link != null)
										{
											xUp += link.uplink;
											xDown += link.downlink;
										}
									}
								}
								currentTotalUp = xUp;
								currentTotalDown = xDown;
								sampled = true;
							}
						}
					}
					catch
					{
					}
				}

				if (sampled)
				{
					if (!hasInitialSample)
					{
						prevUp = currentTotalUp;
						prevDown = currentTotalDown;
						hasInitialSample = true;
					}
					else
					{
						long deltaUp = Math.Max(0L, currentTotalUp - prevUp);
						long deltaDown = Math.Max(0L, currentTotalDown - prevDown);
						prevUp = currentTotalUp;
						prevDown = currentTotalDown;

						_upSpeed = deltaUp;
						_downSpeed = deltaDown;
						_sessionUp += deltaUp;
						_sessionDown += deltaDown;
						Interlocked.Exchange(ref _monitorSampleTick, Environment.TickCount64);

						Publish();
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Logging.SaveLog("traffic monitor: " + ex.Message);
			}
		}
	}

	private void OnNetworkAddressChanged(object? sender, EventArgs e)
	{
		Logging.SaveLog("watchdog: the machine's addresses changed; re-checking the tunnel");
		WakeWatchdog();
	}

	private void WakeWatchdog()
	{
		CancellationTokenSource watchWake;
		lock (_watchLock)
		{
			watchWake = _watchWake;
		}
		try
		{
			watchWake?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async Task WatchAsync(CancellationToken token)
	{
		int failures = 0;
		int repairs = 0;
		TimeSpan wait = WatchGrace;
		checked
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					bool flag = await SleepAsync(wait, token);
					if (token.IsCancellationRequested)
					{
						break;
					}
					wait = WatchEvery;
					if (flag)
					{
						await SleepAsync(SettleAfterNetworkChange, token);
					}
					if (_state != TunnelState.Connected)
					{
						break;
					}
					long num = _sessionUp + _sessionDown;
					bool flag2 = num > _watchLastBytes;
					_watchLastBytes = num;
					if (flag2)
					{
						if (failures > 0)
						{
							Logging.SaveLog("watchdog: traffic is flowing again");
						}
						failures = 0;
						repairs = 0;
						continue;
					}
					if (await TunnelAliveAsync())
					{
						if (failures > 0)
						{
							Logging.SaveLog("watchdog: the tunnel answered again");
						}
						failures = 0;
						repairs = 0;
						continue;
					}
					failures++;
					int needed = flag ? 2 : 3;
					Logging.SaveLog($"watchdog: the tunnel did not answer ({failures}/{needed})");
					if (failures < needed || token.IsCancellationRequested)
					{
						if (flag)
						{
							wait = TimeSpan.FromSeconds(3L);
						}
						continue;
					}
					if (!token.IsCancellationRequested && _state == TunnelState.Connected && _activeId != null)
					{
						string recoverId = _activeId;
						_ = Task.Run(() => RecoverAsync(recoverId));
					}
					break;
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				Logging.SaveLog("watchdog", ex2);
			}
		}
	}

	private async Task<bool> SleepAsync(TimeSpan howLong, CancellationToken token)
	{
		CancellationTokenSource wake = CancellationTokenSource.CreateLinkedTokenSource(token);
		lock (_watchLock)
		{
			_watchWake = wake;
		}
		try
		{
			await Task.Delay(howLong, wake.Token);
			return false;
		}
		catch (OperationCanceledException)
		{
			token.ThrowIfCancellationRequested();
			return true;
		}
		finally
		{
			lock (_watchLock)
			{
				if (_watchWake == wake)
				{
					_watchWake = null;
				}
			}
			wake.Dispose();
		}
	}

	private async Task<bool> TunnelAliveAsync()
	{
		try
		{
			return await NamesResolvingAsync(SocksPort, WatchPatience);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("watchdog: check failed", ex);
			return false;
		}
	}

	private async Task GiveUpAsync()
	{
		Logging.SaveLog("watchdog: a rebuilt tunnel is still not passing traffic; taking the adapter down");
		try
		{
			await DisconnectAsync();
		}
		catch (Exception ex)
		{
			Logging.SaveLog("watchdog: disconnect", ex);
		}
		Notice?.Invoke("Your connection stopped passing traffic and reconnecting didn't fix it, so the VPN has been switched off to give this PC its internet back. Try connecting again, or pick another server.");
	}
}

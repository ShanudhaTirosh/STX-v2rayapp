using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ServiceLib.Common;

namespace ServiceLib.Handler.SysProxy;

/// <summary>What the Windows "Internet Settings" proxy looked like before STX touched it.</summary>
public sealed class ProxySnapshot
{
	public Dictionary<string, string?> Strings { get; set; } = new Dictionary<string, string?>();

	public Dictionary<string, int?> Numbers { get; set; } = new Dictionary<string, int?>();

	/// <summary>Binary registry values (DefaultConnectionSettings holds the "auto-detect" and PAC flags), base64.</summary>
	public Dictionary<string, string?> Blobs { get; set; } = new Dictionary<string, string?>();

	public string? ProxyServer => Strings.TryGetValue("ProxyServer", out string? v) ? v : null;

	public bool ProxyEnabled => Numbers.TryGetValue("ProxyEnable", out int? v) && v == 1;
}

public interface IProxyStore
{
	ProxySnapshot Read();

	void Apply(ProxySnapshot snapshot);

	/// <summary>Turns the system proxy off completely (direct connection).</summary>
	void Clear();
}

/// <summary>
/// Makes the system-proxy change reversible and crash-safe.
///
/// Before STX points Windows at its local proxy it saves what was there (a company PAC script, an
/// existing proxy, "auto-detect"). Disconnect puts that back instead of blindly switching to "direct".
/// If the app is killed or crashes while connected, the saved copy is still on disk, and the next start
/// (or "STXEnterprises.exe --reset-proxy") restores it - otherwise Windows keeps pointing at a dead local
/// port and the whole PC appears to have no internet.
/// </summary>
public sealed class SystemProxyGuard
{
	private readonly IProxyStore _store;

	private readonly string _backupPath;

	private readonly object _lock = new object();

	public SystemProxyGuard(IProxyStore store, string backupPath)
	{
		_store = store;
		_backupPath = backupPath;
	}

	public bool HasBackup => File.Exists(_backupPath);

	/// <summary>Call right before the proxy is switched to <paramref name="ourServer"/> (e.g. "127.0.0.1:10808").</summary>
	public void Capture(string ourServer)
	{
		lock (_lock)
		{
			if (File.Exists(_backupPath))
			{
				return; // an earlier, un-restored session already holds the user's real settings; never overwrite them with ours
			}
			ProxySnapshot current = _store.Read();
			if (current.ProxyEnabled && IsOurs(current.ProxyServer, ourServer))
			{
				// Our proxy is already set but nothing was saved (the backup was lost): the original is unknowable, assume "direct".
				current = new ProxySnapshot();
			}
			AtomicFile.WriteAllText(_backupPath, JsonSerializer.Serialize(current));
		}
	}

	/// <summary>Restores the saved settings. Returns false when there was nothing saved (caller should clear the proxy instead).</summary>
	public bool Restore()
	{
		lock (_lock)
		{
			if (!File.Exists(_backupPath))
			{
				return false;
			}
			ProxySnapshot? snapshot = null;
			try
			{
				snapshot = JsonSerializer.Deserialize<ProxySnapshot>(File.ReadAllText(_backupPath));
			}
			catch (Exception ex)
			{
				Logging.SaveLog("proxy backup unreadable, clearing the proxy instead", ex);
			}
			if (snapshot == null)
			{
				_store.Clear();
			}
			else if (snapshot.Strings.Count == 0 && snapshot.Numbers.Count == 0 && snapshot.Blobs.Count == 0)
			{
				_store.Clear();
			}
			else
			{
				_store.Apply(snapshot);
			}
			TryDelete(_backupPath);
			return true;
		}
	}

	/// <summary>
	/// Startup / --reset-proxy: undo a proxy left behind by a session that never disconnected.
	/// <paramref name="isOurProxy"/> says whether a ProxyServer value belongs to STX (a loopback address).
	/// </summary>
	public bool RecoverStale(Func<string?, bool> isOurProxy)
	{
		lock (_lock)
		{
			if (File.Exists(_backupPath))
			{
				Restore();
				return true;
			}
			ProxySnapshot current = _store.Read();
			if (current.ProxyEnabled && isOurProxy(current.ProxyServer))
			{
				_store.Clear();
				return true;
			}
			return false;
		}
	}

	public static bool IsLoopbackProxy(string? proxyServer)
	{
		if (string.IsNullOrWhiteSpace(proxyServer))
		{
			return false;
		}
		string s = proxyServer.Trim();
		// Forms: "127.0.0.1:10808", "http=127.0.0.1:10808;https=127.0.0.1:10808", "socks=127.0.0.1:10808"
		foreach (string part in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			string hostPort = part.Contains('=') ? part.Substring(part.IndexOf('=') + 1) : part;
			hostPort = hostPort.Replace("http://", "", StringComparison.OrdinalIgnoreCase).Replace("socks5://", "", StringComparison.OrdinalIgnoreCase).Trim();
			if (hostPort.StartsWith("127.", StringComparison.Ordinal) || hostPort.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || hostPort.StartsWith("[::1]", StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>True when the proxy value is a loopback address on exactly this port (i.e. it is STX's own listener).</summary>
	public static bool IsLoopbackProxyOnPort(string? proxyServer, int port)
	{
		if (!IsLoopbackProxy(proxyServer))
		{
			return false;
		}
		foreach (string part in proxyServer!.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			string hostPort = (part.Contains('=') ? part.Substring(part.IndexOf('=') + 1) : part).Trim();
			if (hostPort.EndsWith(":" + port, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsOurs(string? current, string ours)
	{
		return !string.IsNullOrEmpty(current) && (current.Contains(ours, StringComparison.OrdinalIgnoreCase) || IsLoopbackProxy(current));
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
		}
	}
}

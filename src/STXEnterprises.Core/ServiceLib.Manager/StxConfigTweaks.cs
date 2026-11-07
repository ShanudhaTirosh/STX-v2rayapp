using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Models.Entities;

namespace ServiceLib.Manager;

public static class StxConfigTweaks
{
	private const string FileName = "stx-config-tweaks.json";
	private const string LegacyFileName = "pxn-config-tweaks.json";

	private static readonly ConcurrentDictionary<string, StxConfigTweak> _tweaks = new ConcurrentDictionary<string, StxConfigTweak>(StringComparer.OrdinalIgnoreCase);

	private static readonly SemaphoreSlim _io = new SemaphoreSlim(1, 1);

	private static bool _loaded;

	public static StxConfigTweak? For(string? uuid)
	{
		if (!uuid.IsNullOrEmpty())
		{
			return _tweaks.GetValueOrDefault(uuid);
		}
		return null;
	}

	public static bool IsEdited(string? uuid)
	{
		StxConfigTweak? stxConfigTweak = For(uuid);
		if (stxConfigTweak == null)
		{
			return false;
		}
		return !stxConfigTweak.Changed.IsEmpty;
	}

	public static async Task LoadAsync()
	{
		if (_loaded)
		{
			return;
		}
		await _io.WaitAsync();
		try
		{
			if (_loaded)
			{
				return;
			}
			_loaded = true;
			string configPath = Utils.GetConfigPath(FileName);
			if (!File.Exists(configPath))
			{
				string legacyPath = Utils.GetConfigPath(LegacyFileName);
				if (File.Exists(legacyPath))
				{
					configPath = legacyPath;
				}
				else
				{
					return;
				}
			}
			foreach (KeyValuePair<string, StxConfigTweak> item in JsonUtils.Deserialize<Dictionary<string, StxConfigTweak>>(await File.ReadAllTextAsync(configPath)) ?? new Dictionary<string, StxConfigTweak>())
			{
				if (item.Key.IsNotEmpty() && item.Value != null)
				{
					_tweaks[item.Key] = item.Value;
				}
			}
			int num = Enumerable.Count(_tweaks, (KeyValuePair<string, StxConfigTweak> t) => !t.Value.Changed.IsEmpty);
			if (num > 0)
			{
				Logging.SaveLog($"tweaks: {num} config(s) carry settings the customer changed");
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("tweaks: could not read the file; every config keeps what its link says", ex);
		}
		finally
		{
			_io.Release();
		}
	}

	public static StxConfigFields Snapshot(ProfileItem profile)
	{
		TransportExtraItem transportExtra = profile.GetTransportExtra();
		return new StxConfigFields
		{
			Address = (profile.Address ?? string.Empty),
			Port = profile.Port,
			Sni = (profile.Sni ?? string.Empty),
			AllowInsecure = (profile.AllowInsecure ?? string.Empty),
			Fingerprint = (profile.Fingerprint ?? string.Empty),
			Alpn = (profile.Alpn ?? string.Empty),
			Host = (transportExtra.Host ?? string.Empty),
			Path = (transportExtra.Path ?? string.Empty)
		};
	}

	public static async Task SetAsync(string? uuid, StxConfigFields patch, StxConfigFields? issued)
	{
		if (!uuid.IsNullOrEmpty())
		{
			await LoadAsync();
			StxConfigTweak orAdd;
			StxConfigTweak stxConfigTweak = (orAdd = _tweaks.GetOrAdd(uuid, (string _) => new StxConfigTweak()));
			if (orAdd.Issued == null)
			{
				orAdd.Issued = issued;
			}
			StxConfigFields changed = stxConfigTweak.Changed;
			if (patch.Address != null)
			{
				changed.Address = patch.Address;
			}
			if (patch.Port.HasValue)
			{
				changed.Port = patch.Port;
			}
			if (patch.Sni != null)
			{
				changed.Sni = patch.Sni;
			}
			if (patch.AllowInsecure != null)
			{
				changed.AllowInsecure = patch.AllowInsecure;
			}
			if (patch.Fingerprint != null)
			{
				changed.Fingerprint = patch.Fingerprint;
			}
			if (patch.Alpn != null)
			{
				changed.Alpn = patch.Alpn;
			}
			if (patch.Host != null)
			{
				changed.Host = patch.Host;
			}
			if (patch.Path != null)
			{
				changed.Path = patch.Path;
			}
			await SaveAsync();
		}
	}

	public static async Task<StxConfigFields?> ResetAsync(string? uuid)
	{
		if (uuid.IsNullOrEmpty() || !_tweaks.TryRemove(uuid, out StxConfigTweak tweak))
		{
			return null;
		}
		await SaveAsync();
		return tweak.Issued;
	}

	public static void Apply(ProfileItem? profile)
	{
		if (profile != null)
		{
			StxConfigTweak stxConfigTweak = For(profile.Password);
			if (stxConfigTweak != null)
			{
				stxConfigTweak.Issued = Snapshot(profile);
				Overwrite(profile, stxConfigTweak.Changed);
			}
		}
	}

	public static void Overwrite(ProfileItem profile, StxConfigFields fields)
	{
		if (fields.Address.IsNotEmpty())
		{
			profile.Address = fields.Address.TrimEx();
		}
		int? port = fields.Port;
		if (port.HasValue)
		{
			int valueOrDefault = port.GetValueOrDefault();
			if (valueOrDefault > 0 && valueOrDefault < 65536)
			{
				profile.Port = fields.Port.Value;
			}
		}
		if (fields.Sni != null)
		{
			profile.Sni = fields.Sni.TrimEx();
		}
		if (fields.AllowInsecure != null)
		{
			profile.AllowInsecure = fields.AllowInsecure.TrimEx();
		}
		if (fields.Fingerprint != null)
		{
			profile.Fingerprint = fields.Fingerprint.TrimEx();
		}
		if (fields.Alpn != null)
		{
			profile.Alpn = fields.Alpn.TrimEx();
		}
		if (fields.Host != null || fields.Path != null)
		{
			TransportExtraItem transportExtra = profile.GetTransportExtra();
			profile.SetTransportExtra(transportExtra with
			{
				Host = ((fields.Host == null) ? transportExtra.Host : fields.Host.TrimEx()),
				Path = ((fields.Path == null) ? transportExtra.Path : fields.Path.TrimEx())
			});
		}
	}

	private static async Task SaveAsync()
	{
		await _io.WaitAsync();
		try
		{
			await File.WriteAllTextAsync(Utils.GetConfigPath(FileName), JsonUtils.Serialize(new Dictionary<string, StxConfigTweak>(_tweaks)));
		}
		catch (Exception ex)
		{
			Logging.SaveLog("tweaks: could not write the file", ex);
		}
		finally
		{
			_io.Release();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ServiceLib.Handler.SysProxy;

/// <summary>Reads and writes the per-user WinINet proxy settings in the registry.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsProxyStore : IProxyStore
{
	private const string KeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";

	private static readonly string[] StringValues = { "ProxyServer", "ProxyOverride", "AutoConfigURL" };

	private static readonly string[] NumberValues = { "ProxyEnable" };

	private static readonly string[] BlobValues = { "DefaultConnectionSettings", "SavedLegacySettings" };

	public ProxySnapshot Read()
	{
		ProxySnapshot snapshot = new ProxySnapshot();
		using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
		if (key == null)
		{
			return snapshot;
		}
		foreach (string name in StringValues)
		{
			snapshot.Strings[name] = key.GetValue(name) as string;
		}
		foreach (string name in NumberValues)
		{
			snapshot.Numbers[name] = key.GetValue(name) is int i ? i : (int?)null;
		}
		foreach (string name in BlobValues)
		{
			snapshot.Blobs[name] = key.GetValue(name) is byte[] b ? Convert.ToBase64String(b) : null;
		}
		return snapshot;
	}

	public void Apply(ProxySnapshot snapshot)
	{
		using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
		foreach (KeyValuePair<string, string?> kv in snapshot.Strings)
		{
			Write(key, kv.Key, kv.Value, RegistryValueKind.String);
		}
		foreach (KeyValuePair<string, int?> kv in snapshot.Numbers)
		{
			if (kv.Value.HasValue)
			{
				key.SetValue(kv.Key, kv.Value.Value, RegistryValueKind.DWord);
			}
			else
			{
				DeleteQuiet(key, kv.Key);
			}
		}
		foreach (KeyValuePair<string, string?> kv in snapshot.Blobs)
		{
			if (kv.Value != null)
			{
				key.SetValue(kv.Key, Convert.FromBase64String(kv.Value), RegistryValueKind.Binary);
			}
			else
			{
				DeleteQuiet(key, kv.Key);
			}
		}
		Notify();
	}

	public void Clear()
	{
		ProxySettingWindows.UnsetProxy();
	}

	private static void Write(RegistryKey key, string name, string? value, RegistryValueKind kind)
	{
		if (value == null)
		{
			DeleteQuiet(key, name);
		}
		else
		{
			key.SetValue(name, value, kind);
		}
	}

	private static void DeleteQuiet(RegistryKey key, string name)
	{
		try
		{
			key.DeleteValue(name, throwOnMissingValue: false);
		}
		catch
		{
		}
	}

	private static void Notify()
	{
		ProxySettingWindows.NativeMethods.InternetSetOption(IntPtr.Zero, ProxySettingWindows.InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
		ProxySettingWindows.NativeMethods.InternetSetOption(IntPtr.Zero, ProxySettingWindows.InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
	}
}

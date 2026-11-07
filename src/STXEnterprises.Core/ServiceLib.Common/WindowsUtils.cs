using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ServiceLib.Common;

[SupportedOSPlatform("windows")]
internal static class WindowsUtils
{
	private static readonly string _tag = "WindowsUtils";

	public static string? RegReadValue(string path, string name, string def)
	{
		RegistryKey registryKey = null;
		try
		{
			registryKey = Registry.CurrentUser.OpenSubKey(path, writable: false);
			string text = registryKey?.GetValue(name) as string;
			return text.IsNullOrEmpty() ? def : text;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return def;
		}
		finally
		{
			registryKey?.Close();
		}
	}

	public static void RegWriteValue(string path, string name, object value)
	{
		RegistryKey registryKey = null;
		try
		{
			registryKey = Registry.CurrentUser.CreateSubKey(path);
			if (value.ToString().IsNullOrEmpty())
			{
				registryKey?.DeleteValue(name, throwOnMissingValue: false);
			}
			else
			{
				registryKey?.SetValue(name, value);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		finally
		{
			registryKey?.Close();
		}
	}

	public static async Task RemoveTunDevice()
	{
		List<string> list = new List<string> { "wintunSTX Enterprises TUN", "wintunPXN Shield TUN", "wintunsingbox_tun", "xray_tun" };
		foreach (string item in list)
		{
			try
			{
				byte[] b = MD5.HashData(Encoding.UTF8.GetBytes(item));
				Guid value = new Guid(b);
				string arg = $" /remove-device  \"SWD\\Wintun\\{{{value}}}\" ";
				await Utils.GetCliWrapOutput("C:\\Windows\\System32\\pnputil.exe", arg);
			}
			catch (Exception ex)
			{
				Logging.SaveLog(_tag, ex);
			}
		}
	}
}

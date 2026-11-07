using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ServiceLib.Common;

namespace STXEnterprises.Host.Vpn;

public static class AppInventory
{
	private static readonly HashSet<string> Hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"stxenterprises", "stxclient", "stxv2rayclient", "pxnshield", "xray", "sing-box", "explorer", "svchost", "csrss", "wininit", "winlogon", "services", "lsass",
		"smss", "dwm", "fontdrvhost", "sihost", "taskhostw", "ctfmon", "runtimebroker", "searchhost", "startmenuexperiencehost", "shellexperiencehost",
		"textinputhost", "applicationframehost", "systemsettings", "registry", "memory compression", "idle", "system", "audiodg", "conhost", "dllhost",
		"spoolsv", "wudfhost"
	};

	public static List<InstalledApp> List()
	{
		Dictionary<string, InstalledApp> dictionary = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
		foreach (InstalledApp item in FromStartMenu().Concat(FromRunning()))
		{
			if (!Hidden.Contains(item.Process))
			{
				dictionary.TryAdd(item.Process, item);
			}
		}
		return dictionary.Values.OrderBy((InstalledApp a) => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	private static IEnumerable<InstalledApp> FromStartMenu()
	{
		string[] source = new string[2]
		{
			Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
			Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
		};
		foreach (string item in source.Where(Directory.Exists))
		{
			IEnumerable<string> enumerable;
			try
			{
				enumerable = Directory.EnumerateFiles(item, "*.lnk", SearchOption.AllDirectories);
			}
			catch
			{
				continue;
			}
			foreach (string item2 in enumerable)
			{
				string text = null;
				try
				{
					text = ResolveShortcut(item2);
				}
				catch
				{
				}
				if (!text.IsNullOrEmpty() && text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(text))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2);
					if (!fileNameWithoutExtension.StartsWith("uninstall", StringComparison.OrdinalIgnoreCase))
					{
						yield return new InstalledApp(fileNameWithoutExtension, Path.GetFileNameWithoutExtension(text), SafeCompany(text));
					}
				}
			}
		}
	}

	private static IEnumerable<InstalledApp> FromRunning()
	{
		Process[] processes;
		try
		{
			processes = Process.GetProcesses();
		}
		catch
		{
			yield break;
		}
		Process[] array = processes;
		foreach (Process process in array)
		{
			string text = null;
			try
			{
				text = process.MainModule?.FileName;
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
			if (!text.IsNullOrEmpty())
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				yield return new InstalledApp(SafeProductName(text) ?? fileNameWithoutExtension, fileNameWithoutExtension, SafeCompany(text));
			}
		}
	}

	private static string? SafeProductName(string path)
	{
		try
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
			return versionInfo.FileDescription.IsNotEmpty() ? versionInfo.FileDescription : versionInfo.ProductName.NullIfEmpty();
		}
		catch
		{
			return null;
		}
	}

	private static string? SafeCompany(string path)
	{
		try
		{
			return FileVersionInfo.GetVersionInfo(path).CompanyName.NullIfEmpty();
		}
		catch
		{
			return null;
		}
	}

	private static string? ResolveShortcut(string linkPath)
	{
		Type typeFromProgID = Type.GetTypeFromProgID("WScript.Shell");
		if ((object)typeFromProgID == null)
		{
			return null;
		}
		dynamic val = null;
		try
		{
			val = Activator.CreateInstance(typeFromProgID);
			dynamic val2 = val.CreateShortcut(linkPath);
			return val2.TargetPath;
		}
		finally
		{
			if ((object)val != null && OperatingSystem.IsWindows())
			{
				Marshal.FinalReleaseComObject(val);
			}
		}
	}
}

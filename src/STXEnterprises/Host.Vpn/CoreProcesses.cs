using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace STXEnterprises.Host.Vpn;

/// <summary>Finds and stops the xray / sing-box processes that belong to THIS install - and only those.</summary>
public static class CoreProcesses
{
	/// <summary>
	/// True when <paramref name="exePath"/> lies inside <paramref name="installDir"/>.
	/// The directory is compared with a trailing separator, so "C:\STX" does not match "C:\STX-old\xray.exe"
	/// (a plain StartsWith did).
	/// </summary>
	public static bool PathIsUnder(string? exePath, string installDir)
	{
		if (string.IsNullOrWhiteSpace(exePath) || string.IsNullOrWhiteSpace(installDir))
		{
			return false;
		}
		try
		{
			string dir = Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			string exe = Path.GetFullPath(exePath);
			return exe.StartsWith(dir, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Full image path of a process, even for elevated/other-session processes where MainModule is denied.</summary>
	public static string? TryGetPath(Process process)
	{
		try
		{
			return process.MainModule?.FileName;
		}
		catch
		{
		}
		if (OperatingSystem.IsWindows())
		{
			try
			{
				return QueryImagePath(process.Id);
			}
			catch
			{
			}
		}
		return null;
	}

	public static bool IsOurs(Process process, string installDir)
	{
		return PathIsUnder(TryGetPath(process), installDir);
	}

	/// <summary>
	/// Stops every process with one of <paramref name="names"/> whose executable is inside <paramref name="installDir"/>.
	/// Processes whose path cannot be determined are left alone - another VPN client's core must never be killed.
	/// </summary>
	public static int KillOurs(IEnumerable<string> names, string installDir, Action<string> log)
	{
		int killed = 0;
		foreach (string name in names)
		{
			foreach (Process process in Process.GetProcessesByName(name))
			{
				try
				{
					if (process.HasExited)
					{
						continue;
					}
					string? path = TryGetPath(process);
					if (path == null)
					{
						log($"left {name} (pid {process.Id}) alone: could not verify it belongs to this install");
						continue;
					}
					if (!PathIsUnder(path, installDir))
					{
						continue;
					}
					process.Kill(entireProcessTree: true);
					process.WaitForExit(3000);
					killed++;
					log($"stopped a stray {name} left by a previous session (pid {process.Id})");
				}
				catch (Exception ex)
				{
					log($"could not stop {name}: {ex.Message}");
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		return killed;
	}

	public static bool AnyRunning(string name, string installDir)
	{
		foreach (Process process in Process.GetProcessesByName(name))
		{
			try
			{
				if (!process.HasExited && IsOurs(process, installDir))
				{
					return true;
				}
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		return false;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder text, ref int size);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr handle);

	private static string? QueryImagePath(int pid)
	{
		const uint ProcessQueryLimitedInformation = 0x1000;
		IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, inherit: false, pid);
		if (handle == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			StringBuilder sb = new StringBuilder(1024);
			int size = sb.Capacity;
			return QueryFullProcessImageName(handle, 0, sb, ref size) ? sb.ToString(0, size) : null;
		}
		finally
		{
			CloseHandle(handle);
		}
	}
}

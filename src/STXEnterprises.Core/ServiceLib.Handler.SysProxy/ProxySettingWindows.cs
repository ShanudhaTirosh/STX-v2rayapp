using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using ServiceLib.Common;

namespace ServiceLib.Handler.SysProxy;

[SupportedOSPlatform("windows")]
public static class ProxySettingWindows
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct InternetPerConnOptionList
	{
		public int dwSize;

		public nint szConnection;

		public int dwOptionCount;

		public int dwOptionError;

		public nint options;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct InternetConnectionOption
	{
		[StructLayout(LayoutKind.Explicit)]
		public struct InternetConnectionOptionValue
		{
			[FieldOffset(0)]
			public FILETIME m_FileTime;

			[FieldOffset(0)]
			public int m_Int;

			[FieldOffset(0)]
			public nint m_StringPtr;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct RASENTRYNAME
		{
			public int dwSize;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
			public string szEntryName;

			public int dwFlags;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 261)]
			public string szPhonebookPath;
		}

		private static readonly int Size;

		public PerConnOption m_Option;

		public InternetConnectionOptionValue m_Value;

		public const int RAS_MaxEntryName = 256;

		public const int MAX_PATH = 260;

		static InternetConnectionOption()
		{
			Size = Marshal.SizeOf(typeof(InternetConnectionOption));
		}
	}

	public enum InternetOption : uint
	{
		INTERNET_OPTION_PER_CONNECTION_OPTION = 75u,
		INTERNET_OPTION_REFRESH = 37u,
		INTERNET_OPTION_SETTINGS_CHANGED = 39u
	}

	public enum PerConnOption
	{
		INTERNET_PER_CONN_FLAGS = 1,
		INTERNET_PER_CONN_PROXY_SERVER,
		INTERNET_PER_CONN_PROXY_BYPASS,
		INTERNET_PER_CONN_AUTOCONFIG_URL
	}

	[Flags]
	public enum PerConnFlags
	{
		PROXY_TYPE_DIRECT = 1,
		PROXY_TYPE_PROXY = 2,
		PROXY_TYPE_AUTO_PROXY_URL = 4,
		PROXY_TYPE_AUTO_DETECT = 8
	}

	public enum ErrorCode : uint
	{
		ERROR_BUFFER_TOO_SMALL = 603u,
		ERROR_INVALID_SIZE = 632u
	}

	internal static class NativeMethods
	{
		[DllImport("WinInet.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool InternetSetOption(nint hInternet, InternetOption dwOption, nint lpBuffer, int dwBufferLength);

		[DllImport("Rasapi32.dll", CharSet = CharSet.Auto)]
		public static extern uint RasEnumEntries(string? reserved, string? lpszPhonebook, [In][Out] InternetConnectionOption.RASENTRYNAME[]? lprasentryname, ref int lpcb, ref int lpcEntries);
	}

	private const string _regPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";

	private static bool SetProxyFallback(string? strProxy, string? exceptions, int type)
	{
		if (type == 1)
		{
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable", 0);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer", string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyOverride", string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoConfigURL", string.Empty);
		}
		switch (type)
		{
		case 2:
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable", 1);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer", strProxy ?? string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyOverride", exceptions ?? string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoConfigURL", string.Empty);
			break;
		case 4:
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable", 0);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer", string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyOverride", string.Empty);
			WindowsUtils.RegWriteValue("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoConfigURL", strProxy ?? string.Empty);
			break;
		}
		return true;
	}

	public static bool UnsetProxy()
	{
		return SetProxy(null, null, 1);
	}

	public static bool SetProxy(string? strProxy, string? exceptions, int type)
	{
		try
		{
			bool flag = SetConnectionProxy(null, strProxy, exceptions, type);
			foreach (string item in EnumerateRasEntries())
			{
				flag |= SetConnectionProxy(item, strProxy, exceptions, type);
			}
			return flag;
		}
		catch
		{
			SetProxyFallback(strProxy, exceptions, type);
			return false;
		}
	}

	private static bool SetConnectionProxy(string? connectionName, string? strProxy, string? exceptions, int type)
	{
		InternetPerConnOptionList structure = default;
		int num = 1;
		if (type == 1)
		{
			num = 1;
		}
		else if ((type == 2 || type == 4) ? true : false)
		{
			num = (exceptions.IsNullOrEmpty() ? 2 : 3);
		}
		int num2 = 1;
		PerConnOption option = PerConnOption.INTERNET_PER_CONN_FLAGS;
		switch (type)
		{
		case 2:
			num2 = 3;
			option = PerConnOption.INTERNET_PER_CONN_PROXY_SERVER;
			break;
		case 4:
			num2 = 5;
			option = PerConnOption.INTERNET_PER_CONN_AUTOCONFIG_URL;
			break;
		}
		InternetConnectionOption[] array = new InternetConnectionOption[num];
		array[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;
		array[0].m_Value.m_Int = num2;
		if (num > 1)
		{
			array[1].m_Option = option;
			array[1].m_Value.m_StringPtr = Marshal.StringToHGlobalAuto(strProxy);
			if (num > 2)
			{
				array[2].m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_BYPASS;
				array[2].m_Value.m_StringPtr = Marshal.StringToHGlobalAuto(exceptions);
			}
		}
		structure.dwSize = Marshal.SizeOf(structure);
		if (connectionName != null)
		{
			structure.szConnection = Marshal.StringToHGlobalAuto(connectionName);
		}
		else
		{
			structure.szConnection = IntPtr.Zero;
		}
		structure.dwOptionCount = array.Length;
		structure.dwOptionError = 0;
		int num3 = Marshal.SizeOf(typeof(InternetConnectionOption));
		checked
		{
			nint num4 = Marshal.AllocCoTaskMem(num3 * array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				if (Environment.Is64BitOperatingSystem)
				{
					Marshal.StructureToPtr(ptr: new IntPtr(unchecked((IntPtr)num4).ToInt64() + i * num3), structure: array[i], fDeleteOld: false);
					continue;
				}
				Marshal.StructureToPtr(ptr: new IntPtr(unchecked((IntPtr)num4).ToInt32() + i * num3), structure: array[i], fDeleteOld: false);
			}
			structure.options = num4;
			nint num5 = Marshal.AllocCoTaskMem(structure.dwSize);
			Marshal.StructureToPtr(structure, num5, fDeleteOld: false);
			bool flag = NativeMethods.InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION, num5, structure.dwSize);
			int num6 = 0;
			if (!flag)
			{
				num6 = Marshal.GetLastPInvokeError();
			}
			else
			{
				NativeMethods.InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
				NativeMethods.InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
			}
			if (structure.szConnection != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(structure.szConnection);
			}
			if (num > 1)
			{
				Marshal.FreeHGlobal(array[1].m_Value.m_StringPtr);
				if (num > 2)
				{
					Marshal.FreeHGlobal(array[2].m_Value.m_StringPtr);
				}
			}
			Marshal.FreeCoTaskMem(num4);
			Marshal.FreeCoTaskMem(num5);
			if (num6 != 0)
			{
				throw new ApplicationException($"Set Internet Proxy failed with error code: {Marshal.GetLastWin32Error()}");
			}
			return true;
		}
	}

	private static IEnumerable<string> EnumerateRasEntries()
	{
		int lpcEntries = 0;
		InternetConnectionOption.RASENTRYNAME[] array = new InternetConnectionOption.RASENTRYNAME[1];
		int lpcb = Marshal.SizeOf(typeof(InternetConnectionOption.RASENTRYNAME));
		array[0].dwSize = Marshal.SizeOf(typeof(InternetConnectionOption.RASENTRYNAME));
		uint num = NativeMethods.RasEnumEntries(null, null, array, ref lpcb, ref lpcEntries);
		if (num == 603)
		{
			array = new InternetConnectionOption.RASENTRYNAME[lpcb / Marshal.SizeOf(typeof(InternetConnectionOption.RASENTRYNAME))];
			for (int i = 0; i < array.Length; i = checked(i + 1))
			{
				array[i].dwSize = Marshal.SizeOf(typeof(InternetConnectionOption.RASENTRYNAME));
			}
			num = NativeMethods.RasEnumEntries(null, null, array, ref lpcb, ref lpcEntries);
		}
		if (num == 0)
		{
			List<string> list = new List<string>();
			for (int j = 0; j < lpcEntries; j = checked(j + 1))
			{
				list.Add(array[j].szEntryName);
			}
			return list;
		}
		throw new ApplicationException($"RasEnumEntries failed with error code: {num}");
	}
}

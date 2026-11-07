using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ServiceLib.Common;

namespace ServiceLib.Services;

[SupportedOSPlatform("windows")]
public sealed class WindowsJobService : IDisposable
{
	private nint handle = IntPtr.Zero;

	private bool disposed;

	public WindowsJobService()
	{
		handle = CreateJobObject(IntPtr.Zero, null);
		nint num = IntPtr.Zero;
		JOBOBJECT_BASIC_LIMIT_INFORMATION basicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			LimitFlags = 8192u
		};
		JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = basicLimitInformation
		};
		try
		{
			int num2 = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
			num = Marshal.AllocHGlobal(num2);
			Marshal.StructureToPtr(structure, num, fDeleteOld: false);
			if (!SetInformationJobObject(handle, JobObjectInfoType.ExtendedLimitInformation, num, checked((uint)num2)))
			{
				throw new Exception($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");
			}
		}
		finally
		{
			if (num != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(num);
			}
		}
	}

	public bool AddProcess(nint processHandle)
	{
		bool flag = AssignProcessToJobObject(handle, processHandle);
		if (!flag)
		{
			Logging.SaveLog("Failed to call AssignProcessToJobObject! GetLastError=" + Marshal.GetLastWin32Error());
		}
		return flag;
	}

	public bool AddProcess(int processId)
	{
		return AddProcess(Process.GetProcessById(processId).Handle);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			disposed = true;
			if (handle != IntPtr.Zero)
			{
				CloseHandle(handle);
				handle = IntPtr.Zero;
			}
		}
	}

	~WindowsJobService()
	{
		Dispose(disposing: false);
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateJobObject(nint a, string? lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetInformationJobObject(nint hJob, JobObjectInfoType infoType, nint lpJobObjectInfo, uint cbJobObjectInfoLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AssignProcessToJobObject(nint job, nint process);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(nint hObject);
}

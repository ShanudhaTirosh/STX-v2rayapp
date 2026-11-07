using System;
using System.Diagnostics;

namespace ServiceLib.Common;

public static class ProcUtils
{
	private static readonly string _tag = "ProcUtils";

	public static void ProcessStart(string? fileName, string arguments = "")
	{
		ProcessStart(fileName, arguments, null);
	}

	public static int? ProcessStart(string? fileName, string arguments, string? dir)
	{
		if (fileName.IsNullOrEmpty())
		{
			return null;
		}
		try
		{
			if (fileName.Contains(' '))
			{
				fileName = fileName.AppendQuotes();
			}
			if (arguments.Contains(' '))
			{
				arguments = arguments.AppendQuotes();
			}
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					UseShellExecute = true,
					FileName = fileName,
					Arguments = arguments,
					WorkingDirectory = (dir ?? string.Empty)
				}
			};
			process.Start();
			return (dir == null) ? ((int?)null) : new int?(process.Id);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return null;
	}

	public static bool RebootAsAdmin(bool blAdmin = true)
	{
		try
		{
			return Process.Start(new ProcessStartInfo
			{
				UseShellExecute = true,
				Arguments = "rebootas",
				WorkingDirectory = Utils.StartupPath(),
				FileName = Utils.GetExePath().AppendQuotes(),
				Verb = (blAdmin ? "runas" : null)
			}) != null;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return false;
		}
	}
}

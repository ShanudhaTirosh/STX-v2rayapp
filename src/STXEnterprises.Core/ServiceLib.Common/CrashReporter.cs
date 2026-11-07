using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ServiceLib.Common;

/// <summary>
/// Local-only crash reports and a one-click diagnostics bundle.
/// A VPN client must not phone home, so nothing here is uploaded: reports are redacted text files the user can
/// choose to send to support.
/// </summary>
public static class CrashReporter
{
	private const int KeepReports = 20;

	public static string CrashDirectory => Path.Combine(Utils.GetLogPath(), "crash");

	public static string? Write(string source, Exception ex, string? appVersion = null)
	{
		try
		{
			Directory.CreateDirectory(CrashDirectory);
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("STX crash report");
			sb.AppendLine("time (utc): " + DateTime.UtcNow.ToString("u"));
			sb.AppendLine("source:     " + source);
			sb.AppendLine("app:        " + (appVersion ?? "unknown"));
			sb.AppendLine("os:         " + Environment.OSVersion + " / " + (Environment.Is64BitOperatingSystem ? "x64" : "x86"));
			sb.AppendLine("runtime:    " + Environment.Version);
			sb.AppendLine();
			for (Exception? e = ex; e != null; e = e.InnerException)
			{
				sb.AppendLine(e.GetType().FullName + ": " + e.Message);
				sb.AppendLine(e.StackTrace);
				sb.AppendLine();
			}
			string path = Path.Combine(CrashDirectory, $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.txt");
			File.WriteAllText(path, LogRedactor.Redact(sb.ToString()), new UTF8Encoding(false));
			Prune();
			return path;
		}
		catch
		{
			return null; // reporting a crash must never cause another one
		}
	}

	private static void Prune()
	{
		try
		{
			foreach (FileInfo old in new DirectoryInfo(CrashDirectory).GetFiles("crash-*.txt").OrderByDescending((FileInfo f) => f.LastWriteTimeUtc).Skip(KeepReports))
			{
				old.Delete();
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// Zips the last few days of logs, crash reports and a short environment summary, all redacted.
	/// Server configs, account IDs and the config database are deliberately NOT included.
	/// </summary>
	public static string CreateDiagnosticsBundle(string zipPath, string appVersion, string coreSummary)
	{
		string? dir = Path.GetDirectoryName(Path.GetFullPath(zipPath));
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}
		if (File.Exists(zipPath))
		{
			File.Delete(zipPath);
		}
		using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
		{
			AddText(zip, "summary.txt", $"STX diagnostics\r\ncreated (utc): {DateTime.UtcNow:u}\r\napp: {appVersion}\r\nos: {Environment.OSVersion}\r\nruntime: {Environment.Version}\r\n{coreSummary}\r\n");
			string logs = Utils.GetLogPath();
			if (Directory.Exists(logs))
			{
				DateTime since = DateTime.UtcNow.AddDays(-3);
				foreach (FileInfo f in new DirectoryInfo(logs).GetFiles("*.txt").Where((FileInfo f) => f.LastWriteTimeUtc >= since))
				{
					AddText(zip, "logs/" + f.Name, ReadShared(f.FullName, 2_000_000));
				}
				if (Directory.Exists(CrashDirectory))
				{
					foreach (FileInfo f in new DirectoryInfo(CrashDirectory).GetFiles("crash-*.txt").OrderByDescending((FileInfo c) => c.LastWriteTimeUtc).Take(5))
					{
						AddText(zip, "crash/" + f.Name, ReadShared(f.FullName, 200_000));
					}
				}
			}
		}
		return zipPath;
	}

	private static string ReadShared(string path, int maxChars)
	{
		using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using StreamReader sr = new StreamReader(fs);
		string text = sr.ReadToEnd();
		if (text.Length > maxChars)
		{
			text = text.Substring(text.Length - maxChars);
		}
		return LogRedactor.Redact(text);
	}

	private static void AddText(ZipArchive zip, string name, string text)
	{
		ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
		using StreamWriter w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
		w.Write(text);
	}
}

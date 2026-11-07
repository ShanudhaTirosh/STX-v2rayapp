using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ServiceLib.Common;

public class Logging
{
	private static readonly Logger _logger1 = LogManager.GetLogger("Log1");

	private static readonly Logger _logger2 = LogManager.GetLogger("Log2");

	/// <summary>Daily log files older than this are deleted at startup.</summary>
	public const int RetentionDays = 7;

	private const long RolloverBytes = 8L * 1024 * 1024;

	public static void Setup()
	{
		LoggingConfiguration loggingConfiguration = new LoggingConfiguration();
		FileTarget fileTarget = new FileTarget();
		loggingConfiguration.AddTarget("file", fileTarget);
		fileTarget.Layout = "${longdate}-${level:uppercase=true} ${message}";
		fileTarget.FileName = Utils.GetLogPath("${shortdate}.txt");
		// A single noisy day must not fill the disk: roll the file over at 8 MB and keep a few archives.
		fileTarget.ArchiveAboveSize = RolloverBytes;
		fileTarget.MaxArchiveFiles = 3;
		loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));
		LogManager.Configuration = loggingConfiguration;
		PruneOldLogs(Utils.GetLogPath(), RetentionDays);
	}

	/// <summary>Deletes *.txt log files that have not been written to for <paramref name="days"/> days.</summary>
	public static int PruneOldLogs(string directory, int days)
	{
		int removed = 0;
		try
		{
			DirectoryInfo dir = new DirectoryInfo(directory);
			if (!dir.Exists)
			{
				return 0;
			}
			DateTime cutoff = DateTime.UtcNow.AddDays(-days);
			foreach (FileInfo file in dir.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
			{
				try
				{
					if (file.LastWriteTimeUtc < cutoff)
					{
						file.Delete();
						removed++;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return removed;
	}

	public static void LoggingEnabled(bool enable)
	{
		if (!enable)
		{
			LogManager.SuspendLogging();
		}
	}

	public static void SaveLog(string strContent)
	{
		if (LogManager.IsLoggingEnabled())
		{
			_logger1.Info(LogRedactor.Redact(strContent));
		}
	}

	public static void SaveLog(string strTitle, Exception ex)
	{
		if (LogManager.IsLoggingEnabled())
		{
			_logger2.Debug(LogRedactor.Redact(strTitle + "," + ex.Message));
			_logger2.Debug(LogRedactor.Redact(ex.StackTrace));
			if (ex?.InnerException != null)
			{
				_logger2.Error(LogRedactor.Redact(ex.InnerException.ToString()));
			}
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace ServiceLib.Common;

public static class EmbedUtils
{
	private static readonly string _tag = "EmbedUtils";

	private static readonly ConcurrentDictionary<string, string> _dicEmbedCache = new ConcurrentDictionary<string, string>();

	public static string GetEmbedText(string res)
	{
		if (_dicEmbedCache.TryGetValue(res, out string value))
		{
			return value;
		}
		string text = string.Empty;
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
			ArgumentNullException.ThrowIfNull(stream, "stream");
			using StreamReader streamReader = new StreamReader(stream);
			text = streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		_dicEmbedCache.TryAdd(res, text);
		return text;
	}

	public static string? LoadResource(string? res)
	{
		try
		{
			if (File.Exists(res))
			{
				return File.ReadAllText(res);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return null;
	}
}

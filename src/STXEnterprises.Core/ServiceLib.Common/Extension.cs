using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ServiceLib.Enums;

namespace ServiceLib.Common;

public static class Extension
{
	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return string.IsNullOrEmpty(value);
		}
		return true;
	}

	public static bool IsNotEmpty([NotNullWhen(false)] this string? value)
	{
		return !string.IsNullOrWhiteSpace(value);
	}

	public static string? NullIfEmpty(this string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return null;
	}

	public static bool BeginWithAny(this string s, IEnumerable<char> chars)
	{
		if (s.IsNullOrEmpty())
		{
			return false;
		}
		return chars.Contains(s.First());
	}

	private static bool IsWhiteSpace(this string value)
	{
		return value.All(char.IsWhiteSpace);
	}

	public static IEnumerable<string> NonWhiteSpaceLines(this TextReader reader)
	{
		while (true)
		{
			string text = reader.ReadLine();
			if (text != null)
			{
				if (!text.IsWhiteSpace())
				{
					yield return text;
				}
				continue;
			}
			break;
		}
	}

	public static string TrimEx(this string? value)
	{
		if (value != null)
		{
			return value.Trim();
		}
		return string.Empty;
	}

	public static string RemovePrefix(this string value, char prefix)
	{
		if (!value.StartsWith(prefix))
		{
			return value;
		}
		return value.Substring(1);
	}

	public static string RemovePrefix(this string value, string prefix)
	{
		if (!value.StartsWith(prefix))
		{
			return value;
		}
		return value.Substring(prefix.Length);
	}

	public static string UpperFirstChar(this string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return char.ToUpper(value.First()) + value.Substring(1);
	}

	public static string AppendQuotes(this string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return "\"" + value + "\"";
		}
		return string.Empty;
	}

	public static int ToInt(this string? value, int defaultValue = 0)
	{
		if (!int.TryParse(value, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static List<string> AppendEmpty(this IEnumerable<string> source)
	{
		return source.Concat(new string[1] { string.Empty }).ToList();
	}

	public static bool IsGroupType(this EConfigType configType)
	{
		if ((uint)(configType - 101) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool IsComplexType(this EConfigType configType)
	{
		if (configType == EConfigType.Custom || (uint)(configType - 101) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static void AddRangeSafe<T>(this ICollection<T> destination, IEnumerable<T>? source)
	{
		ArgumentNullException.ThrowIfNull(destination, "destination");
		if (source == null)
		{
			return;
		}
		if (destination is List<T> list)
		{
			list.AddRange(source);
			return;
		}
		foreach (T item in source)
		{
			destination.Add(item);
		}
	}
}

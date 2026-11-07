using System;
using System.Collections.Generic;

namespace ServiceLib.Common;

public static class CountryExtension
{
	private static readonly Dictionary<string, string> CountryEmojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "CN", "\ud83c\udde8\ud83c\uddf3" },
		{ "HK", "\ud83c\udded\ud83c\uddf0" },
		{ "TW", "\ud83c\uddf9\ud83c\uddfc" },
		{ "JP", "\ud83c\uddef\ud83c\uddf5" },
		{ "SG", "\ud83c\uddf8\ud83c\uddec" },
		{ "KR", "\ud83c\uddf0\ud83c\uddf7" },
		{ "TH", "\ud83c\uddf9\ud83c\udded" },
		{ "VN", "\ud83c\uddfb\ud83c\uddf3" },
		{ "ID", "\ud83c\uddee\ud83c\udde9" },
		{ "PH", "\ud83c\uddf5\ud83c\udded" },
		{ "MY", "\ud83c\uddf2\ud83c\uddfe" },
		{ "IN", "\ud83c\uddee\ud83c\uddf3" },
		{ "PK", "\ud83c\uddf5\ud83c\uddf0" },
		{ "BD", "\ud83c\udde7\ud83c\udde9" },
		{ "LK", "\ud83c\uddf1\ud83c\uddf0" },
		{ "KH", "\ud83c\uddf0\ud83c\udded" },
		{ "LA", "\ud83c\uddf1\ud83c\udde6" },
		{ "MM", "\ud83c\uddf2\ud83c\uddf2" },
		{ "US", "\ud83c\uddfa\ud83c\uddf8" },
		{ "CA", "\ud83c\udde8\ud83c\udde6" },
		{ "MX", "\ud83c\uddf2\ud83c\uddfd" },
		{ "BR", "\ud83c\udde7\ud83c\uddf7" },
		{ "AR", "\ud83c\udde6\ud83c\uddf7" },
		{ "CL", "\ud83c\udde8\ud83c\uddf1" },
		{ "CO", "\ud83c\udde8\ud83c\uddf4" },
		{ "GB", "\ud83c\uddec\ud83c\udde7" },
		{ "DE", "\ud83c\udde9\ud83c\uddea" },
		{ "FR", "\ud83c\uddeb\ud83c\uddf7" },
		{ "IT", "\ud83c\uddee\ud83c\uddf9" },
		{ "ES", "\ud83c\uddea\ud83c\uddf8" },
		{ "RU", "\ud83c\uddf7\ud83c\uddfa" },
		{ "NL", "\ud83c\uddf3\ud83c\uddf1" },
		{ "CH", "\ud83c\udde8\ud83c\udded" },
		{ "SE", "\ud83c\uddf8\ud83c\uddea" },
		{ "NO", "\ud83c\uddf3\ud83c\uddf4" },
		{ "DK", "\ud83c\udde9\ud83c\uddf0" },
		{ "FI", "\ud83c\uddeb\ud83c\uddee" },
		{ "PL", "\ud83c\uddf5\ud83c\uddf1" },
		{ "CZ", "\ud83c\udde8\ud83c\uddff" },
		{ "AT", "\ud83c\udde6\ud83c\uddf9" },
		{ "GR", "\ud83c\uddec\ud83c\uddf7" },
		{ "PT", "\ud83c\uddf5\ud83c\uddf9" },
		{ "TR", "\ud83c\uddf9\ud83c\uddf7" },
		{ "UA", "\ud83c\uddfa\ud83c\udde6" },
		{ "RO", "\ud83c\uddf7\ud83c\uddf4" },
		{ "AE", "\ud83c\udde6\ud83c\uddea" },
		{ "SA", "\ud83c\uddf8\ud83c\udde6" },
		{ "IL", "\ud83c\uddee\ud83c\uddf1" },
		{ "KZ", "\ud83c\uddf0\ud83c\uddff" },
		{ "AU", "\ud83c\udde6\ud83c\uddfa" },
		{ "NZ", "\ud83c\uddf3\ud83c\uddff" },
		{ "ZA", "\ud83c\uddff\ud83c\udde6" },
		{ "EG", "\ud83c\uddea\ud83c\uddec" }
	};

	public static string? CountryToEmoji(this string? countryCode)
	{
		if (countryCode.IsNullOrEmpty())
		{
			return null;
		}
		if (!CountryEmojiMap.TryGetValue(countryCode, out string value))
		{
			return null;
		}
		return value;
	}
}

using ServiceLib.Common;

namespace ServiceLib.Models.Dto;

public readonly record struct IpInfoResult(string Country, string? Ip)
{
	public override string ToString()
	{
		string value = Country.CountryToEmoji();
		return $"{value}({Country}) {Ip}";
	}
}

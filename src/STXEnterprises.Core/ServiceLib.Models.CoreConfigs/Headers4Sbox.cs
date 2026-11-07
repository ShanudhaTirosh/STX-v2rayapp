using System.Text.Json.Serialization;

namespace ServiceLib.Models.CoreConfigs;

public class Headers4Sbox
{
	public string? Host { get; set; }

	[JsonPropertyName("User-Agent")]
	public string UserAgent { get; set; }
}

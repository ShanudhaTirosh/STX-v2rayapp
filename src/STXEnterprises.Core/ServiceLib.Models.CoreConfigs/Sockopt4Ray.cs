using System.Text.Json.Serialization;

namespace ServiceLib.Models.CoreConfigs;

public class Sockopt4Ray
{
	public string? dialerProxy { get; set; }

	public bool? tcpFastOpen { get; set; }

	[JsonPropertyName("interface")]
	public string? Interface { get; set; }
}

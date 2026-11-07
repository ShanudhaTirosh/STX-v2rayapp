using System.Text.Json.Serialization;

namespace ServiceLib.Models.CoreConfigs;

public class Headers4Ray
{
	[JsonPropertyName("User-Agent")]
	public string UserAgent { get; set; }
}

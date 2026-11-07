using System;
using System.Text.Json.Serialization;

namespace ServiceLib.Models.Dto;

[Serializable]
public class VmessQRCode
{
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
	public int v { get; set; } = 2;

	public string ps { get; set; } = string.Empty;

	public string add { get; set; } = string.Empty;

	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
	public int port { get; set; }

	public string id { get; set; } = string.Empty;

	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
	public int aid { get; set; }

	public string scy { get; set; } = string.Empty;

	public string net { get; set; } = string.Empty;

	public string type { get; set; } = string.Empty;

	public string host { get; set; } = string.Empty;

	public string path { get; set; } = string.Empty;

	public string tls { get; set; } = string.Empty;

	public string sni { get; set; } = string.Empty;

	public string alpn { get; set; } = string.Empty;

	public string fp { get; set; } = string.Empty;

	public string insecure { get; set; } = string.Empty;
}

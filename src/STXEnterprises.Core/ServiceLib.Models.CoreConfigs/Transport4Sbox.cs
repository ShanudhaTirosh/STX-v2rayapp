namespace ServiceLib.Models.CoreConfigs;

public class Transport4Sbox
{
	public string? type { get; set; }

	public object? host { get; set; }

	public string? path { get; set; }

	public Headers4Sbox? headers { get; set; }

	public string? service_name { get; set; }

	public string? idle_timeout { get; set; }

	public string? ping_timeout { get; set; }

	public bool? permit_without_stream { get; set; }

	public int? max_early_data { get; set; }

	public string? early_data_header_name { get; set; }
}

namespace ServiceLib.Models.CoreConfigs;

public class GrpcSettings4Ray
{
	public string? authority { get; set; }

	public string? serviceName { get; set; }

	public bool multiMode { get; set; }

	public int? idle_timeout { get; set; }

	public int? health_check_timeout { get; set; }

	public bool? permit_without_stream { get; set; }

	public int? initial_windows_size { get; set; }

	public string? user_agent { get; set; }
}

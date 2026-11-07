using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class GrpcItem
{
	public int? IdleTimeout { get; set; }

	public int? HealthCheckTimeout { get; set; }

	public bool? PermitWithoutStream { get; set; }

	public int? InitialWindowsSize { get; set; }
}

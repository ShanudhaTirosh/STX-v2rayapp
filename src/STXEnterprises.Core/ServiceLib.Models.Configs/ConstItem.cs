using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class ConstItem
{
	public string? SubConvertUrl { get; set; }

	public string? GeoSourceUrl { get; set; }

	public string? SrsSourceUrl { get; set; }

	public string? RouteRulesTemplateSourceUrl { get; set; }
}

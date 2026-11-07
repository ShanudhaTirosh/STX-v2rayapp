using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class WebDavItem
{
	public string? Url { get; set; }

	public string? UserName { get; set; }

	public string? Password { get; set; }

	public string? DirName { get; set; }
}

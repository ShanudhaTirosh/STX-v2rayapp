using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class CoreBasicItem
{
	public bool LogEnabled { get; set; }

	public string Loglevel { get; set; }

	public bool MuxEnabled { get; set; }

	public bool DefAllowInsecure { get; set; }

	public string DefFingerprint { get; set; }

	public string DefUserAgent { get; set; }

	public string? SendThrough { get; set; }

	public string? BindInterface { get; set; }

	public bool EnableFragment { get; set; }

	public bool EnableCacheFile4Sbox { get; set; } = true;
}

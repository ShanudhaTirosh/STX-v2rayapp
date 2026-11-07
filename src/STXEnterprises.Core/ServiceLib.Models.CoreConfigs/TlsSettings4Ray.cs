using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class TlsSettings4Ray
{
	public bool? allowInsecure { get; set; }

	public string? serverName { get; set; }

	public string? verifyPeerCertByName { get; set; }

	public List<string>? alpn { get; set; }

	public string? fingerprint { get; set; }

	public bool? show { get; set; }

	public string? publicKey { get; set; }

	public string? shortId { get; set; }

	public string? spiderX { get; set; }

	public string? mldsa65Verify { get; set; }

	public List<CertificateSettings4Ray>? certificates { get; set; }

	public string? pinnedPeerCertSha256 { get; set; }

	public bool? disableSystemRoot { get; set; }

	public string? echConfigList { get; set; }

	public string? echForceQuery { get; set; }

	public Sockopt4Ray? echSockopt { get; set; }
}

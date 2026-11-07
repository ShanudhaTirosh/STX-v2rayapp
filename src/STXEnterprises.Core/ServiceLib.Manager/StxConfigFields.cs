namespace ServiceLib.Manager;

public sealed class StxConfigFields
{
	public string? Address { get; set; }

	public int? Port { get; set; }

	public string? Sni { get; set; }

	public string? AllowInsecure { get; set; }

	public string? Fingerprint { get; set; }

	public string? Alpn { get; set; }

	public string? Host { get; set; }

	public string? Path { get; set; }

	public bool IsEmpty
	{
		get
		{
			if (Address == null && !Port.HasValue && Sni == null && AllowInsecure == null && Fingerprint == null && Alpn == null && Host == null)
			{
				return Path == null;
			}
			return false;
		}
	}
}

using ServiceLib.Enums;

namespace ServiceLib.Models.Entities;

public record ProtocolExtraItem
{
	public bool? Uot { get; set; }

	public string? CongestionControl { get; set; }

	public string? AlterId { get; set; }

	public string? VmessSecurity { get; set; }

	public string? Flow { get; set; }

	public string? VlessEncryption { get; set; }

	public string? SsMethod { get; set; }

	public string? WgPublicKey { get; set; }

	public string? WgPresharedKey { get; set; }

	public string? WgInterfaceAddress { get; set; }

	public string? WgReserved { get; set; }

	public int? WgMtu { get; set; }

	public string? SalamanderPass { get; set; }

	public int? UpMbps { get; set; }

	public int? DownMbps { get; set; }

	public string? Ports { get; set; }

	public string? HopInterval { get; set; }

	public int? InsecureConcurrency { get; set; }

	public bool? NaiveQuic { get; set; }

	public string? GroupType { get; set; }

	public string? ChildItems { get; set; }

	public string? SubChildItems { get; set; }

	public string? Filter { get; set; }

	public EMultipleLoad? MultipleLoad { get; set; }
}

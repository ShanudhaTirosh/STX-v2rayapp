namespace ServiceLib.Models.CoreConfigs;

public class StreamSettings4Ray
{
	public string network { get; set; }

	public string security { get; set; }

	public TlsSettings4Ray? tlsSettings { get; set; }

	public RawSettings4Ray? rawSettings { get; set; }

	public KcpSettings4Ray? kcpSettings { get; set; }

	public WsSettings4Ray? wsSettings { get; set; }

	public HttpupgradeSettings4Ray? httpupgradeSettings { get; set; }

	public XhttpSettings4Ray? xhttpSettings { get; set; }

	public HttpSettings4Ray? httpSettings { get; set; }

	public QuicSettings4Ray? quicSettings { get; set; }

	public TlsSettings4Ray? realitySettings { get; set; }

	public GrpcSettings4Ray? grpcSettings { get; set; }

	public HysteriaSettings4Ray? hysteriaSettings { get; set; }

	public object? finalmask { get; set; }

	public Sockopt4Ray? sockopt { get; set; }
}

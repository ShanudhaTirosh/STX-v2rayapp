namespace STXEnterprises.Host.Vpn;

public sealed record TunnelStatus(TunnelState State, string? ConfigId, string? ConfigName, string Mode, long UploadSpeed, long DownloadSpeed, long SessionUp, long SessionDown, long ConnectedAt, string? Message);

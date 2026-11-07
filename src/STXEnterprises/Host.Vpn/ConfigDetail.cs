namespace STXEnterprises.Host.Vpn;

public sealed record ConfigDetail(string Id, string Name, string Protocol, string Network, string Security, string Address, int Port, string Sni, bool AllowInsecure, string Fingerprint, string Alpn, string Host, string Path, bool Edited);

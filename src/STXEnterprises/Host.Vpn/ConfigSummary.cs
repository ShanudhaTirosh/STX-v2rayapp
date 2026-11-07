namespace STXEnterprises.Host.Vpn;

public sealed record ConfigSummary(string Id, string Name, string Address, int Port, string Uuid, bool IsActive, bool Edited);

namespace STXEnterprises.Host.Vpn;

public sealed record UsageHeader(long Upload, long Download, long Total, long Expire);

namespace ServiceLib.Manager;

public sealed record UplinkChoice(string Name, string Address, int Milliseconds, bool IsTunnel, bool WindowsAgreed);

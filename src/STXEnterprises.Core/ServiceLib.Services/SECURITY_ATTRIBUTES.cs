namespace ServiceLib.Services;

public struct SECURITY_ATTRIBUTES
{
	public uint nLength;

	public nint lpSecurityDescriptor;

	public int bInheritHandle;
}

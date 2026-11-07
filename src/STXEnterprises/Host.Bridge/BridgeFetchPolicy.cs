using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace STXEnterprises.Host.Bridge;

/// <summary>
/// What the web UI may ask the host to fetch. The "hostFetch" bridge call used to forward ANY request (any URL,
/// any headers) from the page; if the page were ever tampered with that is a way to reach services on this PC or
/// the local network. Now only https requests to the app's own backends are allowed.
/// </summary>
public static class BridgeFetchPolicy
{
	private static readonly string[] AllowedHosts = { "api.github.com", "stxenterprises.com", "www.stxenterprises.com" };

	private static readonly string[] AllowedSuffixes = { ".supabase.co", ".stxenterprises.com" };

	private static readonly string[] ForbiddenHeaders = { "host", "content-length", "transfer-encoding", "connection", "upgrade", "te", "expect", "proxy-authorization", "proxy-connection" };

	private static readonly string[] AllowedMethods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD" };

	public static Uri ValidateUrl(string? url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
		{
			throw new ArgumentException("Only https addresses can be requested.");
		}
		if (!string.IsNullOrEmpty(uri.UserInfo))
		{
			throw new ArgumentException("Addresses with embedded credentials are not allowed.");
		}
		string host = uri.IdnHost.ToLowerInvariant();
		if (IPAddress.TryParse(host.Trim('[', ']'), out _))
		{
			throw new ArgumentException("Requests to IP addresses are not allowed.");
		}
		bool ok = Array.IndexOf(AllowedHosts, host) >= 0;
		foreach (string suffix in AllowedSuffixes)
		{
			ok |= host.EndsWith(suffix, StringComparison.Ordinal);
		}
		if (!ok)
		{
			throw new ArgumentException("That address is not one STX is allowed to contact.");
		}
		return uri;
	}

	public static string ValidateMethod(string? method)
	{
		string m = (method ?? "GET").ToUpperInvariant();
		if (Array.IndexOf(AllowedMethods, m) < 0)
		{
			throw new ArgumentException("That request method is not allowed.");
		}
		return m;
	}

	public static bool HeaderAllowed(string name)
	{
		return !string.IsNullOrWhiteSpace(name) && Array.IndexOf(ForbiddenHeaders, name.Trim().ToLowerInvariant()) < 0;
	}

	public static bool IsPublicAddress(IPAddress ip)
	{
		if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
		{
			return false;
		}
		if (ip.IsIPv4MappedToIPv6)
		{
			ip = ip.MapToIPv4();
		}
		if (ip.AddressFamily == AddressFamily.InterNetwork)
		{
			byte[] b = ip.GetAddressBytes();
			return !(b[0] == 10 || b[0] == 0 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] >= 224);
		}
		if (ip.AddressFamily == AddressFamily.InterNetworkV6)
		{
			byte[] b = ip.GetAddressBytes();
			return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || (b[0] & 0xFE) == 0xFC);
		}
		return false;
	}

	/// <summary>For user-supplied subscription URLs: https/http only, and the host must not resolve to a private or loopback address.</summary>
	public static async Task EnsurePublicRemoteAsync(string? url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
		{
			throw new ArgumentException("That is not a web address.");
		}
		IPAddress[] addresses = IPAddress.TryParse(uri.IdnHost.Trim('[', ']'), out IPAddress? literal) ? new[] { literal } : await Dns.GetHostAddressesAsync(uri.IdnHost);
		foreach (IPAddress a in addresses)
		{
			if (!IsPublicAddress(a))
			{
				throw new ArgumentException("Addresses on this computer or the local network are not allowed.");
			}
		}
	}
}

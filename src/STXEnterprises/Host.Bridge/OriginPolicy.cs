using System;

namespace STXEnterprises.Host.Bridge;

/// <summary>Only the app's own bundled pages may talk to the host or stay loaded in the window.</summary>
public static class OriginPolicy
{
	private static readonly string[] TrustedHosts = { "stxenterprises.local", "stx.local" };

	public static bool IsTrusted(string? source)
	{
		return Uri.TryCreate(source, UriKind.Absolute, out Uri? u) && u.Scheme == Uri.UriSchemeHttps && Array.IndexOf(TrustedHosts, u.Host.ToLowerInvariant()) >= 0;
	}

	/// <summary>Navigation allowed inside the window: our own pages, plus the inline "files missing" page.</summary>
	public static bool IsAllowedNavigation(string? uri)
	{
		return IsTrusted(uri) || string.Equals(uri, "about:blank", StringComparison.OrdinalIgnoreCase) || (uri != null && uri.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase));
	}
}

using System;
using System.Text.RegularExpressions;

namespace ServiceLib.Common;

/// <summary>
/// Removes secrets and personal data from text before it is written to a log or a diagnostics bundle:
/// account UUIDs, share links, subscription tokens, JWTs, visited destinations and the Windows user name.
/// Logs are plain files that users are asked to send to support, so they must be safe to share.
/// </summary>
public static class LogRedactor
{
	private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

	private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(250);

	private static readonly Regex ShareLink = new Regex(@"\b(vless|vmess|trojan|ssr?|hysteria2?|hy2|tuic|wireguard|anytls|naive\+https|socks5?)://[^\s""'<>]+", Opts, Budget);

	private static readonly Regex Uuid = new Regex(@"\b([0-9a-f]{8})-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", Opts, Budget);

	private static readonly Regex Jwt = new Regex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{4,}\b", Opts, Budget);

	private static readonly Regex SecretQuery = new Regex(@"([?&](?:token|key|secret|password|passwd|pass|pwd|auth|sub|access_token|refresh_token|apikey|api_key|code|uuid|id)=)[^&\s""'<>]+", Opts, Budget);

	private static readonly Regex LongPathSegment = new Regex(@"(https?://[^/\s""'<>]+(?:/[^/\s""'<>?#]*)*?)/[A-Za-z0-9_-]{20,}", Opts, Budget);

	private static readonly Regex Destination = new Regex(@"(\baccepted\s+(?:tcp|udp):)[^\s\[]+", Opts, Budget);

	private static readonly Regex UserProfile = new Regex(@"([A-Za-z]:\\Users\\)[^\\/\s""'<>:]+", Opts, Budget);

	public static string Redact(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		try
		{
			string s = ShareLink.Replace(text, "$1://<redacted>");
			s = Jwt.Replace(s, "<jwt>");
			s = Destination.Replace(s, "$1<redacted>");
			s = SecretQuery.Replace(s, "$1<redacted>");
			s = LongPathSegment.Replace(s, "$1/<redacted>");
			s = Uuid.Replace(s, "$1-****");
			s = UserProfile.Replace(s, "$1<user>");
			return s;
		}
		catch (RegexMatchTimeoutException)
		{
			return "<log line withheld: could not be safely redacted>";
		}
	}
}

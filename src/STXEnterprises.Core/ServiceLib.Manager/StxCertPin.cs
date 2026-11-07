using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace ServiceLib.Manager;

/// <summary>
/// Replacement for "allowInsecure".
///
/// STX configs deliberately present a different SNI than the real server (zero-rating), so normal certificate
/// validation cannot work and every config was shipped with allowInsecure=true - which accepts ANY certificate,
/// so anyone on the path can impersonate the server and read the account UUID and all traffic. Xray announced
/// that allowInsecure stops working on 2026-08-01 and recommends pinnedPeerCertSha256 instead.
///
/// Pinning does not depend on the SNI, so it fits this design. We learn the server's certificate hash the first
/// time we connect (trust on first use), store it on the profile (ProfileItem.CertSha, already emitted by the
/// config builder as pinnedPeerCertSha256 with allowInsecure=false) and from then on Xray rejects any other certificate.
/// </summary>
public static class StxCertPin
{
	/// <summary>Master switch (HostSettings.PinServerCertificates).</summary>
	public static bool Enabled { get; private set; } = true;

	public static void Set(bool enabled)
	{
		Enabled = enabled;
	}

	public enum Need
	{
		/// <summary>Not a TLS profile, or certificate checking is already on (pin / custom CA / explicit allowInsecure=false).</summary>
		Nothing,

		/// <summary>TLS with certificate checking effectively off and no pin yet.</summary>
		LearnPin,
	}

	/// <summary>Pure decision, kept separate so it can be unit-tested.</summary>
	public static Need Decide(string? streamSecurity, string? allowInsecure, string? certSha, string? certPem, bool insecureIsDefault)
	{
		if (!string.Equals(streamSecurity, "tls", StringComparison.OrdinalIgnoreCase))
		{
			return Need.Nothing;
		}
		if (!string.IsNullOrWhiteSpace(certSha) || !string.IsNullOrWhiteSpace(certPem))
		{
			return Need.Nothing;
		}
		bool explicitOn = Utils.ToBool(allowInsecure);
		bool unset = string.IsNullOrEmpty(allowInsecure);
		return (explicitOn || (unset && insecureIsDefault)) ? Need.LearnPin : Need.Nothing;
	}

	/// <summary>Lower-case hex SHA-256 of the DER certificate - the format Xray expects.</summary>
	public static string Sha256Hex(X509Certificate cert)
	{
		using X509Certificate2 c = new X509Certificate2(cert);
		return Convert.ToHexString(c.GetCertHash(HashAlgorithmName.SHA256)).ToLowerInvariant();
	}

	public static bool SameHash(string? a, string? b)
	{
		return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && string.Equals(a.Replace(":", "").Trim(), b.Replace(":", "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Handshakes with the server (optionally from a specific local address) and returns the leaf certificate hash, or null.</summary>
	public static async Task<string?> FetchLeafSha256Async(IPAddress target, int port, string sni, IPAddress? bindLocal, TimeSpan timeout)
	{
		Socket? socket = null;
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(timeout);
			socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if (bindLocal != null && bindLocal.AddressFamily == target.AddressFamily)
			{
				socket.Bind(new IPEndPoint(bindLocal, 0));
			}
			await socket.ConnectAsync(new IPEndPoint(target, port), cts.Token);
			using NetworkStream net = new NetworkStream(socket, ownsSocket: false);
			string? captured = null;
			using SslStream tls = new SslStream(net, leaveInnerStreamOpen: false, (object _, X509Certificate? cert, X509Chain? _, SslPolicyErrors _) =>
			{
				if (cert != null)
				{
					captured = Sha256Hex(cert);
				}
				return true; // we only read the certificate here; trust is decided by the pin check, not by this probe
			});
			await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
			{
				TargetHost = sni
			}, cts.Token);
			return captured;
		}
		catch (Exception ex)
		{
			Logging.SaveLog("certpin: could not read the server certificate (" + ex.GetType().Name + ")");
			return null;
		}
		finally
		{
			socket?.Dispose();
		}
	}
}

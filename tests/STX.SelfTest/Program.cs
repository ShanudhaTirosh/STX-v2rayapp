// Dependency-free self-test runner (no NuGet needed): `dotnet run -c Release`.
// Covers the pure logic added in the audit. Anything touching Windows APIs, WPF or a real server is NOT covered here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Handler.SysProxy;
using ServiceLib.Manager;
using STXEnterprises.Host.Bridge;
using STXEnterprises.Host.Vpn;

internal static class T
{
	private static int _pass, _fail;

	public static void Check(bool ok, string name)
	{
		if (ok) { _pass++; Console.WriteLine("  ok   " + name); }
		else { _fail++; Console.WriteLine("  FAIL " + name); }
	}

	public static void Throws<TEx>(Action a, string name) where TEx : Exception
	{
		try { a(); Check(false, name + " (no exception)"); }
		catch (TEx) { Check(true, name); }
		catch (Exception e) { Check(false, name + " (wrong exception " + e.GetType().Name + ")"); }
	}

	private sealed class FakeStore : IProxyStore
	{
		public ProxySnapshot Current = new ProxySnapshot();
		public int Clears;
		public ProxySnapshot Read() => JsonRoundTrip(Current);
		public void Apply(ProxySnapshot s) { Current = JsonRoundTrip(s); }
		public void Clear() { Clears++; Current = new ProxySnapshot(); }
		private static ProxySnapshot JsonRoundTrip(ProxySnapshot s) => System.Text.Json.JsonSerializer.Deserialize<ProxySnapshot>(System.Text.Json.JsonSerializer.Serialize(s))!;
	}

	private static ProxySnapshot Proxy(string? server, int enable, string? pac = null)
	{
		var s = new ProxySnapshot();
		s.Strings["ProxyServer"] = server; s.Strings["AutoConfigURL"] = pac; s.Numbers["ProxyEnable"] = enable;
		s.Blobs["DefaultConnectionSettings"] = Convert.ToBase64String(new byte[] { 1, 2, 3, 9 });
		return s;
	}

	public static async Task<int> Main()
	{
		Console.WriteLine("LogRedactor");
		string uuid = "5b3e9c1a-1111-2222-3333-444455556666";
		string r = LogRedactor.Redact($"connect vless://{uuid}@host.example:443?security=tls#name done");
		Check(!r.Contains(uuid) && !r.Contains("host.example") && r.Contains("vless://<redacted>"), "share link removed");
		r = LogRedactor.Redact("profile id " + uuid);
		Check(!r.Contains("1111-2222") && r.StartsWith("profile id 5b3e9c1a-****"), "bare UUID masked, prefix kept for support");
		r = LogRedactor.Redact("2026/11/06 [Info] from tcp:127.0.0.1:61022 accepted tcp:www.google.com:443 [socks -> proxy]");
		Check(!r.Contains("google") && r.Contains("accepted tcp:<redacted>") && r.Contains("[socks -> proxy]"), "visited destination removed from core access line");
		r = LogRedactor.Redact("GET https://sub.example.com/api/sub/AbCdEf0123456789AbCdEf0123?token=SECRET123&x=1");
		Check(!r.Contains("SECRET123") && !r.Contains("AbCdEf0123456789"), "subscription token (path and query) removed");
		r = LogRedactor.Redact(@"started from C:\Users\Someone\OneDrive\Desktop\STX\app.exe");
		Check(!r.Contains("Someone") && r.Contains(@"C:\Users\<user>\"), "Windows user name removed");
		r = LogRedactor.Redact("jwt eyJhbGciOiJIUzI1NiJ9.eyJyb2xlIjoiYW5vbiJ9.abcdEFGH12");
		Check(!r.Contains("eyJhbGci"), "JWT removed");
		Check(LogRedactor.Redact("plain message, port 443") == "plain message, port 443", "ordinary text unchanged");
		Check(LogRedactor.Redact(null) == "" && LogRedactor.Redact("") == "", "null/empty safe");
		string nasty = new string('a', 200000) + "-" + new string('b', 200000);
		var sw = System.Diagnostics.Stopwatch.StartNew(); LogRedactor.Redact(nasty); sw.Stop();
		Check(sw.ElapsedMilliseconds < 3000, $"large hostile input finishes quickly ({sw.ElapsedMilliseconds} ms)");

		Console.WriteLine("AtomicFile");
		string dir = Path.Combine(Path.GetTempPath(), "stxtest-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
		string f = Path.Combine(dir, "settings.json");
		AtomicFile.WriteAllText(f, "{\"a\":1}");
		AtomicFile.WriteAllText(f, "{\"a\":2}");
		Check(File.ReadAllText(f) == "{\"a\":2}", "overwrite replaces content");
		Check(Directory.GetFiles(dir, "*.tmp").Length == 0, "no temp files left behind");
		Check(!File.ReadAllBytes(f).AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }), "no BOM");
		await Task.WhenAll(System.Linq.Enumerable.Range(0, 20).Select(i => AtomicFile.WriteAllTextAsync(Path.Combine(dir, "c.json"), new string('x', 50000 + i))));
		Check(File.ReadAllText(Path.Combine(dir, "c.json")).Length >= 50000, "concurrent writers never leave a truncated file");

		Console.WriteLine("SystemProxyGuard");
		string bk = Path.Combine(dir, "proxy-backup.json");
		var store = new FakeStore { Current = Proxy("corp-proxy:8080", 1, "http://pac.corp/wpad.dat") };
		var guard = new SystemProxyGuard(store, bk);
		guard.Capture("127.0.0.1:10808");
		store.Current = Proxy("127.0.0.1:10808", 1); // STX sets its proxy
		Check(guard.Restore(), "restore reports it restored something");
		Check(store.Current.ProxyServer == "corp-proxy:8080" && store.Current.Strings["AutoConfigURL"] == "http://pac.corp/wpad.dat", "user's own proxy/PAC put back");
		Check(store.Current.Blobs["DefaultConnectionSettings"] == Convert.ToBase64String(new byte[] { 1, 2, 3, 9 }), "binary auto-detect settings put back");
		Check(!guard.HasBackup, "backup removed after restore");
		Check(!guard.Restore(), "second restore is a no-op");

		store = new FakeStore { Current = Proxy("corp-proxy:8080", 1) }; guard = new SystemProxyGuard(store, bk);
		guard.Capture("127.0.0.1:10808"); store.Current = Proxy("127.0.0.1:10808", 1);
		guard.Capture("127.0.0.1:10808"); // reconnect while still captured
		guard.Restore();
		Check(store.Current.ProxyServer == "corp-proxy:8080", "a second Capture never overwrites the real original");

		// crash: backup file stays, new process recovers
		store = new FakeStore { Current = Proxy("corp-proxy:8080", 1) }; guard = new SystemProxyGuard(store, bk);
		guard.Capture("127.0.0.1:10808"); store.Current = Proxy("127.0.0.1:10808", 1);
		var guard2 = new SystemProxyGuard(store, bk);
		Check(guard2.RecoverStale(SystemProxyGuard.IsLoopbackProxy) && store.Current.ProxyServer == "corp-proxy:8080", "crash recovery restores the user's proxy at next start");

		// no backup, stale loopback proxy
		store = new FakeStore { Current = Proxy("127.0.0.1:10808", 1) }; guard = new SystemProxyGuard(store, bk);
		Check(guard.RecoverStale(s => SystemProxyGuard.IsLoopbackProxyOnPort(s, 10808)) && store.Clears == 1, "stale STX proxy without a backup is cleared");
		// no backup, user's own proxy on another port must be left alone
		store = new FakeStore { Current = Proxy("127.0.0.1:8888", 1) }; guard = new SystemProxyGuard(store, bk);
		Check(!guard.RecoverStale(s => SystemProxyGuard.IsLoopbackProxyOnPort(s, 10808)) && store.Clears == 0, "someone else's local proxy (Fiddler etc.) is not touched");
		store = new FakeStore { Current = Proxy("corp-proxy:8080", 1) }; guard = new SystemProxyGuard(store, bk);
		Check(!guard.RecoverStale(SystemProxyGuard.IsLoopbackProxy) && store.Clears == 0, "a remote corporate proxy is not touched");
		Check(SystemProxyGuard.IsLoopbackProxy("http=127.0.0.1:10808;https=127.0.0.1:10808") && SystemProxyGuard.IsLoopbackProxy("socks=127.0.0.1:1080"), "per-protocol proxy strings recognised");
		Check(!SystemProxyGuard.IsLoopbackProxyOnPort("127.0.0.1:108080", 10808), "port match is exact");

		Console.WriteLine("CoreProcesses.PathIsUnder");
		string inst = Path.Combine(dir, "STX");
		Check(CoreProcesses.PathIsUnder(Path.Combine(inst, "bin", "xray", "xray"), inst), "core inside the install dir matches");
		Check(!CoreProcesses.PathIsUnder(Path.Combine(dir, "STX-old", "bin", "xray"), inst), "sibling folder with the same prefix does NOT match");
		Check(!CoreProcesses.PathIsUnder(Path.Combine(dir, "v2rayN", "xray"), inst), "another client's xray does not match");
		Check(!CoreProcesses.PathIsUnder(null, inst) && !CoreProcesses.PathIsUnder("x", ""), "null/empty never match");
		Check(CoreProcesses.PathIsUnder(Path.Combine(inst, "..", "STX", "bin", "xray"), inst + Path.DirectorySeparatorChar), "dot-dot and trailing separator normalised");

		Console.WriteLine("BridgeFetchPolicy");
		BridgeFetchPolicy.ValidateUrl("https://gorqunkfwiddhmihrkcv.supabase.co/rest/v1/configs?select=*");
		BridgeFetchPolicy.ValidateUrl("https://api.github.com/repos/shanudhatirosh/stx-vpn-client/releases/latest");
		Check(true, "backend and update URLs allowed");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("http://api.github.com/x"), "plain http refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://127.0.0.1:10812/"), "loopback IP refused (Xray API port)");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://192.168.1.1/admin"), "LAN router refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://localhost/"), "localhost refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://evil.example/"), "unknown host refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://api.github.com.evil.example/"), "look-alike host refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("https://user:pw@api.github.com/"), "embedded credentials refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateUrl("file:///C:/Windows/win.ini"), "file URL refused");
		Throws<ArgumentException>(() => BridgeFetchPolicy.ValidateMethod("TRACE"), "TRACE refused");
		Check(!BridgeFetchPolicy.HeaderAllowed("Host") && !BridgeFetchPolicy.HeaderAllowed("Content-Length") && BridgeFetchPolicy.HeaderAllowed("apikey"), "header filter");
		Check(!BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("10.1.2.3")) && !BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("169.254.1.1")) && !BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("::1")) && !BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("fd00::1")) && !BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("::ffff:192.168.0.1")), "private/loopback/link-local/ULA/mapped ranges rejected");
		Check(BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("1.1.1.1")) && BridgeFetchPolicy.IsPublicAddress(IPAddress.Parse("2606:4700:4700::1111")), "public addresses accepted");
		try { await BridgeFetchPolicy.EnsurePublicRemoteAsync("https://127.0.0.1/sub"); Check(false, "subscription URL to loopback refused"); } catch (ArgumentException) { Check(true, "subscription URL to loopback refused"); }

		Console.WriteLine("OriginPolicy");
		Check(OriginPolicy.IsTrusted("https://stxenterprises.local/index.html") && OriginPolicy.IsTrusted("https://stx.local/"), "own pages trusted");
		Check(!OriginPolicy.IsTrusted("https://evil.example/") && !OriginPolicy.IsTrusted("http://stx.local/") && !OriginPolicy.IsTrusted("https://stx.local.evil.example/") && !OriginPolicy.IsTrusted(null), "foreign / downgraded / look-alike origins rejected");
		Check(!OriginPolicy.IsTrusted("https://pxn.local/"), "legacy pxn.local alias no longer trusted");

		Console.WriteLine("StxCertPin.Decide / hashing");
		Check(StxCertPin.Decide("tls", "true", "", "", true) == StxCertPin.Need.LearnPin, "explicit allowInsecure -> learn pin");
		Check(StxCertPin.Decide("tls", "", "", "", true) == StxCertPin.Need.LearnPin, "unset allowInsecure with insecure default -> learn pin");
		Check(StxCertPin.Decide("tls", "", "", "", false) == StxCertPin.Need.Nothing, "unset allowInsecure, secure default -> nothing");
		Check(StxCertPin.Decide("tls", "false", "", "", true) == StxCertPin.Need.Nothing, "explicit allowInsecure=false -> nothing");
		Check(StxCertPin.Decide("tls", "true", "abcd", "", true) == StxCertPin.Need.Nothing, "already pinned -> nothing");
		Check(StxCertPin.Decide("reality", "true", "", "", true) == StxCertPin.Need.Nothing, "non-TLS -> nothing");
		Check(StxCertPin.SameHash("AB:CD:EF", "abcdef") && !StxCertPin.SameHash("ab", "cd") && !StxCertPin.SameHash(null, "ab"), "hash comparison ignores case/colons");
		// real handshake against a local TLS server
		using var cert = System.Security.Cryptography.X509Certificates.CertificateRequest_Helper.Make();
		string expected = Convert.ToHexString(cert.GetCertHash(System.Security.Cryptography.HashAlgorithmName.SHA256)).ToLowerInvariant();
		var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		var serverTask = Task.Run(async () =>
		{
			using var c = await listener.AcceptTcpClientAsync();
			using var ssl = new System.Net.Security.SslStream(c.GetStream());
			await ssl.AuthenticateAsServerAsync(cert);
			await Task.Delay(200);
		});
		string? got = await StxCertPin.FetchLeafSha256Async(IPAddress.Loopback, port, "m.tiktok.com", null, TimeSpan.FromSeconds(5));
		Check(got == expected, "learned hash equals the server certificate's SHA-256 (works with a spoofed SNI)");
		Check(await StxCertPin.FetchLeafSha256Async(IPAddress.Loopback, 1, "x", null, TimeSpan.FromSeconds(1)) == null, "unreachable server -> null, no exception");
		try { await serverTask; } catch { }
		listener.Stop();

		Console.WriteLine("Logging.PruneOldLogs");
		string logs = Path.Combine(dir, "logs"); Directory.CreateDirectory(logs);
		File.WriteAllText(Path.Combine(logs, "old.txt"), "x"); File.SetLastWriteTimeUtc(Path.Combine(logs, "old.txt"), DateTime.UtcNow.AddDays(-30));
		File.WriteAllText(Path.Combine(logs, "new.txt"), "x");
		Check(Logging.PruneOldLogs(logs, 7) == 1 && File.Exists(Path.Combine(logs, "new.txt")) && !File.Exists(Path.Combine(logs, "old.txt")), "old logs deleted, recent kept");

		Directory.Delete(dir, true);
		Console.WriteLine($"\n{_pass} passed, {_fail} failed");
		return _fail == 0 ? 0 : 1;
	}
}

namespace System.Security.Cryptography.X509Certificates
{
	internal static class CertificateRequest_Helper
	{
		public static X509Certificate2 Make()
		{
			using var rsa = RSA.Create(2048);
			var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			var c = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
			return new X509Certificate2(c.Export(X509ContentType.Pfx));
		}
	}
}

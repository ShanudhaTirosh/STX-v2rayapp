using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace ServiceLib.Manager;

public static class StxUplink
{
	private sealed record Probe(string Name, string Address, string Description, bool Ok, int Milliseconds, string? Why);

	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(4L);

	private static readonly string[] TunnelMarkers = new string[9] { "wireguard", "wintun", "tap-windows", "tap-win", "openvpn", "tunnel", "vpn", "tailscale", "zerotier" };

	private static readonly string[] NeverAWayOut = new string[16]
	{
		"pxn shield", "singbox_tun", "singbox", "sing-box", "utun", "pxn", "loopback", "vethernet", "wsl", "docker",
		"virtualbox", "vmware", "teredo", "isatap", "bluetooth", "wan miniport"
	};

	private static List<string> _report = new List<string>();

	public static UplinkChoice? Current { get; private set; }

	public static string? LastReason { get; private set; }

	public static IReadOnlyList<string> LastReport => _report;

	public static void Clear()
	{
		Current = null;
	}

	public static async Task<UplinkChoice?> ChooseAsync(string? serverAddress, int port, string? sni)
	{
		Current = null;
		LastReason = null;
		_report = new List<string>();
		try
		{
			IPAddress target = TargetIp(serverAddress);
			if (target == null)
			{
				return FromWindows(WindowsChoiceFor(IPAddress.Parse("1.1.1.1")), "no server address to test against");
			}
			string windowsPick = WindowsChoiceFor(target);
			List<(string, string, string)> list = Candidates();
			if (list.Count == 0)
			{
				return FromWindows(windowsPick, "no adapter passed the filter");
			}
			if (list.Count == 1)
			{
				Probe probe = await Reaches(list[0], target, port, sni);
				Current = new UplinkChoice(probe.Name, probe.Address, probe.Milliseconds, IsTunnel(probe.Name, probe.Description), probe.Name.Equals(windowsPick, StringComparison.OrdinalIgnoreCase));
				Say(probe.Ok ? $"uplink: using {probe.Name}, the only way out, which reached the server in {probe.Milliseconds}ms" : $"uplink: using {probe.Name}, the only way out — it could not reach the server ({probe.Why}), but naming it still keeps the tunnel from routing into itself");
				return Current;
			}
			Probe[] source = await Task.WhenAll(list.Select(((string Name, string Address, string Description) c) => Reaches(c, target, port, sni)));
			List<Probe> list2 = (from r in source
				where r.Ok
				orderby r.Milliseconds
				select r).ToList();
			foreach (Probe item in from r in source
				orderby (!r.Ok) ? 1 : 0, r.Milliseconds
				select r)
			{
				Say($"uplink: {item.Name} ({item.Address}) {(item.Ok ? $"reached the server in {item.Milliseconds}ms" : ("could not reach the server — " + item.Why))}");
			}
			if (list2.Count == 0)
			{
				return FromWindows(windowsPick, "nothing reached the server");
			}
			Probe probe2 = list2[0];
			Probe probe3 = list2.FirstOrDefault((Probe r) => r.Name.Equals(windowsPick, StringComparison.OrdinalIgnoreCase));
			bool flag = (object)probe3 != null;
			if (flag && probe3.Milliseconds <= checked(probe2.Milliseconds * 2))
			{
				probe2 = probe3;
			}
			Current = new UplinkChoice(probe2.Name, probe2.Address, probe2.Milliseconds, IsTunnel(probe2.Name, probe2.Description), flag);
			Say(flag ? ("uplink: using " + probe2.Name + ", which is also what Windows would have used") : $"uplink: using {probe2.Name} — Windows would have used {windowsPick ?? "nothing"}, which could not reach the server");
			return Current;
		}
		catch (Exception ex)
		{
			Say("uplink: could not choose one — " + ex.GetType().Name + ": " + ex.Message);
			Current = null;
			return null;
		}
	}

	private static void Say(string message)
	{
		LastReason = message;
		_report.Add(message);
		try
		{
			Logging.SaveLog(message);
		}
		catch
		{
		}
	}

	private static async Task<Probe> Reaches((string Name, string Address, string Description) nic, IPAddress target, int port, string? sni)
	{
		Stopwatch clock = Stopwatch.StartNew();
		Socket socket = null;
		SslStream tls = null;
		checked
		{
			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource(Patience);
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(new IPEndPoint(IPAddress.Parse(nic.Address), 0));
				await socket.ConnectAsync(new IPEndPoint(target, port), cts.Token);
				NetworkStream innerStream = new NetworkStream(socket, ownsSocket: false);
				tls = new SslStream(innerStream, leaveInnerStreamOpen: false, (object _, X509Certificate? _, X509Chain? _, SslPolicyErrors _) => true);
				await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = (sni.IsNullOrEmpty() ? target.ToString() : sni)
				}, cts.Token);
				clock.Stop();
				return new Probe(nic.Name, nic.Address, nic.Description, Ok: true, (int)clock.ElapsedMilliseconds, null);
			}
			catch (Exception ex)
			{
				clock.Stop();
				return new Probe(nic.Name, nic.Address, nic.Description, Ok: false, (int)clock.ElapsedMilliseconds, (ex is OperationCanceledException) ? "no answer" : ex.GetType().Name);
			}
			finally
			{
				tls?.Dispose();
				socket?.Dispose();
			}
		}
	}

	private static List<(string Name, string Address, string Description)> Candidates()
	{
		List<(string, string, string)> list = new List<(string, string, string)>();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			if (networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback || Matches(networkInterface.Name, NeverAWayOut) || Matches(networkInterface.Description, NeverAWayOut))
			{
				continue;
			}
			IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
			if (iPProperties.GatewayAddresses.Any((GatewayIPAddressInformation g) => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)))
			{
				string text = iPProperties.UnicastAddresses.FirstOrDefault((UnicastIPAddressInformation a) => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
				if (!text.IsNullOrEmpty())
				{
					list.Add((networkInterface.Name, text, networkInterface.Description ?? string.Empty));
				}
			}
		}
		return list;
	}

	private static bool IsTunnel(string name, string description)
	{
		if (!Matches(name, TunnelMarkers))
		{
			return Matches(description, TunnelMarkers);
		}
		return true;
	}

	private static bool Matches(string? text, string[] markers)
	{
		if (text.IsNullOrEmpty())
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		return markers.Any(text2.Contains);
	}

	private static IPAddress? TargetIp(string? serverAddress)
	{
		if (serverAddress.IsNullOrEmpty())
		{
			return null;
		}
		if (IPAddress.TryParse(serverAddress, out IPAddress address))
		{
			return address;
		}
		string text = StxServerAddress.Get(serverAddress);
		if (text == null || !IPAddress.TryParse(text, out IPAddress address2))
		{
			return null;
		}
		return address2;
	}

	private static UplinkChoice? FromWindows(string? name, string why)
	{
		if (name.IsNullOrEmpty() || Matches(name, NeverAWayOut))
		{
			(string, string, string) tuple = Candidates().FirstOrDefault();
			if (tuple.Item1.IsNullOrEmpty())
			{
				Say("uplink: " + why + ", and nothing on this machine looks like a way out; leaving the choice to sing-box");
				return null;
			}
			Current = new UplinkChoice(tuple.Item1, tuple.Item2, 0, IsTunnel(tuple.Item1, tuple.Item3), WindowsAgreed: false);
			Say(name.IsNullOrEmpty() ? $"uplink: {why}, and the system named none; using {tuple.Item1}, which at least has a gateway" : $"uplink: {why}, and the system points at {name}, which is our own tunnel; using {tuple.Item1} instead");
			return Current;
		}
		NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault((NetworkInterface n) => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		string address = networkInterface?.GetIPProperties().UnicastAddresses.FirstOrDefault((UnicastIPAddressInformation a) => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? string.Empty;
		Current = new UplinkChoice(name, address, 0, IsTunnel(name, networkInterface?.Description ?? string.Empty), WindowsAgreed: true);
		Say($"uplink: {why}; using {name}, which is what the system itself would use");
		return Current;
	}

	private static string? WindowsChoiceFor(IPAddress target)
	{
		try
		{
			using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Connect(new IPEndPoint(target, 53));
			string local = (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
			if (local.IsNullOrEmpty())
			{
				return null;
			}
			return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault((NetworkInterface n) => n.GetIPProperties().UnicastAddresses.Any((UnicastIPAddressInformation a) => a.Address.ToString().Equals(local, StringComparison.Ordinal)))?.Name;
		}
		catch
		{
			return null;
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace ServiceLib.Manager;

public static class StxServerAddress
{
	private const string FileName = "stx-known-addresses.json";

	private static readonly ConcurrentDictionary<string, string> _pinned = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly SemaphoreSlim _io = new SemaphoreSlim(1, 1);

	private static bool _loaded;

	public static (string Ip, int Port)? DirectProbe { get; private set; }

	public static void SetDirectProbe(string? ip, int port)
	{
		DirectProbe = ((ip.IsNullOrEmpty() || port <= 0) ? (((string, int)?)null) : new (string, int)?((ip, port)));
	}

	public static string? Get(string? domain)
	{
		if (domain.IsNullOrEmpty())
		{
			return null;
		}
		if (!_pinned.TryGetValue(domain, out string value))
		{
			return null;
		}
		return value;
	}

	public static async Task PinAsync(string? domain)
	{
		if (domain.IsNullOrEmpty() || !Utils.IsDomain(domain))
		{
			return;
		}
		await LoadAsync();
		string? text = null;
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0));
			IPAddress[] source = await Dns.GetHostAddressesAsync(domain, cts.Token);
			IPAddress? iPAddress = source.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork) ?? source.FirstOrDefault();
			if (iPAddress != null)
			{
				text = iPAddress.ToString();
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog($"address: OS DNS could not resolve {domain} ({ex.GetType().Name}); trying Cloudflare DoH");
		}

		// Zero MB fallback: Query Cloudflare DNS-over-HTTPS via direct IP
		if (string.IsNullOrEmpty(text))
		{
			text = await ResolveViaCloudflareDohAsync(domain);
		}

		if (!string.IsNullOrEmpty(text))
		{
			if (_pinned.TryGetValue(domain, out string? value) && value == text)
			{
				return;
			}
			_pinned[domain] = text;
			Logging.SaveLog("address: pinned " + domain + " to " + text);
			await SaveAsync();
		}
		else
		{
			Logging.SaveLog($"address: could not resolve {domain}; keeping {Get(domain) ?? "nothing"}");
		}
	}

	public static async Task<string?> ResolveAsync(string? domain)
	{
		if (domain.IsNullOrEmpty())
		{
			return null;
		}
		if (IPAddress.TryParse(domain, out _))
		{
			return domain;
		}
		string? pinned = Get(domain);
		if (!string.IsNullOrEmpty(pinned))
		{
			return pinned;
		}
		string? resolved = await ResolveViaCloudflareDohAsync(domain);
		if (!string.IsNullOrEmpty(resolved))
		{
			_pinned[domain] = resolved;
			return resolved;
		}
		return null;
	}

	public static async Task<string?> ResolveViaCloudflareDohAsync(string domain)
	{
		string[] dohIps = new string[3] { "1.1.1.1", "1.0.0.1", "162.159.38.1" };
		using HttpClient client = new HttpClient();
		client.Timeout = TimeSpan.FromSeconds(3.5);
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/dns-json"));

		foreach (string ip in dohIps)
		{
			try
			{
				string url = $"https://{ip}/dns-query?name={Uri.EscapeDataString(domain)}&type=A";
				using HttpResponseMessage resp = await client.GetAsync(url);
				if (resp.IsSuccessStatusCode)
				{
					string body = await resp.Content.ReadAsStringAsync();
					using JsonDocument doc = JsonDocument.Parse(body);
					if (doc.RootElement.TryGetProperty("Answer", out JsonElement answerElem) && answerElem.ValueKind == JsonValueKind.Array)
					{
						foreach (JsonElement ans in answerElem.EnumerateArray())
						{
							if (ans.TryGetProperty("data", out JsonElement dataElem))
							{
								string? val = dataElem.GetString();
								if (!string.IsNullOrEmpty(val) && IPAddress.TryParse(val, out IPAddress? parsed) && parsed.AddressFamily == AddressFamily.InterNetwork)
								{
									Logging.SaveLog($"address: Cloudflare DoH ({ip}) resolved {domain} -> {val}");
									return val;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logging.SaveLog($"address: Cloudflare DoH ({ip}) probe failed: {ex.Message}");
			}
		}
		return null;
	}

	private static async Task LoadAsync()
	{
		if (_loaded)
		{
			return;
		}
		await _io.WaitAsync();
		try
		{
			if (_loaded)
			{
				return;
			}
			_loaded = true;
			string configPath = Utils.GetConfigPath("stx-known-addresses.json");
			if (!File.Exists(configPath))
			{
				configPath = Utils.GetConfigPath("pxn-known-addresses.json");
			}
			if (!File.Exists(configPath))
			{
				return;
			}
			foreach (KeyValuePair<string, string> item in JsonUtils.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(configPath)) ?? new Dictionary<string, string>())
			{
				if (Utils.IsIpAddress(item.Value))
				{
					_pinned.TryAdd(item.Key, item.Value);
				}
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("address: could not read the known-address file", ex);
		}
		finally
		{
			_io.Release();
		}
	}

	private static async Task SaveAsync()
	{
		await _io.WaitAsync();
		try
		{
			await AtomicFile.WriteAllTextAsync(Utils.GetConfigPath("stx-known-addresses.json"), JsonUtils.Serialize(new Dictionary<string, string>(_pinned)));
		}
		catch (Exception ex)
		{
			Logging.SaveLog("address: could not write the known-address file", ex);
		}
		finally
		{
			_io.Release();
		}
	}
}

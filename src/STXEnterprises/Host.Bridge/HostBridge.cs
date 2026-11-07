using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using STXEnterprises.Host.Vpn;
using ServiceLib.Common;
using ServiceLib.Manager;

namespace STXEnterprises.Host.Bridge;

public sealed class HostBridge
{
	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { (JsonConverter)new JsonStringEnumConverter() },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly HttpClient Http = BuildHttp();

	private readonly IHostView _view;

	private readonly TunnelController _tunnel;

	private readonly HostSettings _settings;

	private readonly LoopbackAuth _auth = new LoopbackAuth();

	private readonly Func<Task> _quit;

	private readonly Action _minimise;

	private static HttpClient BuildHttp()
	{
		HttpClient httpClient = new HttpClient();
		httpClient.Timeout = TimeSpan.FromSeconds(30L);
		string text = typeof(HostBridge).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("STXEnterprises/" + text);
		return httpClient;
	}

	public HostBridge(IHostView view, TunnelController tunnel, HostSettings settings, Func<Task> quit, Action minimise)
	{
		_view = view;
		_tunnel = tunnel;
		_settings = settings;
		_quit = quit;
		_minimise = minimise;
		_view.MessageReceived += OnMessage;
		_tunnel.StatusChanged += (TunnelStatus status) =>
		{
			PostEvent("status", status);
		};
		_tunnel.Notice += (string text) =>
		{
			PostEvent("notice", new { text });
		};
	}

	private async void OnMessage(string message)
	{
		JsonNode jsonNode;
		try
		{
			jsonNode = JsonNode.Parse(message);
		}
		catch
		{
			return;
		}
		int id = (jsonNode?["id"]?.GetValue<int>()).GetValueOrDefault();
		string method = jsonNode?["method"]?.GetValue<string>();
		JsonNode p = jsonNode?["params"];
		if (method == null)
		{
			return;
		}
		try
		{
			int id2 = id;
			Reply(id2, ok: true, await Dispatch(method, p), null);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("bridge " + method, ex);
			Reply(id, ok: false, null, Friendly(ex));
		}
	}

	private async Task<object?> Dispatch(string method, JsonNode? p)
	{
		return method switch
		{
			"hello" => await HelloAsync(), 
			"status" => _tunnel.Snapshot(), 
			"listConfigs" => await _tunnel.ListConfigsAsync(), 
			"syncConfigs" => await SyncAsync(p), 
			"importLink" => Err(await _tunnel.ImportLinkAsync(Str(p, "text"))), 
			"removeConfig" => Err(await _tunnel.RemoveConfigAsync(Str(p, "id"))), 
			"exportConfig" => await _tunnel.ExportConfigAsync(Str(p, "id")), 
			"configDetail" => await _tunnel.ConfigDetailAsync(Str(p, "id")), 
			"setConfigDetail" => await SetConfigDetailAsync(p), 
			"connect" => Err(await _tunnel.ConnectAsync(Str(p, "id"))), 
			"disconnect" => await Nothing(_tunnel.DisconnectAsync()), 
			"reconnect" => Err(await _tunnel.ReconnectAsync()), 
			"setMode" => Err(await _tunnel.SetModeAsync(Str(p, "mode"))), 
			"ping" => new
			{
				ms = await _tunnel.PingAsync(Str(p, "id"))
			}, 
			"clearCache" => new
			{
				freed = await _tunnel.ClearCacheAsync()
			}, 
			"listApps" => AppInventory.List(), 
			"setSplitTunnel" => await SetSplitTunnelAsync(p), 
			"setBlockAds" => await SetBlockAdsAsync(p), 
			"setTcpFastOpen" => await SetTcpFastOpenAsync(p), 
			"subUsage" => await SubscriptionUsage.ReadAsync(Str(p, "url")), 
			"hostFetch" => await HostFetchAsync(p), 
			"getSettings" => Settings(), 
			"setSettings" => await SetSettingsAsync(p), 
			"oauthStart" => _auth.Start(), 
			"oauthAwait" => await _auth.AwaitCodeAsync(TimeSpan.FromMinutes(5L)), 
			"openExternal" => OpenExternal(Str(p, "url")), 
			"getLog" => await ReadLogAsync(), 
			"minimise" => Do(_minimise), 
			"quit" => await Nothing(_quit()), 
			_ => throw new InvalidOperationException("Unknown method: " + method), 
		};
	}

	private async Task<object> HelloAsync()
	{
		CoreAssets.CoreCheck coreCheck = await CoreAssets.VerifyXrayAsync();
		return new
		{
			appVersion = (typeof(HostBridge).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"),
			coreVersion = coreCheck.Version,
			coreOk = coreCheck.Ok,
			coreProblem = coreCheck.Problem,
			tunAvailable = _tunnel.TunAvailable,
			socksPort = _tunnel.SocksPort,
			httpPort = _tunnel.HttpPort,
			settings = Settings(),
			status = _tunnel.Snapshot()
		};
	}

	private async Task<object> SyncAsync(JsonNode? p)
	{
		List<TunnelController.IncomingConfig> incoming = ((p != null) ? p["configs"].Deserialize<List<TunnelController.IncomingConfig>>(Json) : null) ?? new List<TunnelController.IncomingConfig>();
		return new
		{
			synced = await _tunnel.SyncConfigsAsync(incoming)
		};
	}

	private async Task<object> SetSplitTunnelAsync(JsonNode? p)
	{
		_settings.SplitMode = p?["mode"]?.GetValue<string>() ?? "all";
		_settings.SplitApps = ((p != null) ? p["apps"].Deserialize<List<string>>(Json) : null) ?? new List<string>();
		_settings.Save();
		string problem = await _tunnel.ApplySplitTunnelAsync(_settings.SplitMode, _settings.SplitApps);
		return new
		{
			mode = _settings.SplitMode,
			apps = _settings.SplitApps,
			problem = problem
		};
	}

	private async Task<object> SetConfigDetailAsync(JsonNode? p)
	{
		bool flag = p != null && p["reset"]?.GetValue<bool>() == true;
		StxConfigFields stxConfigFields = new StxConfigFields();
		if (!flag && p?["patch"] is JsonObject jsonObject)
		{
			JsonNode jsonNode = jsonObject["address"];
			if (jsonNode != null)
			{
				stxConfigFields.Address = jsonNode.GetValue<string>();
			}
			JsonNode jsonNode2 = jsonObject["port"];
			if (jsonNode2 != null)
			{
				stxConfigFields.Port = jsonNode2.GetValue<int>();
			}
			JsonNode jsonNode3 = jsonObject["sni"];
			if (jsonNode3 != null)
			{
				stxConfigFields.Sni = jsonNode3.GetValue<string>();
			}
			JsonNode jsonNode4 = jsonObject["allowInsecure"];
			if (jsonNode4 != null)
			{
				stxConfigFields.AllowInsecure = (jsonNode4.GetValue<bool>() ? "true" : "false");
			}
			JsonNode jsonNode5 = jsonObject["fingerprint"];
			if (jsonNode5 != null)
			{
				stxConfigFields.Fingerprint = jsonNode5.GetValue<string>();
			}
			JsonNode jsonNode6 = jsonObject["alpn"];
			if (jsonNode6 != null)
			{
				stxConfigFields.Alpn = jsonNode6.GetValue<string>();
			}
			JsonNode jsonNode7 = jsonObject["host"];
			if (jsonNode7 != null)
			{
				stxConfigFields.Host = jsonNode7.GetValue<string>();
			}
			JsonNode jsonNode8 = jsonObject["path"];
			if (jsonNode8 != null)
			{
				stxConfigFields.Path = jsonNode8.GetValue<string>();
			}
		}
		return new
		{
			problem = await _tunnel.SetConfigDetailAsync(Str(p, "id"), stxConfigFields, flag),
			detail = await _tunnel.ConfigDetailAsync(Str(p, "id"))
		};
	}

	private async Task<object> SetTcpFastOpenAsync(JsonNode? p)
	{
		_settings.TcpFastOpen = p?["enabled"]?.GetValue<bool>() == true;
		_settings.Save();
		string problem = await _tunnel.ApplyTcpFastOpenAsync(_settings.TcpFastOpen);
		return new
		{
			enabled = _settings.TcpFastOpen,
			problem = problem
		};
	}

	private async Task<object> SetBlockAdsAsync(JsonNode? p)
	{
		_settings.BlockAds = p?["enabled"]?.GetValue<bool>() == true;
		_settings.Save();
		string problem = await _tunnel.ApplyAdBlockAsync(_settings.BlockAds);
		return new
		{
			enabled = _settings.BlockAds,
			problem = problem
		};
	}

	private object Settings()
	{
		return new
		{
			AutoRun = _settings.AutoRun,
			AutoConnect = _settings.AutoConnect,
			MinimiseToTray = _settings.MinimiseToTray,
			StartMinimised = _settings.StartMinimised,
			Language = _settings.Language,
			Theme = _settings.Theme,
			LastConfigId = _settings.LastConfigId,
			SplitMode = _settings.SplitMode,
			SplitApps = _settings.SplitApps,
			BlockAds = _settings.BlockAds,
			TcpFastOpen = _settings.TcpFastOpen,
			mode = _tunnel.Mode
		};
	}

	private async Task<object> SetSettingsAsync(JsonNode? p)
	{
		bool autoRunWas = _settings.AutoRun;
		JsonNode jsonNode = p?["autoRun"];
		if (jsonNode != null)
		{
			_settings.AutoRun = jsonNode.GetValue<bool>();
		}
		JsonNode jsonNode2 = p?["autoConnect"];
		if (jsonNode2 != null)
		{
			_settings.AutoConnect = jsonNode2.GetValue<bool>();
		}
		JsonNode jsonNode3 = p?["minimiseToTray"];
		if (jsonNode3 != null)
		{
			_settings.MinimiseToTray = jsonNode3.GetValue<bool>();
		}
		JsonNode jsonNode4 = p?["startMinimised"];
		if (jsonNode4 != null)
		{
			_settings.StartMinimised = jsonNode4.GetValue<bool>();
		}
		JsonNode jsonNode5 = p?["language"];
		if (jsonNode5 != null)
		{
			_settings.Language = jsonNode5.GetValue<string>();
		}
		JsonNode jsonNode6 = p?["theme"];
		if (jsonNode6 != null)
		{
			_settings.Theme = jsonNode6.GetValue<string>();
		}
		JsonNode jsonNode7 = p?["lastConfigId"];
		if (jsonNode7 != null)
		{
			_settings.LastConfigId = jsonNode7.GetValue<string>();
		}
		JsonNode jsonNode8 = p?["blockAds"];
		if (jsonNode8 != null)
		{
			_settings.BlockAds = jsonNode8.GetValue<bool>();
			await _tunnel.ApplyAdBlockAsync(_settings.BlockAds);
		}
		_settings.Save();
		if (_settings.AutoRun != autoRunWas)
		{
			await _settings.ApplyAutoRunAsync();
		}
		return Settings();
	}

	private async Task<object> HostFetchAsync(JsonNode? p)
	{
		string requestUri = Str(p, "url");
		string method = p?["method"]?.GetValue<string>() ?? "GET";
		using HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), requestUri);
		if (p?["headers"] is JsonObject jsonObject)
		{
			foreach (var (name, jsonNode2) in jsonObject)
			{
				request.Headers.TryAddWithoutValidation(name, jsonNode2?.GetValue<string>());
			}
		}
		string text2 = p?["body"]?.GetValue<string>();
		if (text2 != null && text2.Length > 0)
		{
			request.Content = new StringContent(text2, Encoding.UTF8, p?["contentType"]?.GetValue<string>() ?? "application/json");
		}
		using HttpResponseMessage response = await Http.SendAsync(request);
		int statusCode = (int)response.StatusCode;
		bool isSuccessStatusCode = response.IsSuccessStatusCode;
		return new
		{
			status = statusCode,
			ok = isSuccessStatusCode,
			body = await response.Content.ReadAsStringAsync(),
			headers = response.Headers.Concat(response.Content.Headers).ToDictionary((KeyValuePair<string, IEnumerable<string>> h) => h.Key.ToLowerInvariant(), (KeyValuePair<string, IEnumerable<string>> h) => string.Join(", ", h.Value))
		};
	}

	private static object? OpenExternal(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
		{
			throw new ArgumentException("That link can't be opened.");
		}
		Process.Start(new ProcessStartInfo(result.AbsoluteUri)
		{
			UseShellExecute = true
		});
		return null;
	}

	private static async Task<object> ReadLogAsync()
	{
		_ = 1;
		try
		{
			FileInfo fileInfo = (from f in new DirectoryInfo(Utils.GetLogPath()).GetFiles("*.txt")
				orderby f.LastWriteTime descending
				select f).FirstOrDefault();
			if (fileInfo == null)
			{
				return new
				{
					text = "Nothing logged yet."
				};
			}
			object result;
			await using (FileStream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using StreamReader reader = new StreamReader(stream);
				string text = await reader.ReadToEndAsync();
				string text2;
				if (text.Length <= 60000)
				{
					text2 = text;
				}
				else
				{
					text2 = text.Substring(text.Length - 60000);
				}
				result = new
				{
					text = text2
				};
			}
			return result;
		}
		catch (Exception ex)
		{
			return new
			{
				text = "Couldn't read the log: " + ex.Message
			};
		}
	}

	private static object? Err(string? problem)
	{
		if (problem != null)
		{
			throw new InvalidOperationException(problem);
		}
		return null;
	}

	private static async Task<object?> Nothing(Task task)
	{
		await task;
		return null;
	}

	private static object? Do(Action action)
	{
		action();
		return null;
	}

	private static string Str(JsonNode? p, string key)
	{
		return p?[key]?.GetValue<string>() ?? throw new ArgumentException("Missing '" + key + "'.");
	}

	private void Reply(int id, bool ok, object? result, string? error)
	{
		Post(new JsonObject
		{
			["id"] = id,
			["kind"] = "reply",
			["ok"] = ok,
			["result"] = ((result == null) ? null : JsonSerializer.SerializeToNode(result, Json)),
			["error"] = error
		});
	}

	private void PostEvent(string name, object payload)
	{
		Post(new JsonObject
		{
			["kind"] = "event",
			["name"] = name,
			["payload"] = JsonSerializer.SerializeToNode(payload, Json)
		});
	}

	private void Post(JsonNode message)
	{
		_view.PostJson(message.ToJsonString(Json));
	}

	private static string Friendly(Exception ex)
	{
		if (!(ex is UnauthorizedAccessException))
		{
			if (!(ex is TimeoutException))
			{
				if (ex is HttpRequestException)
				{
					return "Couldn't reach STX Enterprises. Check your connection.";
				}
				return ex.Message;
			}
			return ex.Message;
		}
		return "STX Enterprises needs administrator rights to set up the tunnel. Restart it and accept the prompt.";
	}
}

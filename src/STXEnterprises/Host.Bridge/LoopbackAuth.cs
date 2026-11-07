using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace STXEnterprises.Host.Bridge;

public sealed class LoopbackAuth : IDisposable
{
	private HttpListener? _listener;

	private TaskCompletionSource<string>? _pending;

	public string Start()
	{
		Stop();
		int value = FreeLoopbackPort();
		string text = $"http://127.0.0.1:{value}/";
		_listener = new HttpListener();
		_listener.Prefixes.Add(text);
		_listener.Start();
		_pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		Task.Run((Func<Task?>)AcceptAsync);
		return text;
	}

	public async Task<string> AwaitCodeAsync(TimeSpan timeout)
	{
		TaskCompletionSource<string> pending = _pending ?? throw new InvalidOperationException("Sign-in wasn't started.");
		Task task = await Task.WhenAny(pending.Task, Task.Delay(timeout));
		Stop();
		if (task != pending.Task)
		{
			throw new TimeoutException("Sign-in timed out. Try again.");
		}
		return await pending.Task;
	}

	private async Task AcceptAsync()
	{
		HttpListener listener = _listener;
		if (listener == null)
		{
			return;
		}
		try
		{
			while (listener.IsListening)
			{
				HttpListenerContext httpListenerContext = await listener.GetContextAsync();
				string code = httpListenerContext.Request.QueryString["code"];
				string error = httpListenerContext.Request.QueryString["error_description"] ?? httpListenerContext.Request.QueryString["error"];
				await RespondAsync(httpListenerContext, code != null, error);
				if (code != null)
				{
					_pending?.TrySetResult(code);
					break;
				}
				if (error != null)
				{
					_pending?.TrySetException(new InvalidOperationException(error));
					break;
				}
			}
		}
		catch
		{
		}
	}

	private static async Task RespondAsync(HttpListenerContext ctx, bool ok, string? error)
	{
		string text = (ok ? "You're signed in. Close this tab and go back to STX Enterprises." : ("Sign-in failed: " + WebUtility.HtmlEncode(error ?? "unknown error")));
		string s = "<!doctype html><html><head><meta charset=\"utf-8\"><title>STX Enterprises</title></head>\n<body style=\"margin:0;height:100vh;display:grid;place-items:center;background:#000000;\n             color:#F2F3F5;font:15px/1.6 'Segoe UI',system-ui,sans-serif\">\n  <div style=\"text-align:center;max-width:32rem;padding:2rem\">\n    <div style=\"font-size:1.5rem;font-weight:650;letter-spacing:-.02em\">STX Enterprises</div>\n    <p style=\"color:#9BA1AC;margin-top:.75rem\">" + text + "</p>\n  </div>\n</body></html>";
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		ctx.Response.ContentType = "text/html; charset=utf-8";
		ctx.Response.ContentLength64 = bytes.Length;
		await ctx.Response.OutputStream.WriteAsync(bytes);
		ctx.Response.Close();
	}

	public void Stop()
	{
		try
		{
			_listener?.Stop();
			_listener?.Close();
		}
		catch
		{
		}
		_listener = null;
	}

	public void Dispose()
	{
		Stop();
	}

	private static int FreeLoopbackPort()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}
}

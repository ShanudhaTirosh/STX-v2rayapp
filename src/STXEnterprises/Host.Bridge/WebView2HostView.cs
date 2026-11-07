using System;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ServiceLib.Common;

namespace STXEnterprises.Host.Bridge;

public sealed class WebView2HostView : IHostView
{
	private readonly CoreWebView2 _webview;

	private readonly Dispatcher _dispatcher;

	public event Action<string>? MessageReceived;

	public WebView2HostView(CoreWebView2 webview)
	{
		_webview = webview;
		_dispatcher = Dispatcher.CurrentDispatcher;
		_webview.WebMessageReceived += (object? _, CoreWebView2WebMessageReceivedEventArgs e) =>
		{
			// Only our own bundled pages may drive the host. Anything else that ends up in the window is ignored.
			if (!OriginPolicy.IsTrusted(e.Source))
			{
				Logging.SaveLog("bridge: ignored a message from an untrusted origin");
				return;
			}
			MessageReceived?.Invoke(e.WebMessageAsJson);
		};
	}

	public void PostJson(string json)
	{
		if (_dispatcher.CheckAccess())
		{
			Send(json);
			return;
		}
		_dispatcher.BeginInvoke((Action)(() =>
		{
			Send(json);
		}));
	}

	private void Send(string json)
	{
		try
		{
			_webview.PostWebMessageAsJson(json);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("bridge post: " + ex.Message);
		}
	}
}

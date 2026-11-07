using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Helper;
using ServiceLib.Manager;
using ServiceLib.Models.Dto;
using ServiceLib.Resx;

namespace ServiceLib.Services;

public class DownloadService
{
	private static readonly string _tag = "DownloadService";

	public event EventHandler<UpdateResult>? UpdateCompleted;

	public event ErrorEventHandler? Error;

	public async Task<int> DownloadDataAsync(string url, IWebProxy webProxy, int downloadTimeout, Func<bool, string, Task> updateFunc)
	{
		try
		{
			Progress<string> progress = new Progress<string>();
			progress.ProgressChanged += (object? sender, string value) =>
			{
				updateFunc?.Invoke(arg1: false, value ?? "");
			};
			await DownloaderHelper.Instance.DownloadDataAsync4Speed(webProxy, url, progress, downloadTimeout);
		}
		catch (Exception ex)
		{
			await (updateFunc?.Invoke(arg1: false, ex.Message));
			if (ex.InnerException != null)
			{
				await (updateFunc?.Invoke(arg1: false, ex.InnerException.Message));
			}
		}
		return 0;
	}

	public async Task DownloadFileAsync(string url, string fileName, bool blProxy, int downloadTimeout)
	{
		_ = 1;
		try
		{
			UpdateCompleted?.Invoke(this, new UpdateResult(success: false, ResUI.Downloading + "   " + url));
			Progress<double> progress = new Progress<double>();
			progress.ProgressChanged += (object? sender, double value) =>
			{
				UpdateCompleted?.Invoke(this, new UpdateResult(value > 100.0, $"...{value}%"));
			};
			WebProxy webProxy = await GetWebProxy(blProxy);
			await DownloaderHelper.Instance.DownloadFileAsync(webProxy, url, fileName, progress, downloadTimeout);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			Error?.Invoke(this, new ErrorEventArgs(ex));
			if (ex.InnerException != null)
			{
				Error?.Invoke(this, new ErrorEventArgs(ex.InnerException));
			}
		}
	}

	public async Task<string?> UrlRedirectAsync(string url, bool blProxy)
	{
		SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler
		{
			AllowAutoRedirect = false
		};
		SocketsHttpHandler socketsHttpHandler2 = socketsHttpHandler;
		socketsHttpHandler2.Proxy = await GetWebProxy(blProxy);
		HttpResponseMessage httpResponseMessage = await new HttpClient(socketsHttpHandler).GetAsync(url);
		if (httpResponseMessage.StatusCode == HttpStatusCode.Found && (object)httpResponseMessage.Headers.Location != null)
		{
			return httpResponseMessage.Headers.Location.ToString();
		}
		Error?.Invoke(this, new ErrorEventArgs(new Exception("StatusCode error: " + httpResponseMessage.StatusCode)));
		Logging.SaveLog("StatusCode error: " + url);
		return null;
	}

	public async Task<string?> TryDownloadString(string url, bool blProxy, string userAgent)
	{
		return await TryDownloadString(url, await GetWebProxy(blProxy), userAgent);
	}

	public async Task<string?> TryDownloadString(string url, IWebProxy? webProxy, string userAgent)
	{
		try
		{
			string text = await DownloadStringAsync(url, webProxy, userAgent, 15);
			if (text.IsNotEmpty())
			{
				return text;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			Error?.Invoke(this, new ErrorEventArgs(ex));
			if (ex.InnerException != null)
			{
				Error?.Invoke(this, new ErrorEventArgs(ex.InnerException));
			}
		}
		try
		{
			string text2 = await DownloadStringViaDownloader(url, webProxy, userAgent, 15);
			if (text2.IsNotEmpty())
			{
				return text2;
			}
		}
		catch (Exception ex2)
		{
			Logging.SaveLog(_tag, ex2);
			Error?.Invoke(this, new ErrorEventArgs(ex2));
			if (ex2.InnerException != null)
			{
				Error?.Invoke(this, new ErrorEventArgs(ex2.InnerException));
			}
		}
		return null;
	}

	private async Task<string?> DownloadStringAsync(string url, IWebProxy? webProxy, string userAgent, int timeout)
	{
		try
		{
			HttpClient httpClient = new HttpClient(new SocketsHttpHandler
			{
				Proxy = webProxy,
				UseProxy = (webProxy != null)
			});
			if (userAgent.IsNullOrEmpty())
			{
				userAgent = Utils.GetVersion(blFull: false);
			}
			httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);
			Uri uri = new Uri(url);
			if (uri.UserInfo.IsNotEmpty())
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Utils.Base64Encode(uri.UserInfo));
			}
			using CancellationTokenSource cts = new CancellationTokenSource();
			return await httpClient.GetStringAsync(url, cts.Token).WaitAsync(TimeSpan.FromSeconds(timeout), cts.Token);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			Error?.Invoke(this, new ErrorEventArgs(ex));
			if (ex.InnerException != null)
			{
				Error?.Invoke(this, new ErrorEventArgs(ex.InnerException));
			}
		}
		return null;
	}

	private async Task<string?> DownloadStringViaDownloader(string url, IWebProxy? webProxy, string userAgent, int timeout)
	{
		try
		{
			if (userAgent.IsNullOrEmpty())
			{
				userAgent = Utils.GetVersion(blFull: false);
			}
			return await DownloaderHelper.Instance.DownloadStringAsync(webProxy, url, userAgent, timeout);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			Error?.Invoke(this, new ErrorEventArgs(ex));
			if (ex.InnerException != null)
			{
				Error?.Invoke(this, new ErrorEventArgs(ex.InnerException));
			}
		}
		return null;
	}

	private async Task<WebProxy?> GetWebProxy(bool blProxy)
	{
		if (!blProxy)
		{
			return null;
		}
		int port = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
		if (!(await SocketCheck("127.0.0.1", port)))
		{
			return null;
		}
		return new WebProxy($"socks5://{"127.0.0.1"}:{port}");
	}

	private async Task<bool> SocketCheck(string ip, int port)
	{
		try
		{
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
			using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			await sock.ConnectAsync(remoteEP);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}

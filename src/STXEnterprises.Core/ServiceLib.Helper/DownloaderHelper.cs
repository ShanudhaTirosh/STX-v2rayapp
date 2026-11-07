using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Downloader;
using ServiceLib.Common;

namespace ServiceLib.Helper;

public class DownloaderHelper
{
	private static readonly Lazy<DownloaderHelper> _instance = new Lazy<DownloaderHelper>(() => new DownloaderHelper());

	public static DownloaderHelper Instance => _instance.Value;

	public async Task<string?> DownloadStringAsync(IWebProxy? webProxy, string url, string? userAgent, int timeout)
	{
		if (url.IsNullOrEmpty())
		{
			return null;
		}
		Uri uri = new Uri(url);
		WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
		if (uri.UserInfo.IsNotEmpty())
		{
			webHeaderCollection.Add(HttpRequestHeader.Authorization, "Basic " + Utils.Base64Encode(uri.UserInfo));
		}
		DownloadConfiguration options = checked(new DownloadConfiguration
		{
			BlockTimeout = timeout * 1000,
			MaxTryAgainOnFailure = 2,
			RequestConfiguration = 
			{
				Headers = webHeaderCollection,
				UserAgent = userAgent,
				ConnectTimeout = timeout * 1000,
				Proxy = webProxy
			}
		});
		string result;
		await using (DownloadService downloader = new DownloadService(options))
		{
			downloader.DownloadFileCompleted += (object? sender, AsyncCompletedEventArgs value) =>
			{
				if (value.Error != null)
				{
					throw value.Error;
				}
			};
			using CancellationTokenSource cts = new CancellationTokenSource();
			string text;
			await using (Stream stream = await downloader.DownloadFileTaskAsync(url, cts.Token).WaitAsync(TimeSpan.FromSeconds(timeout), cts.Token))
			{
				using StreamReader reader = new StreamReader(stream);
				text = await reader.ReadToEndAsync(cts.Token);
			}
			result = text;
		}
		return result;
	}

	public async Task DownloadDataAsync4Speed(IWebProxy webProxy, string url, IProgress<string> progress, int timeout)
	{
		if (url.IsNullOrEmpty())
		{
			throw new ArgumentNullException("url");
		}
		checked
		{
			DownloadConfiguration options = new DownloadConfiguration
			{
				BlockTimeout = timeout * 1000,
				MaxTryAgainOnFailure = 2,
				RequestConfiguration = 
				{
					ConnectTimeout = timeout * 1000,
					Proxy = webProxy
				}
			};
			DateTime lastUpdateTime = DateTime.Now;
			bool hasValue = false;
			double maxSpeed = 0.0;
			await using DownloadService downloader = new DownloadService(options);
			downloader.DownloadProgressChanged += (object? sender, Downloader.DownloadProgressChangedEventArgs value) =>
			{
				if (progress != null && value.BytesPerSecondSpeed > 0.0)
				{
					hasValue = true;
					if (value.BytesPerSecondSpeed > maxSpeed)
					{
						maxSpeed = value.BytesPerSecondSpeed;
					}
					if ((DateTime.Now - lastUpdateTime).TotalMilliseconds >= 1000.0)
					{
						lastUpdateTime = DateTime.Now;
						string value2 = (maxSpeed / 1000.0 / 1000.0).ToString("#0.0");
						progress.Report(value2);
					}
				}
			};
			downloader.DownloadFileCompleted += (object? sender, AsyncCompletedEventArgs value) =>
			{
				if (progress != null)
				{
					if (hasValue && maxSpeed > 0.0)
					{
						string value2 = (maxSpeed / 1000.0 / 1000.0).ToString("#0.0");
						progress.Report(value2);
					}
					else if (value.Error != null)
					{
						progress.Report(value.Error?.Message);
					}
					else
					{
						progress.Report("0");
					}
				}
			};
			using CancellationTokenSource cts = new CancellationTokenSource();
			cts.CancelAfter(timeout * 1000);
			await using (await downloader.DownloadFileTaskAsync(url, cts.Token))
			{
			}
		}
	}

	public async Task DownloadFileAsync(IWebProxy? webProxy, string url, string fileName, IProgress<double> progress, int timeout)
	{
		if (url.IsNullOrEmpty())
		{
			throw new ArgumentNullException("url");
		}
		if (fileName.IsNullOrEmpty())
		{
			throw new ArgumentNullException("fileName");
		}
		if (File.Exists(fileName))
		{
			File.Delete(fileName);
		}
		DownloadConfiguration options = checked(new DownloadConfiguration
		{
			BlockTimeout = timeout * 1000,
			MaxTryAgainOnFailure = 2,
			RequestConfiguration = 
			{
				ConnectTimeout = timeout * 1000,
				Proxy = webProxy
			}
		});
		int progressPercentage = 0;
		bool hasValue = false;
		await using DownloadService downloader = new DownloadService(options);
		downloader.DownloadStarted += (object? sender, DownloadStartedEventArgs value) =>
		{
			progress?.Report(0.0);
		};
		downloader.DownloadProgressChanged += (object? sender, Downloader.DownloadProgressChangedEventArgs value) =>
		{
			hasValue = true;
			int num = checked((int)value.ProgressPercentage);
			if (progressPercentage != num && num % 10 == 0)
			{
				progressPercentage = num;
				progress.Report(num);
			}
		};
		downloader.DownloadFileCompleted += (object? sender, AsyncCompletedEventArgs value) =>
		{
			if (progress != null)
			{
				if (hasValue && value.Error == null)
				{
					progress.Report(101.0);
				}
				else if (value.Error != null)
				{
					throw value.Error;
				}
			}
		};
		using CancellationTokenSource cts = new CancellationTokenSource();
		await downloader.DownloadFileTaskAsync(url, fileName, cts.Token);
	}
}

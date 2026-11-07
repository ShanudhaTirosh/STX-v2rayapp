using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace STXEnterprises.Host.Vpn;

public static class SubscriptionUsage
{
	private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = (HttpRequestMessage _, X509Certificate2? _, X509Chain? _, SslPolicyErrors _) => true
	})
	{
		Timeout = TimeSpan.FromSeconds(15L)
	};

	public static async Task<UsageHeader?> ReadAsync(string url)
	{
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.TryAddWithoutValidation("User-Agent", "STXClient");
			using HttpResponseMessage httpResponseMessage = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			if (!httpResponseMessage.Headers.TryGetValues("subscription-userinfo", out IEnumerable<string> values))
			{
				return null;
			}
			string text = values.FirstOrDefault();
			if (text.IsNullOrEmpty())
			{
				return null;
			}
			string[] array = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, long> dictionary = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string[] array3 = array2[i].Split('=', 2);
				if (array3.Length == 2 && long.TryParse(array3[1].Trim(), out var result))
				{
					dictionary[array3[0].Trim()] = result;
				}
			}
			return new UsageHeader(dictionary.GetValueOrDefault("upload"), dictionary.GetValueOrDefault("download"), dictionary.GetValueOrDefault("total"), dictionary.GetValueOrDefault("expire"));
		}
		catch (Exception ex)
		{
			Logging.SaveLog("usage header " + url.Substring(0, Math.Min(40, url.Length)) + ": " + ex.Message);
			return null;
		}
	}
}

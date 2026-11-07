using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace ServiceLib.Helper;

public class HttpClientHelper
{
	private static readonly Lazy<HttpClientHelper> _instance = new Lazy<HttpClientHelper>(() => new HttpClientHelper(new HttpClient(new SocketsHttpHandler
	{
		UseCookies = false,
		UseProxy = false
	})));

	private readonly HttpClient httpClient;

	public static HttpClientHelper Instance => _instance.Value;

	private HttpClientHelper(HttpClient httpClient)
	{
		this.httpClient = httpClient;
	}

	public async Task<string?> TryGetAsync(string url)
	{
		if (url.IsNullOrEmpty())
		{
			return null;
		}
		try
		{
			return await (await httpClient.GetAsync(url)).Content.ReadAsStringAsync();
		}
		catch
		{
			return null;
		}
	}

	public async Task<string?> GetAsync(string url)
	{
		if (url.IsNullOrEmpty())
		{
			return null;
		}
		return await httpClient.GetStringAsync(url);
	}

	public async Task PutAsync(string url, Dictionary<string, string> headers)
	{
		StringContent content = new StringContent(JsonUtils.Serialize(headers), Encoding.UTF8, "application/json");
		await httpClient.PutAsync(url, content);
	}

	public async Task PatchAsync(string url, Dictionary<string, string> headers)
	{
		string s = JsonUtils.Serialize(headers);
		ByteArrayContent byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(s));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		await httpClient.PatchAsync(url, byteArrayContent);
	}

	public async Task DeleteAsync(string url)
	{
		await httpClient.DeleteAsync(url);
	}
}

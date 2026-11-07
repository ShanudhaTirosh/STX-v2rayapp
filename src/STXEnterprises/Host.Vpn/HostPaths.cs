using System;
using System.IO;

namespace STXEnterprises.Host.Vpn;

public static class HostPaths
{
	/// <summary>WebView2 profile/cache folder. One definition, used by both the window and Clear cache.</summary>
	public static string WebViewUserData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STX Enterprises", "WebView2");
}

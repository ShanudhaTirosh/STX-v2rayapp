using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace STXEnterprises.Host.Vpn;

public static class CoreAssets
{
	public sealed record CoreCheck(bool Ok, string Version, string? Problem);

	public const int MaxSupportedMajor = 26;

	public const int MaxSupportedMinor = 7;

	public const string PinnedVersion = "26.3.27";

	private static CoreCheck? _cached;

	public static string BinRoot => Path.Combine(AppContext.BaseDirectory, "bin");

	public static string XrayPath => Path.Combine(BinRoot, "xray", Exe("xray"));

	public static string SingBoxPath => Path.Combine(BinRoot, "sing_box", Exe("sing-box"));

	public static bool HasXray => File.Exists(XrayPath);

	public static bool HasSingBox => File.Exists(SingBoxPath);

	private static string Exe(string name)
	{
		if (!OperatingSystem.IsWindows())
		{
			return name;
		}
		return name + ".exe";
	}

	public static async Task<CoreCheck> VerifyXrayAsync()
	{
		if ((object)_cached != null)
		{
			return _cached;
		}
		if (!HasXray)
		{
			return _cached = new CoreCheck(Ok: false, "", "The VPN core is missing. Reinstall STX Enterprises.");
		}
		string input;
		try
		{
			using Process proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = XrayPath,
					Arguments = "-version",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = (Path.GetDirectoryName(XrayPath) ?? BinRoot)
				}
			};
			proc.Start();
			Task<string> stdout = proc.StandardOutput.ReadToEndAsync();
			Task<string> stderr = proc.StandardError.ReadToEndAsync();
			await proc.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10L));
			input = await stdout + await stderr;
		}
		catch (Exception ex)
		{
			return _cached = new CoreCheck(Ok: false, "", "The VPN core would not start (" + ex.Message + "). Reinstall STX Enterprises.");
		}
		Match match = Regex.Match(input, "Xray\\s+(\\d+)\\.(\\d+)\\.(\\d+)");
		if (!match.Success)
		{
			return _cached = new CoreCheck(Ok: false, "", "Couldn't read the VPN core version. Reinstall STX Enterprises.");
		}
		string text = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
		int num = int.Parse(match.Groups[1].Value);
		int num2 = int.Parse(match.Groups[2].Value);
		if (num > 26 || (num == 26 && num2 > 7))
		{
			return _cached = new CoreCheck(Ok: false, text, $"This copy has VPN core {text}, which no longer supports the certificate option every STX config relies on. Reinstall STX Enterprises to restore core {"26.3.27"}.");
		}
		return _cached = new CoreCheck(Ok: true, text, null);
	}
}

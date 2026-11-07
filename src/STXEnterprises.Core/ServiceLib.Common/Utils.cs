using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;

namespace ServiceLib.Common;

public class Utils
{
	private static readonly string _tag = "Utils";

	public static string List2String(List<string>? lst, bool wrap = false)
	{
		if (lst == null || lst.Count == 0)
		{
			return string.Empty;
		}
		string separator = (wrap ? ("," + Environment.NewLine) : ",");
		try
		{
			return string.Join(separator, lst);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return string.Empty;
		}
	}

	public static List<string>? String2List(string? str)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return null;
		}
		try
		{
			str = str.Replace(Environment.NewLine, string.Empty);
			return new List<string>(str.Split(',', StringSplitOptions.RemoveEmptyEntries));
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return null;
		}
	}

	public static List<string>? String2ListSorted(string str)
	{
		List<string>? list = String2List(str);
		if (list != null)
		{
			list.Sort();
			return list;
		}
		return list;
	}

	public static string Base64Encode(string plainText, bool removePadding = false)
	{
		try
		{
			string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
			if (removePadding)
			{
				text = text.TrimEnd('=');
			}
			return text;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return string.Empty;
	}

	public static string Base64Decode(string? plainText)
	{
		try
		{
			if (plainText.IsNullOrEmpty())
			{
				return string.Empty;
			}
			plainText = plainText.Trim().Replace(Environment.NewLine, "").Replace("\n", "")
				.Replace("\r", "")
				.Replace('_', '/')
				.Replace('-', '+')
				.Replace(" ", "");
			if (plainText.Length % 4 > 0)
			{
				checked
				{
					plainText = plainText.PadRight(plainText.Length + 4 - unchecked(plainText.Length % 4), '=');
				}
			}
			byte[] bytes = Convert.FromBase64String(plainText);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return string.Empty;
	}

	public static bool ToBool(object obj)
	{
		try
		{
			return Convert.ToBoolean(obj);
		}
		catch
		{
			return false;
		}
	}

	public static string ToString(object? obj)
	{
		try
		{
			return obj?.ToString() ?? string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	public static string HumanFy(long amount)
	{
		checked
		{
			if (amount > 0)
			{
				string[] array = new string[5] { "KB", "MB", "GB", "TB", "PB" };
				int num = 0;
				double num2 = amount;
				while (num2 >= 1024.0 && num < array.Length - 1)
				{
					num2 /= 1024.0;
					num++;
				}
				return $"{num2:f1} {array[num]}";
			}
			return $"{amount:f1} B";
		}
	}

	public static string UrlEncode(string url)
	{
		return Uri.EscapeDataString(url);
	}

	public static string UrlDecode(string url)
	{
		return Uri.UnescapeDataString(url);
	}

	public static NameValueCollection ParseQueryString(string query)
	{
		NameValueCollection nameValueCollection = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
		if (query.IsNullOrEmpty())
		{
			return nameValueCollection;
		}
		string[] array = query.Substring(1).Split('&', StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('=');
			if (array2.Length == 2)
			{
				string name = Uri.UnescapeDataString(array2.First());
				string value = Uri.UnescapeDataString(array2.Last());
				if (nameValueCollection[name] == null)
				{
					nameValueCollection.Add(name, value);
				}
			}
		}
		return nameValueCollection;
	}

	public static string GetMd5(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return string.Empty;
		}
		try
		{
			byte[] array = MD5.HashData(Encoding.UTF8.GetBytes(str));
			StringBuilder stringBuilder = new StringBuilder(32);
			byte[] array2 = array;
			foreach (byte b in array2)
			{
				stringBuilder.Append(b.ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return string.Empty;
		}
	}

	public static string GetFileHash(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return string.Empty;
		}
		if (!File.Exists(filePath))
		{
			return string.Empty;
		}
		try
		{
			using MD5 mD = MD5.Create();
			using FileStream inputStream = File.OpenRead(filePath);
			return BitConverter.ToString(mD.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return string.Empty;
		}
	}

	public static string GetPunycode(string url)
	{
		if (url.IsNullOrEmpty())
		{
			return url;
		}
		try
		{
			Uri uri = new Uri(url);
			if (uri.Host == uri.IdnHost || uri.Host == "[" + uri.IdnHost + "]")
			{
				return url;
			}
			return url.Replace(uri.Host, uri.IdnHost);
		}
		catch
		{
			return url;
		}
	}

	public static bool IsBase64String(string? plainText)
	{
		if (plainText.IsNullOrEmpty())
		{
			return false;
		}
		Span<byte> bytes = new Span<byte>(new byte[plainText.Length]);
		int bytesWritten;
		return Convert.TryFromBase64String(plainText, bytes, out bytesWritten);
	}

	public static string Convert2Comma(string text)
	{
		if (text.IsNullOrEmpty())
		{
			return text;
		}
		return text.Replace("，", ",").Replace(Environment.NewLine, ",");
	}

	public static List<string> GetEnumNames<TEnum>() where TEnum : Enum
	{
		return (from TEnum e in Enum.GetValues(typeof(TEnum))
			select e.ToString()).ToList();
	}

	public static Dictionary<string, List<string>> ParseHostsToDictionary(string? hostsContent)
	{
		if (hostsContent.IsNullOrEmpty())
		{
			return new Dictionary<string, List<string>>();
		}
		return (from line in hostsContent.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			select line.Trim() into line
			where !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#')
			where !string.IsNullOrWhiteSpace(line) && line.Contains(' ')
			select line.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries) into parts
			where parts.Length >= 2
			group parts by parts[0]).ToDictionary((IGrouping<string, string[]> group) => group.Key, (IGrouping<string, string[]> group) => group.SelectMany((string[] parts) => parts.Skip(1)).ToList());
	}

	public static (string domain, string scheme, int port, string path) ParseUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return (domain: "", scheme: "", port: 0, path: "");
		}
		if (Uri.TryCreate(url, UriKind.Absolute, out Uri result) && !string.IsNullOrEmpty(result.Host))
		{
			string scheme = result.Scheme;
			string host = result.Host;
			int item = ((!result.IsDefaultPort) ? result.Port : 0);
			string pathAndQuery = result.PathAndQuery;
			return (domain: host, scheme: scheme, port: item, path: pathAndQuery);
		}
		Match match = Regex.Match(url, "^(?:([a-zA-Z][a-zA-Z0-9+.-]*):/{2,})?([^/?#]+)([^?#]*)?.*$");
		if (match.Success)
		{
			string value = match.Groups[1].Value;
			string text = match.Groups[2].Value;
			string value2 = match.Groups[3].Value;
			int num = text.LastIndexOf('@');
			if (num > 0)
			{
				text = text.Substring(checked(num + 1));
			}
			var (text2, item2) = ParseAuthority(text);
			if (!string.IsNullOrEmpty(text2))
			{
				return (domain: text2, scheme: value, port: item2, path: value2);
			}
		}
		return (domain: url, scheme: "", port: 0, path: "");
	}

	private static (string domain, int port) ParseAuthority(string authority)
	{
		if (string.IsNullOrEmpty(authority))
		{
			return (domain: "", port: 0);
		}
		int item = 0;
		string item2 = authority;
		checked
		{
			if (authority.StartsWith('[') && authority.Contains(']'))
			{
				int num = authority.LastIndexOf(']');
				if (num < authority.Length - 1 && authority[num + 1] == ':')
				{
					if (int.TryParse(authority.Substring(num + 2), out var result))
					{
						item = result;
					}
					item2 = authority.Substring(0, num + 1);
				}
				else
				{
					item2 = authority;
				}
			}
			else
			{
				int num2 = authority.LastIndexOf(':');
				if (num2 > 0 && num2 < authority.Length - 1 && authority.Substring(num2 + 1).All(char.IsDigit) && int.TryParse(authority.Substring(num2 + 1), out var result2))
				{
					item = result2;
					item2 = authority.Substring(0, num2);
				}
			}
			return (domain: item2, port: item);
		}
	}

	public static string? DomainStrategy4Sbox(string? strategy)
	{
		if (strategy != null)
		{
			if (strategy.StartsWith("UseIPv4"))
			{
				return "prefer_ipv4";
			}
			if (strategy.StartsWith("UseIPv6"))
			{
				return "prefer_ipv6";
			}
			if (strategy.StartsWith("ForceIPv4"))
			{
				return "ipv4_only";
			}
			if (strategy.StartsWith("ForceIPv6"))
			{
				return "ipv6_only";
			}
		}
		return null;
	}

	public static bool IsNumeric(string oText)
	{
		return oText.All(char.IsNumber);
	}

	public static bool IsDomain(string? domain)
	{
		if (domain.IsNullOrEmpty())
		{
			return false;
		}
		string extension = Path.GetExtension(domain);
		bool flag = extension.IsNotEmpty();
		bool flag2;
		if (flag)
		{
			string text = extension.Substring(1).ToLowerInvariant();
			if (text != null)
			{
				int length = text.Length;
				if (length != 3)
				{
					if (length == 4)
					{
						char c = text[0];
						if (c != 'j')
						{
							if (c != 't')
							{
								if (c == 'y' && text == "yaml")
								{
									goto IL_012b;
								}
							}
							else if (text == "toml")
							{
								goto IL_012b;
							}
						}
						else if (text == "json")
						{
							goto IL_012b;
						}
					}
				}
				else
				{
					char c = text[0];
					if ((uint)c <= 108u)
					{
						if (c != 'c')
						{
							if (c != 'i')
							{
								if (c == 'l' && text == "log")
								{
									goto IL_012b;
								}
							}
							else if (text == "ini")
							{
								goto IL_012b;
							}
						}
						else if (text == "cfg")
						{
							goto IL_012b;
						}
					}
					else if (c != 't')
					{
						if (c != 'x')
						{
							if (c == 'y' && text == "yml")
							{
								goto IL_012b;
							}
						}
						else if (text == "xml")
						{
							goto IL_012b;
						}
					}
					else if (text == "txt")
					{
						goto IL_012b;
					}
				}
			}
			flag2 = false;
			goto IL_0131;
		}
		goto IL_0133;
		IL_0131:
		flag = flag2;
		goto IL_0133;
		IL_012b:
		flag2 = true;
		goto IL_0131;
		IL_0133:
		if (flag)
		{
			return false;
		}
		return Uri.CheckHostName(domain) == UriHostNameType.Dns;
	}

	public static bool IsIpv6(string ip)
	{
		if (IPAddress.TryParse(ip, out IPAddress address))
		{
			return address.AddressFamily switch
			{
				AddressFamily.InterNetwork => false, 
				AddressFamily.InterNetworkV6 => true, 
				_ => false, 
			};
		}
		return false;
	}

	public static bool IsIpv4(string? ip)
	{
		if (ip.IsNullOrEmpty())
		{
			return false;
		}
		ip = ip.Trim();
		if (!IPAddress.TryParse(ip, out IPAddress address))
		{
			return false;
		}
		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			return ip.Count((char c) => c == '.') == 3;
		}
		return false;
	}

	public static bool IsIpAddress(string? ip)
	{
		if (ip.IsNullOrEmpty())
		{
			return false;
		}
		ip = ip.Trim();
		if (!IPAddress.TryParse(ip, out IPAddress address))
		{
			return false;
		}
		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			return ip.Count((char c) => c == '.') == 3;
		}
		return address.AddressFamily == AddressFamily.InterNetworkV6;
	}

	public static Uri? TryUri(string url)
	{
		try
		{
			return new Uri(url);
		}
		catch (UriFormatException)
		{
			return null;
		}
	}

	public static bool IsPrivateNetwork(string ip)
	{
		if (IPAddress.TryParse(ip, out IPAddress address))
		{
			if (IPAddress.IsLoopback(address))
			{
				return true;
			}
			byte[] addressBytes = address.GetAddressBytes();
			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				if (addressBytes[0] == 10)
				{
					return true;
				}
				if (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31)
				{
					return true;
				}
				if (addressBytes[0] == 192 && addressBytes[1] == 168)
				{
					return true;
				}
			}
			else if (address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				if (addressBytes[0] == 254 && (addressBytes[1] & 0xC0) == 128)
				{
					return true;
				}
				if ((addressBytes[0] & 0xFE) == 252)
				{
					return true;
				}
				if (address.IsIPv4MappedToIPv6)
				{
					byte[] array = addressBytes.Skip(12).ToArray();
					if (array[0] == 10)
					{
						return true;
					}
					if (array[0] == 172 && array[1] >= 16 && array[1] <= 31)
					{
						return true;
					}
					if (array[0] == 192 && array[1] == 168)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool PortInUse(int port)
	{
		try
		{
			var (list, list2) = GetActiveNetworkInfo();
			if (list != null && list.FindIndex((IPEndPoint it) => it.Port == port) >= 0)
			{
				return true;
			}
			if (list2 != null && list2.FindIndex((TcpConnectionInformation it) => it.LocalEndPoint.Port == port) >= 0)
			{
				return true;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return false;
	}

	public static int GetFreePort(int defaultPort = 0)
	{
		try
		{
			if (defaultPort != 0 && !PortInUse(defaultPort))
			{
				return defaultPort;
			}
			TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
			tcpListener.Start();
			int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
			tcpListener.Stop();
			return port;
		}
		catch
		{
		}
		return 59090;
	}

	public static (List<IPEndPoint> endpoints, List<TcpConnectionInformation> connections) GetActiveNetworkInfo()
	{
		List<IPEndPoint> list = new List<IPEndPoint>();
		List<TcpConnectionInformation> list2 = new List<TcpConnectionInformation>();
		try
		{
			IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			list.AddRange(iPGlobalProperties.GetActiveTcpListeners());
			list.AddRange(iPGlobalProperties.GetActiveUdpListeners());
			list2.AddRange(iPGlobalProperties.GetActiveTcpConnections());
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return (endpoints: list, connections: list2);
	}

	public static bool UpgradeAppExists(out string upgradeFileName)
	{
		upgradeFileName = Path.Combine(GetBaseDirectory(), GetExeName("AmazTool"));
		return File.Exists(upgradeFileName);
	}

	public static string GetVersion(bool blFull = true)
	{
		try
		{
			return blFull ? $"{"STX Enterprises"} - V{GetVersionInfo()} - {RuntimeInformation.ProcessArchitecture}" : ("STX Enterprises/" + GetVersionInfo());
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return "STX Enterprises";
	}

	public static string GetVersionInfo()
	{
		try
		{
			return (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())?.GetName()?.Version?.ToString(3) ?? "0.0";
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return "0.0";
		}
	}

	public static string GetRuntimeInfo()
	{
		return $"{GetVersion()} | {StartupPath()} | {GetExePath()} | {Environment.OSVersion}";
	}

	public static string GetGuid(bool full = true)
	{
		try
		{
			if (full)
			{
				return Guid.NewGuid().ToString("D");
			}
			return BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return string.Empty;
	}

	public static bool IsGuidByParse(string strSrc)
	{
		Guid result;
		return Guid.TryParse(strSrc, out result);
	}

	private static Dictionary<string, string> GetSystemHosts(string hostFile)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		try
		{
			if (!File.Exists(hostFile))
			{
				return dictionary;
			}
			string[] array = File.ReadAllText(hostFile).Replace("\r", "").Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.IsNullOrEmpty() || text.StartsWith("#"))
				{
					continue;
				}
				int num = text.IndexOf('#');
				if (num >= 0)
				{
					text = text.Substring(0, num).Trim();
				}
				if (text.IsNullOrEmpty())
				{
					continue;
				}
				string[] array2 = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (array2.Length >= 2)
				{
					string text2 = array2[0];
					string text3 = array2[1];
					if (IsIpAddress(text2) && !text3.IsNullOrEmpty() && text3.Length <= 255)
					{
						dictionary[text3] = text2;
					}
				}
			}
			return dictionary;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return dictionary;
		}
	}

	public static Dictionary<string, string> GetSystemHosts()
	{
		if (IsWindows())
		{
			Dictionary<string, string> systemHosts = GetSystemHosts("C:\\Windows\\System32\\drivers\\etc\\hosts");
			{
				foreach (var (key, value) in GetSystemHosts("C:\\Windows\\System32\\drivers\\etc\\hosts.ics"))
				{
					systemHosts[key] = value;
				}
				return systemHosts;
			}
		}
		if (IsLinux() || IsMacOS())
		{
			return GetSystemHosts("/etc/hosts");
		}
		return new Dictionary<string, string>();
	}

	public static async Task<string?> GetCliWrapOutput(string filePath, string? arg)
	{
		return await GetCliWrapOutput(filePath, (arg != null) ? new List<string> { arg } : null);
	}

	public static async Task<string?> GetCliWrapOutput(string filePath, IEnumerable<string>? args)
	{
		try
		{
			Command command = Cli.Wrap(filePath);
			if (args != null)
			{
				command = ((args.Count() != 1) ? command.WithArguments(args) : command.WithArguments(args.First()));
			}
			BufferedCommandResult bufferedCommandResult = await command.ExecuteBufferedAsync();
			if (bufferedCommandResult.IsSuccess)
			{
				return bufferedCommandResult.StandardOutput ?? "";
			}
			Logging.SaveLog(bufferedCommandResult.ToString() ?? "");
		}
		catch (Exception ex)
		{
			Logging.SaveLog("GetCliWrapOutput", ex);
		}
		return null;
	}

	public static bool HasWritePermission()
	{
		try
		{
			string baseDirectory = GetBaseDirectory();
			if (File.Exists(Path.Combine(baseDirectory, "NotStoreConfigHere.txt")))
			{
				return false;
			}
			if (IsWindows() && baseDirectory.Contains("Users") && baseDirectory.Contains("WinGet"))
			{
				return false;
			}
			string text = Path.Combine(baseDirectory, "guiTemps");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = Path.Combine(text, GetGuid());
			File.Create(path).Close();
			File.Delete(path);
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static string GetPath(string fileName)
	{
		string text = StartupPath();
		if (fileName.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, fileName);
	}

	public static string GetBaseDirectory(string fileName = "")
	{
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
	}

	public static string GetExePath()
	{
		return Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
	}

	public static string StartupPath()
	{
		if (Environment.GetEnvironmentVariable("STXENTERPRISES_LOCAL_APPLICATION_DATA") == "1" || Environment.GetEnvironmentVariable("PXNSHIELD_LOCAL_APPLICATION_DATA") == "1")
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STX Enterprises");
		}
		return GetBaseDirectory();
	}

	public static string GetTempPath(string filename = "")
	{
		string text = Path.Combine(StartupPath(), "guiTemps");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	public static string GetBackupPath(string filename)
	{
		string text = Path.Combine(StartupPath(), "guiBackups");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return Path.Combine(text, filename);
	}

	public static string GetConfigPath(string filename = "")
	{
		string text = Path.Combine(StartupPath(), "guiConfigs");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	public static string GetBinPath(string filename, string? coreType = null)
	{
		string text = Path.Combine(StartupPath(), "bin");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (coreType != null)
		{
			text = Path.Combine(text, coreType.ToLower().ToString());
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	public static string GetLogPath(string filename = "")
	{
		string text = Path.Combine(StartupPath(), "guiLogs");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	public static string GetFontsPath(string filename = "")
	{
		string text = Path.Combine(StartupPath(), "guiFonts");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	public static string GetBinConfigPath(string filename = "")
	{
		string text = Path.Combine(StartupPath(), "binConfigs");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		if (filename.IsNullOrEmpty())
		{
			return text;
		}
		return Path.Combine(text, filename);
	}

	[SupportedOSPlatformGuard("windows")]
	public static bool IsWindows()
	{
		return OperatingSystem.IsWindows();
	}

	[SupportedOSPlatformGuard("linux")]
	public static bool IsLinux()
	{
		return OperatingSystem.IsLinux();
	}

	[SupportedOSPlatformGuard("macos")]
	public static bool IsMacOS()
	{
		return OperatingSystem.IsMacOS();
	}

	[UnsupportedOSPlatformGuard("windows")]
	public static bool IsNonWindows()
	{
		return !OperatingSystem.IsWindows();
	}

	public static string GetExeName(string name)
	{
		if (name.IsNullOrEmpty() || IsNonWindows())
		{
			return name;
		}
		if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			return name;
		}
		return name + ".exe";
	}

	public static bool IsAdministrator()
	{
		if (IsWindows())
		{
			return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}
		return false;
	}

	public static bool IsPackagedInstall()
	{
		try
		{
			if (IsWindows() || IsMacOS())
			{
				return false;
			}
			string exePath = GetExePath();
			string text = (string.IsNullOrEmpty(exePath) ? StartupPath() : (Path.GetDirectoryName(exePath) ?? "")).Replace('\\', '/');
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			if (text.StartsWith("/opt/STXEnterprises", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (text.StartsWith("/usr/lib/STXEnterprises", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (text.StartsWith("/usr/share/STXEnterprises", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static async Task<string?> GetLinuxUserId()
	{
		List<string> args = new List<string> { "-c", "id -u" };
		return await GetCliWrapOutput("/bin/bash", args);
	}

	public static async Task<string?> SetLinuxChmod(string? fileName)
	{
		if (fileName.IsNullOrEmpty())
		{
			return null;
		}
		if (SetUnixFileMode(fileName))
		{
			Logging.SaveLog("Successfully set the file execution permission, " + fileName);
			return string.Empty;
		}
		if (fileName.Contains(' '))
		{
			fileName = fileName.AppendQuotes();
		}
		List<string> args = new List<string>
		{
			"-c",
			"chmod +x " + fileName
		};
		return await GetCliWrapOutput("/bin/bash", args);
	}

	public static bool SetUnixFileMode(string? fileName)
	{
		if (IsWindows())
		{
			return false;
		}
		return SetUnixFileModeInternal(fileName);
	}

	[UnsupportedOSPlatform("windows")]
	private static bool SetUnixFileModeInternal(string? fileName)
	{
		try
		{
			if (fileName.IsNullOrEmpty())
			{
				return false;
			}
			if (File.Exists(fileName))
			{
				UnixFileMode unixFileMode = File.GetUnixFileMode(fileName);
				File.SetUnixFileMode(fileName, unixFileMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
				return true;
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("SetUnixFileMode", ex);
		}
		return false;
	}

	public static async Task<string?> GetLinuxFontFamily(string lang)
	{
		List<string> args = new List<string> { "-c", "fc-list : family" };
		return await GetCliWrapOutput("/bin/bash", args);
	}

	public static string? GetHomePath()
	{
		if (!IsWindows())
		{
			return Environment.GetEnvironmentVariable("HOME");
		}
		return Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
	}
}

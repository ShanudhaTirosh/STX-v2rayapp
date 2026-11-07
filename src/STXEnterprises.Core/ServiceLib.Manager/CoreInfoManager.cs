using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.CoreConfigs;
using ServiceLib.Resx;

namespace ServiceLib.Manager;

public sealed class CoreInfoManager
{
	private static readonly Lazy<CoreInfoManager> _instance = new Lazy<CoreInfoManager>(() => new CoreInfoManager());

	private List<CoreInfo>? _coreInfo;

	public static CoreInfoManager Instance => _instance.Value;

	public CoreInfoManager()
	{
		InitCoreInfo();
	}

	public CoreInfo? GetCoreInfo(ECoreType coreType)
	{
		if (_coreInfo == null)
		{
			InitCoreInfo();
		}
		return _coreInfo?.FirstOrDefault((CoreInfo t) => t.CoreType == coreType);
	}

	public List<CoreInfo> GetCoreInfo()
	{
		if (_coreInfo == null)
		{
			InitCoreInfo();
		}
		return _coreInfo ?? new List<CoreInfo>();
	}

	public string GetCoreExecFile(CoreInfo? coreInfo, out string msg)
	{
		string text = string.Empty;
		msg = string.Empty;
		foreach (string item in coreInfo?.CoreExes)
		{
			string binPath = Utils.GetBinPath(Utils.GetExeName(item), coreInfo.CoreType.ToString());
			if (File.Exists(binPath))
			{
				text = binPath;
				break;
			}
		}
		if (text.IsNullOrEmpty())
		{
			msg = string.Format(ResUI.NotFoundCore, Utils.GetBinPath("", coreInfo?.CoreType.ToString()), coreInfo?.CoreExes?.LastOrDefault(), coreInfo?.Url);
			Logging.SaveLog(msg);
		}
		return text;
	}

	public List<ECoreType> GetCheckUpdateCoreTypes()
	{
		List<ECoreType> list = new List<ECoreType>();
		if (RuntimeInformation.ProcessArchitecture != Architecture.X86)
		{
			if (IsCheckUpdateSupported(ECoreType.app))
			{
				list.Add(ECoreType.app);
			}
			if (!Utils.IsWindows() || Environment.OSVersion.Version.Major >= 10)
			{
				list.Add(ECoreType.Xray);
				list.Add(ECoreType.mihomo);
				list.Add(ECoreType.sing_box);
			}
		}
		return list;
	}

	public bool IsCheckUpdateSupported(ECoreType type)
	{
		return type switch
		{
			ECoreType.app => !Utils.IsPackagedInstall(), 
			ECoreType.Xray => true, 
			ECoreType.mihomo => true, 
			ECoreType.sing_box => true, 
			_ => false, 
		};
	}

	public bool GetCheckPreRelease(ECoreType type, bool preRelease)
	{
		return type switch
		{
			ECoreType.app => preRelease, 
			ECoreType.Xray => preRelease, 
			_ => false, 
		};
	}

	private void InitCoreInfo()
	{
		string coreUrl = GetCoreUrl(ECoreType.app);
		GetCoreUrl(ECoreType.Xray);
		string coreUrl2 = GetCoreUrl(ECoreType.mihomo);
		string coreUrl3 = GetCoreUrl(ECoreType.sing_box);
		int num = 15;
		List<CoreInfo> list = new List<CoreInfo>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<CoreInfo> span = CollectionsMarshal.AsSpan(list);
		span[0] = new CoreInfo
		{
			CoreType = ECoreType.app,
			Url = GetCoreUrl(ECoreType.app),
			ReleaseApiUrl = coreUrl.Replace("https://github.com", "https://api.github.com/repos")
		};
		ref CoreInfo reference = ref span[1];
		CoreInfo coreInfo = new CoreInfo
		{
			CoreType = ECoreType.v2fly
		};
		int num2 = 1;
		List<string> list2 = new List<string>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		CollectionsMarshal.AsSpan(list2)[0] = "v2ray";
		coreInfo.CoreExes = list2;
		coreInfo.Arguments = "{0}";
		coreInfo.Url = GetCoreUrl(ECoreType.v2fly);
		coreInfo.Match = "V2Ray";
		coreInfo.VersionArg = "-version";
		coreInfo.Environment = new Dictionary<string, string> { 
		{
			"V2RAY_LOCATION_ASSET",
			Utils.GetBinPath("")
		} };
		reference = coreInfo;
		ref CoreInfo reference2 = ref span[2];
		CoreInfo coreInfo2 = new CoreInfo
		{
			CoreType = ECoreType.v2fly_v5
		};
		num2 = 1;
		List<string> list3 = new List<string>(num2);
		CollectionsMarshal.SetCount(list3, num2);
		CollectionsMarshal.AsSpan(list3)[0] = "v2ray";
		coreInfo2.CoreExes = list3;
		coreInfo2.Arguments = "run -c {0} -format jsonv5";
		coreInfo2.Url = GetCoreUrl(ECoreType.v2fly_v5);
		coreInfo2.Match = "V2Ray";
		coreInfo2.VersionArg = "version";
		coreInfo2.Environment = new Dictionary<string, string> { 
		{
			"V2RAY_LOCATION_ASSET",
			Utils.GetBinPath("")
		} };
		reference2 = coreInfo2;
		ref CoreInfo reference3 = ref span[3];
		CoreInfo coreInfo3 = new CoreInfo
		{
			CoreType = ECoreType.Xray
		};
		num2 = 1;
		List<string> list4 = new List<string>(num2);
		CollectionsMarshal.SetCount(list4, num2);
		CollectionsMarshal.AsSpan(list4)[0] = "xray";
		coreInfo3.CoreExes = list4;
		coreInfo3.Arguments = "run -c {0}";
		coreInfo3.Match = "Xray";
		coreInfo3.VersionArg = "-version";
		coreInfo3.Environment = new Dictionary<string, string>
		{
			{
				"XRAY_LOCATION_ASSET",
				Utils.GetBinPath("", ECoreType.Xray.ToString())
			},
			{
				"XRAY_LOCATION_CERT",
				Utils.GetBinPath("", ECoreType.Xray.ToString())
			}
		};
		reference3 = coreInfo3;
		span[4] = new CoreInfo
		{
			CoreType = ECoreType.mihomo,
			CoreExes = GetMihomoCoreExes(),
			Arguments = "-f {0}" + PortableMode(),
			Url = GetCoreUrl(ECoreType.mihomo),
			ReleaseApiUrl = coreUrl2.Replace("https://github.com", "https://api.github.com/repos"),
			DownloadUrlWin64 = coreUrl2 + "/download/{0}/mihomo-windows-amd64-v1-{0}.zip",
			DownloadUrlWinArm64 = coreUrl2 + "/download/{0}/mihomo-windows-arm64-{0}.zip",
			DownloadUrlLinux64 = coreUrl2 + "/download/{0}/mihomo-linux-amd64-v1-{0}.gz",
			DownloadUrlLinuxArm64 = coreUrl2 + "/download/{0}/mihomo-linux-arm64-{0}.gz",
			DownloadUrlLinuxRiscV64 = coreUrl2 + "/download/{0}/mihomo-linux-riscv64-{0}.gz",
			DownloadUrlLinuxLoong64 = coreUrl2 + "/download/{0}/mihomo-linux-loong64-abi2-{0}.gz",
			DownloadUrlOSX64 = coreUrl2 + "/download/{0}/mihomo-darwin-amd64-v1-{0}.gz",
			DownloadUrlOSXArm64 = coreUrl2 + "/download/{0}/mihomo-darwin-arm64-{0}.gz",
			Match = "Mihomo",
			VersionArg = "-v"
		};
		ref CoreInfo reference4 = ref span[5];
		CoreInfo coreInfo4 = new CoreInfo
		{
			CoreType = ECoreType.hysteria
		};
		num2 = 1;
		List<string> list5 = new List<string>(num2);
		CollectionsMarshal.SetCount(list5, num2);
		CollectionsMarshal.AsSpan(list5)[0] = "hysteria";
		coreInfo4.CoreExes = list5;
		coreInfo4.Arguments = "";
		coreInfo4.Url = GetCoreUrl(ECoreType.hysteria);
		reference4 = coreInfo4;
		ref CoreInfo reference5 = ref span[6];
		CoreInfo coreInfo5 = new CoreInfo
		{
			CoreType = ECoreType.naiveproxy
		};
		num2 = 2;
		List<string> list6 = new List<string>(num2);
		CollectionsMarshal.SetCount(list6, num2);
		Span<string> span2 = CollectionsMarshal.AsSpan(list6);
		span2[0] = "naive";
		span2[1] = "naiveproxy";
		coreInfo5.CoreExes = list6;
		coreInfo5.Arguments = "{0}";
		coreInfo5.Url = GetCoreUrl(ECoreType.naiveproxy);
		reference5 = coreInfo5;
		ref CoreInfo reference6 = ref span[7];
		CoreInfo coreInfo6 = new CoreInfo
		{
			CoreType = ECoreType.tuic
		};
		num2 = 2;
		List<string> list7 = new List<string>(num2);
		CollectionsMarshal.SetCount(list7, num2);
		Span<string> span3 = CollectionsMarshal.AsSpan(list7);
		span3[0] = "tuic-client";
		span3[1] = "tuic";
		coreInfo6.CoreExes = list7;
		coreInfo6.Arguments = "-c {0}";
		coreInfo6.Url = GetCoreUrl(ECoreType.tuic);
		reference6 = coreInfo6;
		ref CoreInfo reference7 = ref span[8];
		CoreInfo coreInfo7 = new CoreInfo
		{
			CoreType = ECoreType.sing_box
		};
		num2 = 2;
		List<string> list8 = new List<string>(num2);
		CollectionsMarshal.SetCount(list8, num2);
		Span<string> span4 = CollectionsMarshal.AsSpan(list8);
		span4[0] = "sing-box-client";
		span4[1] = "sing-box";
		coreInfo7.CoreExes = list8;
		coreInfo7.Arguments = "run -c {0} --disable-color";
		coreInfo7.Url = GetCoreUrl(ECoreType.sing_box);
		coreInfo7.ReleaseApiUrl = coreUrl3.Replace("https://github.com", "https://api.github.com/repos");
		coreInfo7.DownloadUrlWin64 = coreUrl3 + "/download/{0}/sing-box-{1}-windows-amd64.zip";
		coreInfo7.DownloadUrlWinArm64 = coreUrl3 + "/download/{0}/sing-box-{1}-windows-arm64.zip";
		coreInfo7.DownloadUrlLinux64 = coreUrl3 + "/download/{0}/sing-box-{1}-linux-amd64.tar.gz";
		coreInfo7.DownloadUrlLinuxArm64 = coreUrl3 + "/download/{0}/sing-box-{1}-linux-arm64.tar.gz";
		coreInfo7.DownloadUrlLinuxRiscV64 = coreUrl3 + "/download/{0}/sing-box-{1}-linux-riscv64.tar.gz";
		coreInfo7.DownloadUrlLinuxLoong64 = coreUrl3 + "/download/{0}/sing-box-{1}-linux-loong64.tar.gz";
		coreInfo7.DownloadUrlOSX64 = coreUrl3 + "/download/{0}/sing-box-{1}-darwin-amd64.tar.gz";
		coreInfo7.DownloadUrlOSXArm64 = coreUrl3 + "/download/{0}/sing-box-{1}-darwin-arm64.tar.gz";
		coreInfo7.Match = "sing-box";
		coreInfo7.VersionArg = "version";
		reference7 = coreInfo7;
		ref CoreInfo reference8 = ref span[9];
		CoreInfo coreInfo8 = new CoreInfo
		{
			CoreType = ECoreType.juicity
		};
		num2 = 2;
		List<string> list9 = new List<string>(num2);
		CollectionsMarshal.SetCount(list9, num2);
		Span<string> span5 = CollectionsMarshal.AsSpan(list9);
		span5[0] = "juicity-client";
		span5[1] = "juicity";
		coreInfo8.CoreExes = list9;
		coreInfo8.Arguments = "run -c {0}";
		coreInfo8.Url = GetCoreUrl(ECoreType.juicity);
		reference8 = coreInfo8;
		ref CoreInfo reference9 = ref span[10];
		CoreInfo coreInfo9 = new CoreInfo
		{
			CoreType = ECoreType.hysteria2
		};
		num2 = 3;
		List<string> list10 = new List<string>(num2);
		CollectionsMarshal.SetCount(list10, num2);
		Span<string> span6 = CollectionsMarshal.AsSpan(list10);
		span6[0] = "hysteria-windows-amd64";
		span6[1] = "hysteria-linux-amd64";
		span6[2] = "hysteria";
		coreInfo9.CoreExes = list10;
		coreInfo9.Arguments = "";
		coreInfo9.Url = GetCoreUrl(ECoreType.hysteria2);
		reference9 = coreInfo9;
		ref CoreInfo reference10 = ref span[11];
		CoreInfo coreInfo10 = new CoreInfo
		{
			CoreType = ECoreType.brook
		};
		num2 = 3;
		List<string> list11 = new List<string>(num2);
		CollectionsMarshal.SetCount(list11, num2);
		Span<string> span7 = CollectionsMarshal.AsSpan(list11);
		span7[0] = "brook_windows_amd64";
		span7[1] = "brook_linux_amd64";
		span7[2] = "brook";
		coreInfo10.CoreExes = list11;
		coreInfo10.Arguments = " {0}";
		coreInfo10.Url = GetCoreUrl(ECoreType.brook);
		coreInfo10.AbsolutePath = true;
		reference10 = coreInfo10;
		ref CoreInfo reference11 = ref span[12];
		CoreInfo coreInfo11 = new CoreInfo
		{
			CoreType = ECoreType.overtls
		};
		num2 = 2;
		List<string> list12 = new List<string>(num2);
		CollectionsMarshal.SetCount(list12, num2);
		Span<string> span8 = CollectionsMarshal.AsSpan(list12);
		span8[0] = "overtls-bin";
		span8[1] = "overtls";
		coreInfo11.CoreExes = list12;
		coreInfo11.Arguments = "-r client -c {0}";
		coreInfo11.Url = GetCoreUrl(ECoreType.overtls);
		coreInfo11.AbsolutePath = false;
		reference11 = coreInfo11;
		ref CoreInfo reference12 = ref span[13];
		CoreInfo coreInfo12 = new CoreInfo
		{
			CoreType = ECoreType.shadowquic
		};
		num2 = 1;
		List<string> list13 = new List<string>(num2);
		CollectionsMarshal.SetCount(list13, num2);
		CollectionsMarshal.AsSpan(list13)[0] = "shadowquic";
		coreInfo12.CoreExes = list13;
		coreInfo12.Arguments = "-c {0}";
		coreInfo12.Url = GetCoreUrl(ECoreType.shadowquic);
		coreInfo12.AbsolutePath = false;
		reference12 = coreInfo12;
		ref CoreInfo reference13 = ref span[14];
		CoreInfo coreInfo13 = new CoreInfo
		{
			CoreType = ECoreType.mieru
		};
		num2 = 1;
		List<string> list14 = new List<string>(num2);
		CollectionsMarshal.SetCount(list14, num2);
		CollectionsMarshal.AsSpan(list14)[0] = "mieru";
		coreInfo13.CoreExes = list14;
		coreInfo13.Arguments = "run";
		coreInfo13.Url = GetCoreUrl(ECoreType.mieru);
		coreInfo13.AbsolutePath = false;
		coreInfo13.Environment = new Dictionary<string, string> { { "MIERU_CONFIG_JSON_FILE", "{0}" } };
		reference13 = coreInfo13;
		_coreInfo = list;
	}

	private static string PortableMode()
	{
		return " -d " + Utils.GetBinPath("").AppendQuotes();
	}

	private static string GetCoreUrl(ECoreType eCoreType)
	{
		return "https://github.com/" + Global.CoreUrls[eCoreType] + "/releases";
	}

	private static List<string>? GetMihomoCoreExes()
	{
		List<string> list = new List<string>();
		if (Utils.IsWindows())
		{
			list.Add("mihomo-windows-amd64-v1");
			list.Add("mihomo-windows-amd64-compatible");
			list.Add("mihomo-windows-amd64");
			list.Add("mihomo-windows-arm64");
		}
		else if (Utils.IsLinux())
		{
			list.Add("mihomo-linux-amd64-v1");
			list.Add("mihomo-linux-amd64");
			list.Add("mihomo-linux-arm64");
			list.Add("mihomo-linux-riscv64");
			list.Add("mihomo-linux-loong64-abi2");
		}
		else if (Utils.IsMacOS())
		{
			list.Add("mihomo-darwin-amd64-v1");
			list.Add("mihomo-darwin-amd64");
			list.Add("mihomo-darwin-arm64");
		}
		list.Add("clash");
		list.Add("mihomo");
		return list;
	}
}

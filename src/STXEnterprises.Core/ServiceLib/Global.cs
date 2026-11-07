using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ServiceLib.Enums;

namespace ServiceLib;

public class Global
{
	public const string AppName = "STX Enterprises";

	public const string TunAdapterName = "STX Enterprises TUN";

	public const string GithubUrl = "https://github.com";

	public const string GithubApiUrl = "https://api.github.com/repos";

	public const string GeoUrl = "https://github.com/Loyalsoldier/v2ray-rules-dat/releases/latest/download/{0}.dat";

	public const string SingboxRulesetUrl = "";

	public const string PromotionUrl = "aHR0cHM6Ly85LjIzNDQ1Ni54eXovYWJjLmh0bWw=";

	public const string ConfigFileName = "guiNConfig.json";

	public const string CoreConfigFileName = "config.json";

	public const string CorePreConfigFileName = "configPre.json";

	public const string CoreSpeedtestConfigFileName = "configTest{0}.json";

	public const string ClashMixinConfigFileName = "Mixin.yaml";

	public const string NamespaceSample = "ServiceLib.Sample.";

	public const string V2raySampleClient = "ServiceLib.Sample.SampleClientConfig";

	public const string SingboxSampleClient = "ServiceLib.Sample.SingboxSampleClientConfig";

	public const string V2raySampleHttpRequestFileName = "ServiceLib.Sample.SampleHttpRequest";

	public const string V2raySampleHttpResponseFileName = "ServiceLib.Sample.SampleHttpResponse";

	public const string V2raySampleInbound = "ServiceLib.Sample.SampleInbound";

	public const string V2raySampleOutbound = "ServiceLib.Sample.SampleOutbound";

	public const string V2raySampleTunInbound = "ServiceLib.Sample.SampleTunInbound";

	public const string V2raySampleTunRules = "ServiceLib.Sample.SampleTunRules";

	public const string SingboxSampleOutbound = "ServiceLib.Sample.SingboxSampleOutbound";

	public const string CustomRoutingFileName = "ServiceLib.Sample.custom_routing_";

	public const string TunSingboxDNSFileName = "ServiceLib.Sample.tun_singbox_dns";

	public const string TunSingboxInboundFileName = "ServiceLib.Sample.tun_singbox_inbound";

	public const string TunSingboxRulesFileName = "ServiceLib.Sample.tun_singbox_rules";

	public const string DNSV2rayDefaultFileName = "ServiceLib.Sample.dns_v2ray_normal";

	public const string DNSSingboxNormalFileName = "ServiceLib.Sample.dns_singbox_normal";

	public const string ClashMixinYaml = "ServiceLib.Sample.clash_mixin_yaml";

	public const string ClashTunYaml = "ServiceLib.Sample.clash_tun_yaml";

	public const string LinuxAutostartConfig = "ServiceLib.Sample.linux_autostart_config";

	public const string PacFileName = "ServiceLib.Sample.pac";

	public const string ProxySetOSXShellFileName = "ServiceLib.Sample.proxy_set_osx_sh";

	public const string ProxySetLinuxShellFileName = "ServiceLib.Sample.proxy_set_linux_sh";

	public const string KillAsSudoOSXShellFileName = "ServiceLib.Sample.kill_as_sudo_osx_sh";

	public const string KillAsSudoLinuxShellFileName = "ServiceLib.Sample.kill_as_sudo_linux_sh";

	public const string SingboxFakeIPFilterFileName = "ServiceLib.Sample.singbox_fakeip_filter";

	public const string DefaultSecurity = "auto";

	public const string DefaultNetwork = "raw";

	public const string RawHeaderHttp = "http";

	public const string None = "none";

	public const string RawNetworkAlias = "tcp";

	public const string DefaultXhttpMode = "auto";

	public const string ProxyTag = "proxy";

	public const string DirectTag = "direct";

	public const string BlockTag = "block";

	public const string DnsOutboundTag = "dns";

	public const string DnsTag = "dns-module";

	public const string DirectDnsTag = "direct-dns";

	public const string BalancerTagSuffix = "-balancer";

	public const string StreamSecurity = "tls";

	public const string StreamSecurityReality = "reality";

	public const string Loopback = "127.0.0.1";

	public const string InboundAPIProtocol = "dokodemo-door";

	public const string HttpProtocol = "http://";

	public const string HttpsProtocol = "https://";

	public const string SocksProtocol = "socks://";

	public const string Socks5Protocol = "socks5://";

	public const string InnerUriProtocol = "stxenterprises://";

	public const string AsIs = "AsIs";

	public const string IPIfNonMatch = "IPIfNonMatch";

	public const string IPOnDemand = "IPOnDemand";

	public const string GeoSitePrefix = "geosite:";

	public const string GeoIPPrefix = "geoip:";

	public const string UserEMail = "t@t.tt";

	public const string AutoRunRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	public const string AutoRunName = "STXEnterprisesAutoRun";

	public const string SystemProxyExceptionsWindows = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*";

	public const string SystemProxyExceptionsLinux = "localhost,127.0.0.0/8,::1";

	public const string RoutingRuleComma = "<COMMA>";

	public const string GrpcGunMode = "gun";

	public const string GrpcMultiMode = "multi";

	public const int MaxPort = 65536;

	public const int MinFontSize = 8;

	public const int MinFontSizeCount = 13;

	public const string RebootAs = "rebootas";

	public const string AvaAssets = "avares://STXEnterprises/Assets/";

	public const string LocalAppData = "STXENTERPRISES_LOCAL_APPLICATION_DATA";

	public const string V2RayLocalAsset = "V2RAY_LOCATION_ASSET";

	public const string XrayLocalAsset = "XRAY_LOCATION_ASSET";

	public const string XrayLocalCert = "XRAY_LOCATION_CERT";

	public const int SpeedTestPageSize = 1000;

	public const string LinuxBash = "/bin/bash";

	public const string SingboxDirectDNSTag = "direct_dns";

	public const string SingboxRemoteDNSTag = "remote_dns";

	public const string SingboxLocalDNSTag = "local_local";

	public const string SingboxHostsDNSTag = "hosts_dns";

	public const string SingboxFakeDNSTag = "fake_dns";

	public const int Hysteria2DefaultHopInt = 30;

	public const string PolicyGroupExcludeKeywords = "剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset";

	public const string PolicyGroupDefaultAllFilter = "^(?!.*(?:剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset)).*$";

	public static readonly List<string> PolicyGroupDefaultFilterList;

	public static readonly List<string> IEProxyProtocols;

	public static readonly List<string> SubConvertUrls;

	public static readonly List<string> SubConvertConfig;

	public static readonly List<string> SubConvertTargets;

	public static readonly List<string> SpeedTestUrls;

	public static readonly List<string> SpeedPingTestUrls;

	public static readonly List<string> GeoFilesSources;

	public static readonly List<string> SingboxRulesetSources;

	public static readonly List<string> RoutingRulesSources;

	public static readonly List<string> DNSTemplateSources;

	public static readonly Dictionary<string, string> RawHttpUserAgentTexts;

	public const string Hysteria2ProtocolShare = "hy2://";

	public const string NaiveHttpsProtocolShare = "naive+https://";

	public const string NaiveQuicProtocolShare = "naive+quic://";

	public const string SOCKS5Protocol = "socks5://";

	public const string SOCKS4Protocol = "socks4://";

	public static readonly Dictionary<EConfigType, string> ProtocolShares;

	public static readonly Dictionary<EConfigType, string> ProtocolTypes;

	public static readonly List<string> VmessSecurities;

	public static readonly List<string> SsSecurities;

	public static readonly List<string> SsSecuritiesInXray;

	public static readonly List<string> SsSecuritiesInSingbox;

	public static readonly List<string> Flows;

	public static readonly List<string> Networks;

	public static readonly List<string> KcpHeaderTypes;

	public static readonly Dictionary<string, string> KcpHeaderMaskMap;

	public static readonly List<string> CoreTypes;

	public static readonly HashSet<EConfigType> XraySupportConfigType;

	public static readonly HashSet<EConfigType> SingboxSupportConfigType;

	public static readonly HashSet<EConfigType> SingboxOnlyConfigType;

	public static readonly List<string> DomainStrategies;

	public static readonly List<string> DomainStrategies4Sbox;

	public static readonly List<string> Fingerprints;

	public static readonly List<string> UserAgent;

	public static readonly List<string> XhttpMode;

	public static readonly List<string> AllowInsecure;

	public static readonly List<string> DomainStrategy;

	public static readonly List<string> DomainDirectDNSAddress;

	public static readonly List<string> DomainRemoteDNSAddress;

	public static readonly List<string> DomainPureIPDNSAddress;

	public static readonly List<string> Languages;

	public static readonly List<string> Alpns;

	public static readonly List<string> LogLevels;

	public static readonly Dictionary<string, string> LogLevelColors;

	public static readonly List<string> InboundTags;

	public static readonly List<string> RuleProtocols;

	public static readonly List<string> RuleNetworks;

	public static readonly List<string> destOverrideProtocols;

	public static readonly List<int> TunMtus;

	public static readonly List<string> TunStacks;

	public static readonly List<string> PresetMsgFilters;

	public static readonly List<string> SingboxMuxs;

	public static readonly List<string> TuicCongestionControls;

	public static readonly List<string> NaiveCongestionControls;

	public static readonly List<string> allowSelectType;

	public static readonly List<string> notAllowTestType;

	public static readonly List<string> proxyVehicleType;

	public static readonly Dictionary<ECoreType, string> CoreUrls;

	public static readonly List<string> OtherGeoUrls;

	public static readonly List<string> IPAPIUrls;

	public static readonly List<string> UdpTestTargets;

	public static readonly List<string> OutboundTags;

	public static readonly Dictionary<string, List<string>> PredefinedHosts;

	public static readonly List<string> ExpectedIPs;

	public static readonly List<string> TunIcmpRoutingPolicies;

	static Global()
	{
		int num = 4;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = "^(?!.*(?:剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset)).*$";
		span[1] = "^.*(?:[×xX✕*]\\s*0\\.[0-9]+|0\\.[0-9]+\\s*[×xX✕*倍]).*$";
		span[2] = "^(?!.*(?:剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset)).*(?:专线|IPLC|IEPL|中转).*$";
		span[3] = "^(?!.*(?:剩余|过期|到期|重置|[Rr]emaining|[Ee]xpir|[Rr]eset)).*(?:日本|\\\\b[Jj][Pp]\\\\b|\ud83c\uddef\ud83c\uddf5|[Jj]apan).*$";
		PolicyGroupDefaultFilterList = list;
		num = 5;
		List<string> list2 = new List<string>(num);
		CollectionsMarshal.SetCount(list2, num);
		Span<string> span2 = CollectionsMarshal.AsSpan(list2);
		span2[0] = "{ip}:{http_port}";
		span2[1] = "socks={ip}:{socks_port}";
		span2[2] = "http={ip}:{http_port};https={ip}:{http_port};ftp={ip}:{http_port};socks={ip}:{socks_port}";
		span2[3] = "http=http://{ip}:{http_port};https=http://{ip}:{http_port}";
		span2[4] = "";
		IEProxyProtocols = list2;
		num = 4;
		List<string> list3 = new List<string>(num);
		CollectionsMarshal.SetCount(list3, num);
		Span<string> span3 = CollectionsMarshal.AsSpan(list3);
		span3[0] = "https://sub.xeton.dev/sub?url={0}";
		span3[1] = "https://api.dler.io/sub?url={0}";
		span3[2] = "http://127.0.0.1:25500/sub?url={0}";
		span3[3] = "";
		SubConvertUrls = list3;
		num = 1;
		List<string> list4 = new List<string>(num);
		CollectionsMarshal.SetCount(list4, num);
		CollectionsMarshal.AsSpan(list4)[0] = "https://raw.githubusercontent.com/ACL4SSR/ACL4SSR/master/Clash/config/ACL4SSR_Online.ini";
		SubConvertConfig = list4;
		num = 5;
		List<string> list5 = new List<string>(num);
		CollectionsMarshal.SetCount(list5, num);
		Span<string> span4 = CollectionsMarshal.AsSpan(list5);
		span4[0] = "";
		span4[1] = "mixed";
		span4[2] = "v2ray";
		span4[3] = "clash";
		span4[4] = "ss";
		SubConvertTargets = list5;
		num = 7;
		List<string> list6 = new List<string>(num);
		CollectionsMarshal.SetCount(list6, num);
		Span<string> span5 = CollectionsMarshal.AsSpan(list6);
		span5[0] = "https://cachefly.cachefly.net/50mb.test";
		span5[1] = "https://cachefly.cachefly.net/100mb.test";
		span5[2] = "https://cachefly.cachefly.net/1mb.test";
		span5[3] = "https://cachefly.cachefly.net/10mb.test";
		span5[4] = "https://speed.cloudflare.com/__down?bytes=10000000";
		span5[5] = "https://speed.cloudflare.com/__down?bytes=50000000";
		span5[6] = "https://speed.cloudflare.com/__down?bytes=99999999";
		SpeedTestUrls = list6;
		num = 6;
		List<string> list7 = new List<string>(num);
		CollectionsMarshal.SetCount(list7, num);
		Span<string> span6 = CollectionsMarshal.AsSpan(list7);
		span6[0] = "https://www.google.com/generate_204";
		span6[1] = "https://www.youtube.com/generate_204";
		span6[2] = "https://www.googlevideo.com/generate_204";
		span6[3] = "https://www.gstatic.com/generate_204";
		span6[4] = "https://www.apple.com/library/test/success.html";
		span6[5] = "http://www.msftconnecttest.com/connecttest.txt";
		SpeedPingTestUrls = list7;
		num = 3;
		List<string> list8 = new List<string>(num);
		CollectionsMarshal.SetCount(list8, num);
		Span<string> span7 = CollectionsMarshal.AsSpan(list8);
		span7[0] = "";
		span7[1] = "https://github.com/runetfreedom/russia-v2ray-rules-dat/releases/latest/download/{0}.dat";
		span7[2] = "https://github.com/Chocolate4U/Iran-v2ray-rules/releases/latest/download/{0}.dat";
		GeoFilesSources = list8;
		num = 3;
		List<string> list9 = new List<string>(num);
		CollectionsMarshal.SetCount(list9, num);
		Span<string> span8 = CollectionsMarshal.AsSpan(list9);
		span8[0] = "";
		span8[1] = "https://raw.githubusercontent.com/runetfreedom/russia-v2ray-rules-dat/release/sing-box/rule-set-{0}/{1}.srs";
		span8[2] = "https://raw.githubusercontent.com/chocolate4u/Iran-sing-box-rules/rule-set/{1}.srs";
		SingboxRulesetSources = list9;
		num = 1;
		List<string> list10 = new List<string>(num);
		CollectionsMarshal.SetCount(list10, num);
		CollectionsMarshal.AsSpan(list10)[0] = "";
		RoutingRulesSources = list10;
		num = 1;
		List<string> list11 = new List<string>(num);
		CollectionsMarshal.SetCount(list11, num);
		CollectionsMarshal.AsSpan(list11)[0] = "";
		DNSTemplateSources = list11;
		RawHttpUserAgentTexts = new Dictionary<string, string>
		{
			{ "chrome", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36" },
			{ "firefox", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:90.0) Gecko/20100101 Firefox/90.0" },
			{ "safari", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15" },
			{ "edge", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.70" },
			{ "none", "" },
			{ "golang", "Go-http-client/1.1" },
			{ "curl", "curl/7.68.0" }
		};
		ProtocolShares = new Dictionary<EConfigType, string>
		{
			{
				EConfigType.VMess,
				"vmess://"
			},
			{
				EConfigType.Shadowsocks,
				"ss://"
			},
			{
				EConfigType.SOCKS,
				"socks://"
			},
			{
				EConfigType.VLESS,
				"vless://"
			},
			{
				EConfigType.Trojan,
				"trojan://"
			},
			{
				EConfigType.Hysteria2,
				"hysteria2://"
			},
			{
				EConfigType.TUIC,
				"tuic://"
			},
			{
				EConfigType.WireGuard,
				"wireguard://"
			},
			{
				EConfigType.Anytls,
				"anytls://"
			},
			{
				EConfigType.Naive,
				"naive://"
			}
		};
		ProtocolTypes = new Dictionary<EConfigType, string>
		{
			{
				EConfigType.VMess,
				"vmess"
			},
			{
				EConfigType.Shadowsocks,
				"shadowsocks"
			},
			{
				EConfigType.SOCKS,
				"socks"
			},
			{
				EConfigType.HTTP,
				"http"
			},
			{
				EConfigType.VLESS,
				"vless"
			},
			{
				EConfigType.Trojan,
				"trojan"
			},
			{
				EConfigType.Hysteria2,
				"hysteria2"
			},
			{
				EConfigType.TUIC,
				"tuic"
			},
			{
				EConfigType.WireGuard,
				"wireguard"
			},
			{
				EConfigType.Anytls,
				"anytls"
			},
			{
				EConfigType.Naive,
				"naive"
			}
		};
		num = 5;
		List<string> list12 = new List<string>(num);
		CollectionsMarshal.SetCount(list12, num);
		Span<string> span9 = CollectionsMarshal.AsSpan(list12);
		span9[0] = "aes-128-gcm";
		span9[1] = "chacha20-poly1305";
		span9[2] = "auto";
		span9[3] = "none";
		span9[4] = "zero";
		VmessSecurities = list12;
		num = 6;
		List<string> list13 = new List<string>(num);
		CollectionsMarshal.SetCount(list13, num);
		Span<string> span10 = CollectionsMarshal.AsSpan(list13);
		span10[0] = "aes-256-gcm";
		span10[1] = "aes-128-gcm";
		span10[2] = "chacha20-poly1305";
		span10[3] = "chacha20-ietf-poly1305";
		span10[4] = "none";
		span10[5] = "plain";
		SsSecurities = list13;
		num = 11;
		List<string> list14 = new List<string>(num);
		CollectionsMarshal.SetCount(list14, num);
		Span<string> span11 = CollectionsMarshal.AsSpan(list14);
		span11[0] = "aes-256-gcm";
		span11[1] = "aes-128-gcm";
		span11[2] = "chacha20-poly1305";
		span11[3] = "chacha20-ietf-poly1305";
		span11[4] = "xchacha20-poly1305";
		span11[5] = "xchacha20-ietf-poly1305";
		span11[6] = "none";
		span11[7] = "plain";
		span11[8] = "2022-blake3-aes-128-gcm";
		span11[9] = "2022-blake3-aes-256-gcm";
		span11[10] = "2022-blake3-chacha20-poly1305";
		SsSecuritiesInXray = list14;
		num = 18;
		List<string> list15 = new List<string>(num);
		CollectionsMarshal.SetCount(list15, num);
		Span<string> span12 = CollectionsMarshal.AsSpan(list15);
		span12[0] = "aes-256-gcm";
		span12[1] = "aes-192-gcm";
		span12[2] = "aes-128-gcm";
		span12[3] = "chacha20-ietf-poly1305";
		span12[4] = "xchacha20-ietf-poly1305";
		span12[5] = "none";
		span12[6] = "2022-blake3-aes-128-gcm";
		span12[7] = "2022-blake3-aes-256-gcm";
		span12[8] = "2022-blake3-chacha20-poly1305";
		span12[9] = "aes-128-ctr";
		span12[10] = "aes-192-ctr";
		span12[11] = "aes-256-ctr";
		span12[12] = "aes-128-cfb";
		span12[13] = "aes-192-cfb";
		span12[14] = "aes-256-cfb";
		span12[15] = "rc4-md5";
		span12[16] = "chacha20-ietf";
		span12[17] = "xchacha20";
		SsSecuritiesInSingbox = list15;
		num = 3;
		List<string> list16 = new List<string>(num);
		CollectionsMarshal.SetCount(list16, num);
		Span<string> span13 = CollectionsMarshal.AsSpan(list16);
		span13[0] = "";
		span13[1] = "xtls-rprx-vision";
		span13[2] = "xtls-rprx-vision-udp443";
		Flows = list16;
		num = 6;
		List<string> list17 = new List<string>(num);
		CollectionsMarshal.SetCount(list17, num);
		Span<string> span14 = CollectionsMarshal.AsSpan(list17);
		span14[0] = "raw";
		span14[1] = "xhttp";
		span14[2] = "kcp";
		span14[3] = "grpc";
		span14[4] = "ws";
		span14[5] = "httpupgrade";
		Networks = list17;
		num = 6;
		List<string> list18 = new List<string>(num);
		CollectionsMarshal.SetCount(list18, num);
		Span<string> span15 = CollectionsMarshal.AsSpan(list18);
		span15[0] = "srtp";
		span15[1] = "utp";
		span15[2] = "wechat-video";
		span15[3] = "dtls";
		span15[4] = "wireguard";
		span15[5] = "dns";
		KcpHeaderTypes = list18;
		KcpHeaderMaskMap = new Dictionary<string, string>
		{
			{ "srtp", "header-srtp" },
			{ "utp", "header-utp" },
			{ "wechat-video", "header-wechat" },
			{ "dtls", "header-dtls" },
			{ "wireguard", "header-wireguard" },
			{ "dns", "header-dns" }
		};
		num = 2;
		List<string> list19 = new List<string>(num);
		CollectionsMarshal.SetCount(list19, num);
		Span<string> span16 = CollectionsMarshal.AsSpan(list19);
		span16[0] = "Xray";
		span16[1] = "sing_box";
		CoreTypes = list19;
		XraySupportConfigType = new HashSet<EConfigType>
		{
			EConfigType.VMess,
			EConfigType.VLESS,
			EConfigType.Shadowsocks,
			EConfigType.Trojan,
			EConfigType.Hysteria2,
			EConfigType.WireGuard,
			EConfigType.SOCKS,
			EConfigType.HTTP
		};
		SingboxSupportConfigType = new HashSet<EConfigType>
		{
			EConfigType.VMess,
			EConfigType.VLESS,
			EConfigType.Shadowsocks,
			EConfigType.Trojan,
			EConfigType.Hysteria2,
			EConfigType.TUIC,
			EConfigType.Anytls,
			EConfigType.Naive,
			EConfigType.WireGuard,
			EConfigType.SOCKS,
			EConfigType.HTTP
		};
		SingboxOnlyConfigType = SingboxSupportConfigType.Except(XraySupportConfigType).ToHashSet();
		num = 3;
		List<string> list20 = new List<string>(num);
		CollectionsMarshal.SetCount(list20, num);
		Span<string> span17 = CollectionsMarshal.AsSpan(list20);
		span17[0] = "AsIs";
		span17[1] = "IPIfNonMatch";
		span17[2] = "IPOnDemand";
		DomainStrategies = list20;
		num = 5;
		List<string> list21 = new List<string>(num);
		CollectionsMarshal.SetCount(list21, num);
		Span<string> span18 = CollectionsMarshal.AsSpan(list21);
		span18[0] = "";
		span18[1] = "prefer_ipv4";
		span18[2] = "prefer_ipv6";
		span18[3] = "ipv4_only";
		span18[4] = "ipv6_only";
		DomainStrategies4Sbox = list21;
		num = 11;
		List<string> list22 = new List<string>(num);
		CollectionsMarshal.SetCount(list22, num);
		Span<string> span19 = CollectionsMarshal.AsSpan(list22);
		span19[0] = "chrome";
		span19[1] = "firefox";
		span19[2] = "safari";
		span19[3] = "ios";
		span19[4] = "android";
		span19[5] = "edge";
		span19[6] = "360";
		span19[7] = "qq";
		span19[8] = "random";
		span19[9] = "randomized";
		span19[10] = "";
		Fingerprints = list22;
		num = 5;
		List<string> list23 = new List<string>(num);
		CollectionsMarshal.SetCount(list23, num);
		Span<string> span20 = CollectionsMarshal.AsSpan(list23);
		span20[0] = "chrome";
		span20[1] = "firefox";
		span20[2] = "edge";
		span20[3] = "curl";
		span20[4] = "golang";
		UserAgent = list23;
		num = 4;
		List<string> list24 = new List<string>(num);
		CollectionsMarshal.SetCount(list24, num);
		Span<string> span21 = CollectionsMarshal.AsSpan(list24);
		span21[0] = "auto";
		span21[1] = "packet-up";
		span21[2] = "stream-up";
		span21[3] = "stream-one";
		XhttpMode = list24;
		num = 3;
		List<string> list25 = new List<string>(num);
		CollectionsMarshal.SetCount(list25, num);
		Span<string> span22 = CollectionsMarshal.AsSpan(list25);
		span22[0] = "true";
		span22[1] = "false";
		span22[2] = "";
		AllowInsecure = list25;
		num = 7;
		List<string> list26 = new List<string>(num);
		CollectionsMarshal.SetCount(list26, num);
		Span<string> span23 = CollectionsMarshal.AsSpan(list26);
		span23[0] = "AsIs";
		span23[1] = "UseIP";
		span23[2] = "UseIPv4v6";
		span23[3] = "UseIPv6v4";
		span23[4] = "UseIPv4";
		span23[5] = "UseIPv6";
		span23[6] = "";
		DomainStrategy = list26;
		num = 9;
		List<string> list27 = new List<string>(num);
		CollectionsMarshal.SetCount(list27, num);
		Span<string> span24 = CollectionsMarshal.AsSpan(list27);
		span24[0] = "1.1.1.1";
		span24[1] = "8.8.8.8";
		span24[2] = "119.29.29.29";
		span24[3] = "223.5.5.5";
		span24[4] = "119.29.29.29,223.5.5.5,https://doh.pub/dns-query";
		span24[5] = "https://doh.pub/dns-query";
		span24[6] = "https://dns.alidns.com/dns-query";
		span24[7] = "https://doh.pub/dns-query,https://dns.alidns.com/dns-query";
		span24[8] = "localhost";
		DomainDirectDNSAddress = list27;
		num = 12;
		List<string> list28 = new List<string>(num);
		CollectionsMarshal.SetCount(list28, num);
		Span<string> span25 = CollectionsMarshal.AsSpan(list28);
		span25[0] = "https://cloudflare-dns.com/dns-query";
		span25[1] = "https://dns.google/dns-query";
		span25[2] = "https://cloudflare-dns.com/dns-query,https://dns.google/dns-query,8.8.8.8";
		span25[3] = "https://dns.cloudflare.com/dns-query";
		span25[4] = "https://doh.dns.sb/dns-query";
		span25[5] = "https://doh.opendns.com/dns-query";
		span25[6] = "https://common.dot.dns.yandex.net/dns-query";
		span25[7] = "8.8.8.8";
		span25[8] = "1.1.1.1";
		span25[9] = "185.222.222.222";
		span25[10] = "208.67.222.222";
		span25[11] = "77.88.8.8";
		DomainRemoteDNSAddress = list28;
		num = 5;
		List<string> list29 = new List<string>(num);
		CollectionsMarshal.SetCount(list29, num);
		Span<string> span26 = CollectionsMarshal.AsSpan(list29);
		span26[0] = "1.1.1.1";
		span26[1] = "8.8.8.8";
		span26[2] = "119.29.29.29";
		span26[3] = "223.5.5.5";
		span26[4] = "localhost";
		DomainPureIPDNSAddress = list29;
		num = 7;
		List<string> list30 = new List<string>(num);
		CollectionsMarshal.SetCount(list30, num);
		Span<string> span27 = CollectionsMarshal.AsSpan(list30);
		span27[0] = "zh-Hans";
		span27[1] = "zh-Hant";
		span27[2] = "en";
		span27[3] = "fa-Ir";
		span27[4] = "fr";
		span27[5] = "ru";
		span27[6] = "hu";
		Languages = list30;
		num = 7;
		List<string> list31 = new List<string>(num);
		CollectionsMarshal.SetCount(list31, num);
		Span<string> span28 = CollectionsMarshal.AsSpan(list31);
		span28[0] = "h3";
		span28[1] = "h2";
		span28[2] = "http/1.1";
		span28[3] = "h3,h2";
		span28[4] = "h2,http/1.1";
		span28[5] = "h3,h2,http/1.1";
		span28[6] = "";
		Alpns = list31;
		num = 5;
		List<string> list32 = new List<string>(num);
		CollectionsMarshal.SetCount(list32, num);
		Span<string> span29 = CollectionsMarshal.AsSpan(list32);
		span29[0] = "debug";
		span29[1] = "info";
		span29[2] = "warning";
		span29[3] = "error";
		span29[4] = "none";
		LogLevels = list32;
		LogLevelColors = new Dictionary<string, string>
		{
			{ "debug", "#6C757D" },
			{ "info", "#2ECC71" },
			{ "warning", "#FFA500" },
			{ "error", "#E74C3C" }
		};
		num = 4;
		List<string> list33 = new List<string>(num);
		CollectionsMarshal.SetCount(list33, num);
		Span<string> span30 = CollectionsMarshal.AsSpan(list33);
		span30[0] = "tun";
		span30[1] = "socks";
		span30[2] = "socks2";
		span30[3] = "socks3";
		InboundTags = list33;
		num = 4;
		List<string> list34 = new List<string>(num);
		CollectionsMarshal.SetCount(list34, num);
		Span<string> span31 = CollectionsMarshal.AsSpan(list34);
		span31[0] = "http";
		span31[1] = "tls";
		span31[2] = "quic";
		span31[3] = "bittorrent";
		RuleProtocols = list34;
		num = 4;
		List<string> list35 = new List<string>(num);
		CollectionsMarshal.SetCount(list35, num);
		Span<string> span32 = CollectionsMarshal.AsSpan(list35);
		span32[0] = "";
		span32[1] = "tcp";
		span32[2] = "udp";
		span32[3] = "tcp,udp";
		RuleNetworks = list35;
		num = 4;
		List<string> list36 = new List<string>(num);
		CollectionsMarshal.SetCount(list36, num);
		Span<string> span33 = CollectionsMarshal.AsSpan(list36);
		span33[0] = "http";
		span33[1] = "tls";
		span33[2] = "quic";
		span33[3] = "fakedns";
		destOverrideProtocols = list36;
		num = 6;
		List<int> list37 = new List<int>(num);
		CollectionsMarshal.SetCount(list37, num);
		Span<int> span34 = CollectionsMarshal.AsSpan(list37);
		span34[0] = 1280;
		span34[1] = 1408;
		span34[2] = 1500;
		span34[3] = 4064;
		span34[4] = 9000;
		span34[5] = 65535;
		TunMtus = list37;
		num = 3;
		List<string> list38 = new List<string>(num);
		CollectionsMarshal.SetCount(list38, num);
		Span<string> span35 = CollectionsMarshal.AsSpan(list38);
		span35[0] = "gvisor";
		span35[1] = "system";
		span35[2] = "mixed";
		TunStacks = list38;
		num = 4;
		List<string> list39 = new List<string>(num);
		CollectionsMarshal.SetCount(list39, num);
		Span<string> span36 = CollectionsMarshal.AsSpan(list39);
		span36[0] = "proxy";
		span36[1] = "direct";
		span36[2] = "block";
		span36[3] = "";
		PresetMsgFilters = list39;
		num = 4;
		List<string> list40 = new List<string>(num);
		CollectionsMarshal.SetCount(list40, num);
		Span<string> span37 = CollectionsMarshal.AsSpan(list40);
		span37[0] = "h2mux";
		span37[1] = "smux";
		span37[2] = "yamux";
		span37[3] = "";
		SingboxMuxs = list40;
		num = 3;
		List<string> list41 = new List<string>(num);
		CollectionsMarshal.SetCount(list41, num);
		Span<string> span38 = CollectionsMarshal.AsSpan(list41);
		span38[0] = "cubic";
		span38[1] = "new_reno";
		span38[2] = "bbr";
		TuicCongestionControls = list41;
		num = 4;
		List<string> list42 = new List<string>(num);
		CollectionsMarshal.SetCount(list42, num);
		Span<string> span39 = CollectionsMarshal.AsSpan(list42);
		span39[0] = "bbr";
		span39[1] = "bbr2";
		span39[2] = "cubic";
		span39[3] = "reno";
		NaiveCongestionControls = list42;
		num = 4;
		List<string> list43 = new List<string>(num);
		CollectionsMarshal.SetCount(list43, num);
		Span<string> span40 = CollectionsMarshal.AsSpan(list43);
		span40[0] = "selector";
		span40[1] = "urltest";
		span40[2] = "loadbalance";
		span40[3] = "fallback";
		allowSelectType = list43;
		num = 8;
		List<string> list44 = new List<string>(num);
		CollectionsMarshal.SetCount(list44, num);
		Span<string> span41 = CollectionsMarshal.AsSpan(list44);
		span41[0] = "selector";
		span41[1] = "urltest";
		span41[2] = "direct";
		span41[3] = "reject";
		span41[4] = "compatible";
		span41[5] = "pass";
		span41[6] = "loadbalance";
		span41[7] = "fallback";
		notAllowTestType = list44;
		num = 2;
		List<string> list45 = new List<string>(num);
		CollectionsMarshal.SetCount(list45, num);
		Span<string> span42 = CollectionsMarshal.AsSpan(list45);
		span42[0] = "file";
		span42[1] = "http";
		proxyVehicleType = list45;
		CoreUrls = new Dictionary<ECoreType, string>
		{
			{
				ECoreType.v2fly,
				"v2fly/v2ray-core"
			},
			{
				ECoreType.v2fly_v5,
				"v2fly/v2ray-core"
			},
			{
				ECoreType.Xray,
				"XTLS/Xray-core"
			},
			{
				ECoreType.sing_box,
				"SagerNet/sing-box"
			},
			{
				ECoreType.mihomo,
				"MetaCubeX/mihomo"
			},
			{
				ECoreType.hysteria,
				"apernet/hysteria"
			},
			{
				ECoreType.hysteria2,
				"apernet/hysteria"
			},
			{
				ECoreType.naiveproxy,
				"klzgrad/naiveproxy"
			},
			{
				ECoreType.tuic,
				"EAimTY/tuic"
			},
			{
				ECoreType.juicity,
				"juicity/juicity"
			},
			{
				ECoreType.brook,
				"txthinking/brook"
			},
			{
				ECoreType.overtls,
				"ShadowsocksR-Live/overtls"
			},
			{
				ECoreType.shadowquic,
				"spongebob888/shadowquic"
			},
			{
				ECoreType.mieru,
				"enfein/mieru"
			},
			{
				ECoreType.app,
				"shanudhatirosh/stx-vpn-client"
			}
		};
		num = 3;
		List<string> list46 = new List<string>(num);
		CollectionsMarshal.SetCount(list46, num);
		Span<string> span43 = CollectionsMarshal.AsSpan(list46);
		span43[0] = "https://raw.githubusercontent.com/Loyalsoldier/geoip/release/geoip-only-cn-private.dat";
		span43[1] = "https://raw.githubusercontent.com/Loyalsoldier/geoip/release/Country.mmdb";
		span43[2] = "https://github.com/MetaCubeX/meta-rules-dat/releases/download/latest/geoip.metadb";
		OtherGeoUrls = list46;
		num = 5;
		List<string> list47 = new List<string>(num);
		CollectionsMarshal.SetCount(list47, num);
		Span<string> span44 = CollectionsMarshal.AsSpan(list47);
		span44[0] = "https://api.ip.sb/geoip";
		span44[1] = "https://api-ipv4.ip.sb/geoip";
		span44[2] = "https://api-ipv6.ip.sb/geoip";
		span44[3] = "https://api.ipapi.is";
		span44[4] = "";
		IPAPIUrls = list47;
		num = 14;
		List<string> list48 = new List<string>(num);
		CollectionsMarshal.SetCount(list48, num);
		Span<string> span45 = CollectionsMarshal.AsSpan(list48);
		span45[0] = "ntp:pool.ntp.org";
		span45[1] = "ntp:time.google.com";
		span45[2] = "dns:1.1.1.1";
		span45[3] = "dns:8.8.8.8";
		span45[4] = "dns:dns.google";
		span45[5] = "stun:stun.voztovoice.org";
		span45[6] = "stun:stun.cloudflare.com";
		span45[7] = "stun:stun.l.google.com:19302";
		span45[8] = "mcbe:pms.mc-complex.com";
		span45[9] = "mcbe:bedrock.opblocks.com";
		span45[10] = "mcbe:opsucht.net";
		span45[11] = "mcbe:play.craftersmc.net";
		span45[12] = "mcbe:mps.lemoncloud.net";
		span45[13] = "mcbe:bedrock.talonmc.net";
		UdpTestTargets = list48;
		num = 3;
		List<string> list49 = new List<string>(num);
		CollectionsMarshal.SetCount(list49, num);
		Span<string> span46 = CollectionsMarshal.AsSpan(list49);
		span46[0] = "proxy";
		span46[1] = "direct";
		span46[2] = "block";
		OutboundTags = list49;
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		num = 4;
		List<string> list50 = new List<string>(num);
		CollectionsMarshal.SetCount(list50, num);
		Span<string> span47 = CollectionsMarshal.AsSpan(list50);
		span47[0] = "8.8.8.8";
		span47[1] = "8.8.4.4";
		span47[2] = "2001:4860:4860::8888";
		span47[3] = "2001:4860:4860::8844";
		dictionary.Add("dns.google", list50);
		num = 4;
		List<string> list51 = new List<string>(num);
		CollectionsMarshal.SetCount(list51, num);
		Span<string> span48 = CollectionsMarshal.AsSpan(list51);
		span48[0] = "223.5.5.5";
		span48[1] = "223.6.6.6";
		span48[2] = "2400:3200::1";
		span48[3] = "2400:3200:baba::1";
		dictionary.Add("dns.alidns.com", list51);
		num = 4;
		List<string> list52 = new List<string>(num);
		CollectionsMarshal.SetCount(list52, num);
		Span<string> span49 = CollectionsMarshal.AsSpan(list52);
		span49[0] = "1.1.1.1";
		span49[1] = "1.0.0.1";
		span49[2] = "2606:4700:4700::1111";
		span49[3] = "2606:4700:4700::1001";
		dictionary.Add("one.one.one.one", list52);
		num = 4;
		List<string> list53 = new List<string>(num);
		CollectionsMarshal.SetCount(list53, num);
		Span<string> span50 = CollectionsMarshal.AsSpan(list53);
		span50[0] = "1.1.1.1";
		span50[1] = "1.0.0.1";
		span50[2] = "2606:4700:4700::1111";
		span50[3] = "2606:4700:4700::1001";
		dictionary.Add("1dot1dot1dot1.cloudflare-dns.com", list53);
		num = 4;
		List<string> list54 = new List<string>(num);
		CollectionsMarshal.SetCount(list54, num);
		Span<string> span51 = CollectionsMarshal.AsSpan(list54);
		span51[0] = "104.16.249.249";
		span51[1] = "104.16.248.249";
		span51[2] = "2606:4700::6810:f8f9";
		span51[3] = "2606:4700::6810:f9f9";
		dictionary.Add("cloudflare-dns.com", list54);
		num = 4;
		List<string> list55 = new List<string>(num);
		CollectionsMarshal.SetCount(list55, num);
		Span<string> span52 = CollectionsMarshal.AsSpan(list55);
		span52[0] = "162.159.61.8";
		span52[1] = "172.64.41.8";
		span52[2] = "2a06:98c1:52::8";
		span52[3] = "2803:f800:53::8";
		dictionary.Add("dns.cloudflare.com", list55);
		num = 2;
		List<string> list56 = new List<string>(num);
		CollectionsMarshal.SetCount(list56, num);
		Span<string> span53 = CollectionsMarshal.AsSpan(list56);
		span53[0] = "1.12.12.12";
		span53[1] = "120.53.53.53";
		dictionary.Add("dot.pub", list56);
		num = 2;
		List<string> list57 = new List<string>(num);
		CollectionsMarshal.SetCount(list57, num);
		Span<string> span54 = CollectionsMarshal.AsSpan(list57);
		span54[0] = "1.12.12.12";
		span54[1] = "120.53.53.53";
		dictionary.Add("doh.pub", list57);
		num = 4;
		List<string> list58 = new List<string>(num);
		CollectionsMarshal.SetCount(list58, num);
		Span<string> span55 = CollectionsMarshal.AsSpan(list58);
		span55[0] = "9.9.9.9";
		span55[1] = "149.112.112.112";
		span55[2] = "2620:fe::fe";
		span55[3] = "2620:fe::9";
		dictionary.Add("dns.quad9.net", list58);
		num = 4;
		List<string> list59 = new List<string>(num);
		CollectionsMarshal.SetCount(list59, num);
		Span<string> span56 = CollectionsMarshal.AsSpan(list59);
		span56[0] = "77.88.8.8";
		span56[1] = "77.88.8.1";
		span56[2] = "2a02:6b8::feed:0ff";
		span56[3] = "2a02:6b8:0:1::feed:0ff";
		dictionary.Add("dns.yandex.net", list59);
		num = 4;
		List<string> list60 = new List<string>(num);
		CollectionsMarshal.SetCount(list60, num);
		Span<string> span57 = CollectionsMarshal.AsSpan(list60);
		span57[0] = "45.11.45.11";
		span57[1] = "185.222.222.222";
		span57[2] = "2a09::";
		span57[3] = "2a11::";
		dictionary.Add("dns.sb", list60);
		num = 4;
		List<string> list61 = new List<string>(num);
		CollectionsMarshal.SetCount(list61, num);
		Span<string> span58 = CollectionsMarshal.AsSpan(list61);
		span58[0] = "208.67.220.220";
		span58[1] = "208.67.222.222";
		span58[2] = "2620:119:35::35";
		span58[3] = "2620:119:53::53";
		dictionary.Add("dns.umbrella.com", list61);
		num = 4;
		List<string> list62 = new List<string>(num);
		CollectionsMarshal.SetCount(list62, num);
		Span<string> span59 = CollectionsMarshal.AsSpan(list62);
		span59[0] = "208.67.220.220";
		span59[1] = "208.67.222.222";
		span59[2] = "2620:119:35::35";
		span59[3] = "2620:119:53::53";
		dictionary.Add("dns.sse.cisco.com", list62);
		num = 2;
		List<string> list63 = new List<string>(num);
		CollectionsMarshal.SetCount(list63, num);
		Span<string> span60 = CollectionsMarshal.AsSpan(list63);
		span60[0] = "162.159.192.1";
		span60[1] = "2606:4700:d0::a29f:c001";
		dictionary.Add("engage.cloudflareclient.com", list63);
		PredefinedHosts = dictionary;
		num = 4;
		List<string> list64 = new List<string>(num);
		CollectionsMarshal.SetCount(list64, num);
		Span<string> span61 = CollectionsMarshal.AsSpan(list64);
		span61[0] = "geoip:cn";
		span61[1] = "geoip:ir";
		span61[2] = "geoip:ru";
		span61[3] = "";
		ExpectedIPs = list64;
		num = 5;
		List<string> list65 = new List<string>(num);
		CollectionsMarshal.SetCount(list65, num);
		Span<string> span62 = CollectionsMarshal.AsSpan(list65);
		span62[0] = "rule";
		span62[1] = "direct";
		span62[2] = "unreachable";
		span62[3] = "drop";
		span62[4] = "reply";
		TunIcmpRoutingPolicies = list65;
	}
}

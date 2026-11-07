using System;
using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

[Serializable]
public class Rule4Sbox
{
	public string? outbound { get; set; }

	public string? server { get; set; }

	public bool? disable_cache { get; set; }

	public string? type { get; set; }

	public string? mode { get; set; }

	public bool? ip_is_private { get; set; }

	public string? client_subnet { get; set; }

	public int? rewrite_ttl { get; set; }

	public bool? invert { get; set; }

	public string? clash_mode { get; set; }

	public List<string>? inbound { get; set; }

	public List<string>? protocol { get; set; }

	public List<string>? network { get; set; }

	public List<int>? port { get; set; }

	public List<string>? port_range { get; set; }

	public List<string>? geosite { get; set; }

	public List<string>? domain { get; set; }

	public List<string>? domain_suffix { get; set; }

	public List<string>? domain_keyword { get; set; }

	public List<string>? domain_regex { get; set; }

	public List<string>? geoip { get; set; }

	public List<string>? ip_cidr { get; set; }

	public List<string>? source_ip_cidr { get; set; }

	public List<string>? process_name { get; set; }

	public List<string>? process_path { get; set; }

	public List<string>? rule_set { get; set; }

	public List<Rule4Sbox>? rules { get; set; }

	public string? action { get; set; }

	public string? strategy { get; set; }

	public List<string>? sniffer { get; set; }

	public string? rcode { get; set; }

	public List<int>? query_type { get; set; }

	public List<string>? answer { get; set; }

	public List<string>? ns { get; set; }

	public List<string>? extra { get; set; }

	public string? method { get; set; }

	public bool? no_drop { get; set; }

	public bool? source_ip_is_private { get; set; }

	public bool? ip_accept_any { get; set; }

	public int? source_port { get; set; }

	public List<string>? source_port_range { get; set; }

	public List<string>? network_type { get; set; }

	public bool? network_is_expensive { get; set; }

	public bool? network_is_constrained { get; set; }

	public List<string>? wifi_ssid { get; set; }

	public List<string>? wifi_bssid { get; set; }

	public bool? rule_set_ip_cidr_match_source { get; set; }

	public bool? rule_set_ip_cidr_accept_empty { get; set; }
}

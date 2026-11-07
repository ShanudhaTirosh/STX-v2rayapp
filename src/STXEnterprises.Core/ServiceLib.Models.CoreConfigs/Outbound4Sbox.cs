using System.Collections.Generic;

namespace ServiceLib.Models.CoreConfigs;

public class Outbound4Sbox : BaseServer4Sbox
{
	public string? server { get; set; }

	public int? server_port { get; set; }

	public List<string>? server_ports { get; set; }

	public string? uuid { get; set; }

	public string? security { get; set; }

	public int? alter_id { get; set; }

	public string? flow { get; set; }

	public string? hop_interval { get; set; }

	public int? up_mbps { get; set; }

	public int? down_mbps { get; set; }

	public string? auth_str { get; set; }

	public int? recv_window_conn { get; set; }

	public int? recv_window { get; set; }

	public bool? disable_mtu_discovery { get; set; }

	public int? insecure_concurrency { get; set; }

	public bool? udp_over_tcp { get; set; }

	public string? method { get; set; }

	public string? username { get; set; }

	public string? password { get; set; }

	public string? congestion_control { get; set; }

	public bool? quic { get; set; }

	public string? quic_congestion_control { get; set; }

	public string? version { get; set; }

	public string? network { get; set; }

	public string? packet_encoding { get; set; }

	public string? plugin { get; set; }

	public string? plugin_opts { get; set; }

	public List<string>? outbounds { get; set; }

	public bool? interrupt_exist_connections { get; set; }

	public int? tolerance { get; set; }
}

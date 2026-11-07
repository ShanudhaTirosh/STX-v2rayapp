namespace ServiceLib.Models.CoreConfigs;

public class Multiplex4Sbox
{
	public bool enabled { get; set; }

	public string protocol { get; set; }

	public int max_connections { get; set; }

	public bool? padding { get; set; }
}

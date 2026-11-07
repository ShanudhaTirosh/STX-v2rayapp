using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class Mux4SboxItem
{
	public string Protocol { get; set; }

	public int MaxConnections { get; set; }

	public bool? Padding { get; set; }
}

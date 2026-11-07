using System.Collections.Generic;

namespace ServiceLib.Models.Dto;

public class CmdItem
{
	public string? Cmd { get; set; }

	public List<string>? Arguments { get; set; }
}

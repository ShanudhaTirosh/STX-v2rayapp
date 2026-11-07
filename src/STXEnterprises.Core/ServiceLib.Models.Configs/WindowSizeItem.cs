using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class WindowSizeItem
{
	public string TypeName { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }
}

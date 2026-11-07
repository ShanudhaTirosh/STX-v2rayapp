using System;

namespace ServiceLib.Models.Configs;

[Serializable]
public class ColumnItem
{
	public string Name { get; set; }

	public int Width { get; set; }

	public int Index { get; set; }
}

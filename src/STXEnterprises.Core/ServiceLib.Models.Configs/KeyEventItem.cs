using System;
using ServiceLib.Enums;

namespace ServiceLib.Models.Configs;

[Serializable]
public class KeyEventItem
{
	public EGlobalHotkey EGlobalHotkey { get; set; }

	public bool Alt { get; set; }

	public bool Control { get; set; }

	public bool Shift { get; set; }

	public int? KeyCode { get; set; }
}

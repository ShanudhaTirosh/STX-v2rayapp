using System;
using System.Collections.Generic;
using ServiceLib.Enums;

namespace ServiceLib.Models.Configs;

[Serializable]
public class UIItem
{
	public bool EnableAutoAdjustMainLvColWidth { get; set; }

	public int MainGirdHeight1 { get; set; }

	public int MainGirdHeight2 { get; set; }

	public EGirdOrientation MainGirdOrientation { get; set; } = EGirdOrientation.Vertical;

	public string? ColorPrimaryName { get; set; }

	public string? CurrentTheme { get; set; }

	public string CurrentLanguage { get; set; }

	public string CurrentFontFamily { get; set; }

	public int CurrentFontSize { get; set; }

	public bool EnableDragDropSort { get; set; }

	public bool DoubleClick2Activate { get; set; }

	public bool AutoHideStartup { get; set; }

	public bool Hide2TrayWhenClose { get; set; }

	public bool MacOSShowInDock { get; set; }

	public List<ColumnItem> MainColumnItem { get; set; }

	public List<WindowSizeItem> WindowSizeItem { get; set; }
}

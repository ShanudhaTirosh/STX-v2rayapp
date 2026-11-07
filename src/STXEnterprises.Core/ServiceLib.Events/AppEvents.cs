using ServiceLib.Enums;
using ServiceLib.Models.Dto;

namespace ServiceLib.Events;

public static class AppEvents
{
	public static readonly EventChannel<Unit> ReloadRequested = new EventChannel<Unit>();

	public static readonly EventChannel<bool?> ShowHideWindowRequested = new EventChannel<bool?>();

	public static readonly EventChannel<Unit> AddServerViaScanRequested = new EventChannel<Unit>();

	public static readonly EventChannel<Unit> AddServerViaClipboardRequested = new EventChannel<Unit>();

	public static readonly EventChannel<bool> SubscriptionsUpdateRequested = new EventChannel<bool>();

	public static readonly EventChannel<bool> HasUpdateNotified = new EventChannel<bool>();

	public static readonly EventChannel<Unit> ProfilesRefreshRequested = new EventChannel<Unit>();

	public static readonly EventChannel<Unit> SubscriptionsRefreshRequested = new EventChannel<Unit>();

	public static readonly EventChannel<Unit> ProxiesReloadRequested = new EventChannel<Unit>();

	public static readonly EventChannel<ServerSpeedItem> DispatcherStatisticsRequested = new EventChannel<ServerSpeedItem>();

	public static readonly EventChannel<string> SendSnackMsgRequested = new EventChannel<string>();

	public static readonly EventChannel<string> SendMsgViewRequested = new EventChannel<string>();

	public static readonly EventChannel<Unit> AppExitRequested = new EventChannel<Unit>();

	public static readonly EventChannel<bool> ShutdownRequested = new EventChannel<bool>();

	public static readonly EventChannel<Unit> AdjustMainLvColWidthRequested = new EventChannel<Unit>();

	public static readonly EventChannel<string> SetDefaultServerRequested = new EventChannel<string>();

	public static readonly EventChannel<Unit> RoutingsMenuRefreshRequested = new EventChannel<Unit>();

	public static readonly EventChannel<Unit> TestServerRequested = new EventChannel<Unit>();

	public static readonly EventChannel<Unit> InboundDisplayRequested = new EventChannel<Unit>();

	public static readonly EventChannel<ESysProxyType> SysProxyChangeRequested = new EventChannel<ESysProxyType>();
}

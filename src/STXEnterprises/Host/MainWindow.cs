using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using STXEnterprises.Host.Bridge;
using STXEnterprises.Host.Vpn;
using ServiceLib.Common;
using ServiceLib.Manager;

namespace STXEnterprises.Host;

public sealed partial class MainWindow : Window
{
	private readonly TunnelController _tunnel = new TunnelController();

	private readonly HostSettings _settings = HostSettings.Load();

	private readonly bool _prefsApplied;

	private TrayService? _tray;

	private HostBridge? _bridge;

	private bool _quitting;

	private const string MissingUi = "<!doctype html><html><head><meta charset=\"utf-8\"></head>\n<body style=\"margin:0;height:100vh;display:grid;place-items:center;background:#000000;\n             color:#F2F3F5;font:15px/1.6 'Segoe UI',system-ui,sans-serif\">\n  <div style=\"text-align:center;max-width:30rem;padding:2rem\">\n    <div style=\"font-size:1.4rem;font-weight:650\">STX Enterprises</div>\n    <p style=\"color:#9BA1AC\">The app files are missing. Reinstall STX Enterprises.</p>\n  </div>\n</body></html>";

	public bool StartHidden { get; }

	private static bool ApplyCorePrefs(HostSettings s)
	{
		StxAdBlock.Set(s.BlockAds);
		StxTcpFastOpen.Set(s.TcpFastOpen);
		StxCertPin.Set(s.PinServerCertificates);
		return true;
	}

	public MainWindow(bool startHidden)
	{
		InitializeComponent();
		_prefsApplied = ApplyCorePrefs(_settings);
		ApplyIcon();
		ApplyWebBackground();
		StartHidden = startHidden;
		Loaded += OnLoaded;
	}

	private void ApplyIcon()
	{
		try
		{
			Icon = BitmapFrame.Create(new Uri("pack://application:,,,/assets/stxenterprises.ico"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("window icon: " + ex.Message);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ApplyWebBackground()
	{
		try
		{
			Web.DefaultBackgroundColor = Color.FromArgb(0, 0, 0);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("webview background: " + ex.Message);
		}
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	private void ApplyTitleBarTheme()
	{
		try
		{
			nint handle = new WindowInteropHelper(this).Handle;
			if (handle != IntPtr.Zero)
			{
				int value = ((_settings.Theme != "light") ? 1 : 0);
				DwmSetWindowAttribute(handle, 20, ref value, 4);
				DwmSetWindowAttribute(handle, 19, ref value, 4);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("title bar theme: " + ex.Message);
		}
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		Loaded -= OnLoaded;
		ApplyTitleBarTheme();
		_tray = new TrayService(ShowFromTray, ConnectLastAsync, _tunnel.DisconnectAsync, () =>
		{
			_ = QuitAsync();
		}, ExportDiagnostics);
		_tunnel.ConfirmCertificateChange = (string server) => Dispatcher.InvokeAsync(() =>
			MessageBox.Show("The security certificate of \"" + server + "\" is different from the one STX saw when you first connected.\n\nThis can happen when the server's certificate is renewed - or if someone is intercepting your connection.\n\nTrust the new certificate and connect?", "STX Enterprises", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes).Task;
		if (StartHidden)
		{
			Hide();
		}
		_tunnel.StatusChanged += (TunnelStatus status) =>
		{
			Dispatcher.BeginInvoke((Action)(() =>
			{
				_tray?.Update(status);
				if (status.State == TunnelState.Connected && status.ConfigId != null)
				{
					_settings.LastConfigId = status.ConfigId;
					_settings.Save();
				}
			}));
		};
		// 1. Initialize WebView2 and render the UI immediately
		await StartWebViewAsync();

		// 2. Initialize VPN Core and Tunnel asynchronously
		try
		{
			string problem = await _tunnel.InitAsync();
			await _tunnel.ApplySplitTunnelAsync(_settings.SplitMode, _settings.SplitApps);
			await _tunnel.ApplyAdBlockAsync(_settings.BlockAds);
			if (problem != null)
			{
				MessageBox.Show(problem, "STX Enterprises", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("tunnel init", ex);
		}

		CoreAssets.CoreCheck coreCheck = await CoreAssets.VerifyXrayAsync();
		if (!coreCheck.Ok)
		{
			_tray?.Notify("STX Enterprises", coreCheck.Problem ?? "The VPN core isn't usable.");
		}
		else if (_settings.AutoConnect)
		{
			string lastConfigId = _settings.LastConfigId;
			if (lastConfigId != null && lastConfigId.Length > 0)
			{
				ConnectLastAsync();
			}
		}
	}

	private async Task StartWebViewAsync()
	{
		string text = HostPaths.WebViewUserData;
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(AppContext.BaseDirectory, "webview2");
		string browserExecutableFolder = ((Directory.Exists(text2) && File.Exists(Path.Combine(text2, "msedgewebview2.exe"))) ? text2 : null);
		try
		{
			CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(browserExecutableFolder, text);
			await Web.EnsureCoreWebView2Async(environment);
		}
		catch (Exception ex)
		{
			Logging.SaveLog("webview2", ex);
			MessageBox.Show("STX Enterprises needs the Microsoft Edge WebView2 runtime, which is missing or damaged on this computer.\n\nWindows 11 has it built in. On Windows 10 you may need it installed once — reinstall STX Enterprises and the installer will handle it, or get it from go.microsoft.com/fwlink/p/?LinkId=2124703", "STX Enterprises", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		CoreWebView2 coreWebView = Web.CoreWebView2;
		Harden(coreWebView);
		coreWebView.NavigationCompleted += (object? _, CoreWebView2NavigationCompletedEventArgs e) =>
		{
			if (!e.IsSuccess)
			{
				Logging.SaveLog($"webview navigation failed: {e.WebErrorStatus}");
			}
		};
		_bridge = new HostBridge(new WebView2HostView(coreWebView), _tunnel, _settings, QuitAsync, () =>
		{
			WindowState = WindowState.Minimized;
		});
		string text3 = Path.Combine(AppContext.BaseDirectory, "wwwroot");
		if (!Directory.Exists(text3) || !File.Exists(Path.Combine(text3, "index.html")))
		{
			text3 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
		}
		if (Directory.Exists(text3) && File.Exists(Path.Combine(text3, "index.html")))
		{
			coreWebView.SetVirtualHostNameToFolderMapping("stxenterprises.local", text3, CoreWebView2HostResourceAccessKind.Allow);
			coreWebView.SetVirtualHostNameToFolderMapping("stx.local", text3, CoreWebView2HostResourceAccessKind.Allow);
			coreWebView.SetVirtualHostNameToFolderMapping("pxn.local", text3, CoreWebView2HostResourceAccessKind.Allow);
			coreWebView.Navigate("https://stxenterprises.local/index.html");
		}
		else
		{
			coreWebView.NavigateToString("<!doctype html><html><head><meta charset=\"utf-8\"></head>\n<body style=\"margin:0;height:100vh;display:grid;place-items:center;background:#000000;\n             color:#F2F3F5;font:15px/1.6 'Segoe UI',system-ui,sans-serif\">\n  <div style=\"text-align:center;max-width:30rem;padding:2rem\">\n    <div style=\"font-size:1.4rem;font-weight:650\">STX Enterprises</div>\n    <p style=\"color:#9BA1AC\">The app files are missing. Reinstall STX Enterprises.</p>\n  </div>\n</body></html>");
		}
	}

	private static void Harden(CoreWebView2 core)
	{
		CoreWebView2Settings settings = core.Settings;
#if DEBUG
		settings.AreDefaultContextMenusEnabled = true;
		settings.AreBrowserAcceleratorKeysEnabled = true;
		settings.AreDevToolsEnabled = true;
#else
		// Release builds: no DevTools, no right-click menu, no F5/Ctrl+... browser shortcuts. The page drives a
		// privileged bridge (connect, system proxy), so it should not be inspectable or scriptable by the user's tabs.
		settings.AreDefaultContextMenusEnabled = false;
		settings.AreBrowserAcceleratorKeysEnabled = false;
		settings.AreDevToolsEnabled = false;
#endif
		settings.IsZoomControlEnabled = false;
		settings.IsStatusBarEnabled = false;
		settings.IsPasswordAutosaveEnabled = false;
		settings.IsGeneralAutofillEnabled = false;
		// The window may only show our bundled pages. Anything else is handed to the default browser instead of
		// being loaded next to the host bridge.
		core.NavigationStarting += (object? _, CoreWebView2NavigationStartingEventArgs e) =>
		{
			if (!OriginPolicy.IsAllowedNavigation(e.Uri))
			{
				e.Cancel = true;
				if (Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? target) && (target.Scheme == Uri.UriSchemeHttp || target.Scheme == Uri.UriSchemeHttps))
				{
					Process.Start(new ProcessStartInfo(target.AbsoluteUri)
					{
						UseShellExecute = true
					});
				}
			}
		};
		core.PermissionRequested += (object? _, CoreWebView2PermissionRequestedEventArgs e) =>
		{
			e.State = CoreWebView2PermissionState.Deny;
		};
		core.DownloadStarting += (object? _, CoreWebView2DownloadStartingEventArgs e) =>
		{
			e.Cancel = true;
		};
		core.NewWindowRequested += (object? _, CoreWebView2NewWindowRequestedEventArgs e) =>
		{
			e.Handled = true;
			if (Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
			{
				Process.Start(new ProcessStartInfo(result.AbsoluteUri)
				{
					UseShellExecute = true
				});
			}
		};
	}

	private async Task ConnectLastAsync()
	{
		string text = _settings.LastConfigId;
		if (text.IsNullOrEmpty())
		{
			text = (await _tunnel.ListConfigsAsync()).FirstOrDefault()?.Id;
		}
		if (text.IsNullOrEmpty())
		{
			_tray?.Notify("STX Enterprises", "No configs yet. Open the app and sign in.");
			return;
		}
		string text2 = await _tunnel.ConnectAsync(text);
		if (text2 != null)
		{
			_tray?.Notify("STX Enterprises", text2);
		}
	}

	/// <summary>Called when Windows signs out / shuts down or the process exits: put the proxy back and stop the cores.</summary>
	public void EmergencyShutdown()
	{
		_quitting = true;
		_tunnel.EmergencyCleanup();
		_tray?.Dispose();
	}

	private void ExportDiagnostics()
	{
		try
		{
			Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
			{
				FileName = $"STX-diagnostics-{DateTime.Now:yyyyMMdd-HHmm}.zip",
				Filter = "Zip file (*.zip)|*.zip",
				Title = "Save diagnostics"
			};
			if (dialog.ShowDialog() != true)
			{
				return;
			}
			string version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";
			CoreAssets.CoreCheck core = CoreAssets.VerifyXrayAsync().GetAwaiter().GetResult();
			CrashReporter.CreateDiagnosticsBundle(dialog.FileName, version, $"xray: {core.Version} ok={core.Ok}\r\nsing-box present: {CoreAssets.HasSingBox}");
			Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + dialog.FileName + "\"")
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Logging.SaveLog("export diagnostics", ex);
			MessageBox.Show("Couldn't create the diagnostics file: " + ex.Message, "STX Enterprises", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	public void ShowFromTray()
	{
		Dispatcher.Invoke(() =>
		{
			Show();
			WindowState = WindowState.Normal;
			Activate();
			Topmost = true;
			Topmost = false;
		});
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!_quitting && _settings.MinimiseToTray)
		{
			e.Cancel = true;
			Hide();
			_tray?.Notify("STX Enterprises", "Still running. Your VPN stays connected.");
		}
		else
		{
			base.OnClosing(e);
		}
	}

	private async Task QuitAsync()
	{
		if (!_quitting)
		{
			_quitting = true;
			try
			{
				await _tunnel.DisconnectAsync();
			}
			catch (Exception ex)
			{
				Logging.SaveLog("quit disconnect", ex);
			}
			_tray?.Dispose();
			Dispatcher.Invoke(() =>
			{
				Application.Current.Shutdown();
			});
		}
	}
}

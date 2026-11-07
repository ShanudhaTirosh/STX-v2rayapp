using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using STXEnterprises.Host.Vpn;

namespace STXEnterprises.Host;

public sealed class TrayService : IDisposable
{
	private readonly NotifyIcon _icon;

	private readonly ToolStripMenuItem _toggle;

	private readonly ToolStripMenuItem _statusLine;

	private readonly Func<Task> _connect;

	private readonly Func<Task> _disconnect;

	private TunnelState _state;

	public TrayService(Action show, Func<Task> connect, Func<Task> disconnect, Action quit)
	{
		TrayService trayService = this;
		_connect = connect;
		_disconnect = disconnect;
		_statusLine = new ToolStripMenuItem("Not connected")
		{
			Enabled = false
		};
		_toggle = new ToolStripMenuItem("Connect", null, async (object? _, EventArgs _) =>
		{
			await trayService.ToggleAsync();
		});
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip
		{
			Items = 
			{
				(ToolStripItem)_statusLine,
				(ToolStripItem)new ToolStripSeparator(),
				(ToolStripItem)_toggle,
				(ToolStripItem)new ToolStripMenuItem("Open STX Enterprises", null, (object? _, EventArgs _) =>
				{
					show();
				}),
				(ToolStripItem)new ToolStripSeparator(),
				(ToolStripItem)new ToolStripMenuItem("Quit", null, (object? _, EventArgs _) =>
				{
					quit();
				})
			}
		};
		_icon = new NotifyIcon
		{
			Icon = LoadIcon(),
			Text = "STX Enterprises",
			Visible = true,
			ContextMenuStrip = contextMenuStrip
		};
		_icon.DoubleClick += (object? _, EventArgs _) =>
		{
			show();
		};
	}

	private async Task ToggleAsync()
	{
		if (_state == TunnelState.Connected)
		{
			await _disconnect();
		}
		else if (_state == TunnelState.Disconnected)
		{
			await _connect();
		}
	}

	public void Update(TunnelStatus status)
	{
		_state = status.State;
		string text;
		switch (status.State)
		{
		case TunnelState.Connected:
		{
			string configName = status.ConfigName;
			text = ((configName != null && configName.Length > 0) ? ("Connected — " + status.ConfigName) : "Connected");
			break;
		}
		case TunnelState.Connecting:
			text = "Connecting…";
			break;
		default:
			text = "Not connected";
			break;
		}
		string text2 = text;
		_statusLine.Text = text2;
		_toggle.Text = ((status.State == TunnelState.Connected) ? "Disconnect" : "Connect");
		_toggle.Enabled = status.State != TunnelState.Connecting;
		string text3 = "STX Client — " + text2;
		_icon.Text = ((text3.Length > 63) ? (text3.Substring(0, 60) + "…") : text3);
	}

	public void Notify(string title, string message)
	{
		_icon.BalloonTipTitle = title;
		_icon.BalloonTipText = message;
		_icon.ShowBalloonTip(4000);
	}

	private static Icon LoadIcon()
	{
		try
		{
			string text = Path.Combine(AppContext.BaseDirectory, "Assets", "stxenterprises.ico");
			if (File.Exists(text))
			{
				return new Icon(text);
			}
			Stream stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/stxenterprises.ico"))?.Stream;
			if (stream != null)
			{
				return new Icon(stream);
			}
		}
		catch
		{
		}
		return SystemIcons.Shield;
	}

	public void Dispose()
	{
		_icon.Visible = false;
		_icon.Dispose();
	}
}

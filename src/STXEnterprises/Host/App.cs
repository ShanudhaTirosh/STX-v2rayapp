using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ServiceLib.Common;

namespace STXEnterprises.Host;

public partial class App : Application
{
	private static EventWaitHandle? _alreadyRunning;

	private static bool _windowUp;

	private void Die(string what, Exception ex)
	{
		try
		{
			MessageBox.Show(what + "\n\n" + Explain(ex) + "\n\nSend this to STX Enterprises support and we will sort it.", "STX Enterprises", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		catch
		{
		}
		Shutdown(1);
	}

	public App()
	{
		DispatcherUnhandledException += (object _, DispatcherUnhandledExceptionEventArgs e) =>
		{
			Logging.SaveLog("dispatcher", e.Exception);
			if (_windowUp)
			{
				e.Handled = true;
			}
			else
			{
				Die("STX Enterprises couldn't start.", e.Exception);
			}
		};
		AppDomain.CurrentDomain.UnhandledException += (object _, UnhandledExceptionEventArgs e) =>
		{
			if (e.ExceptionObject is Exception ex)
			{
				Logging.SaveLog("domain", ex);
			}
		};
		TaskScheduler.UnobservedTaskException += (object? _, UnobservedTaskExceptionEventArgs e) =>
		{
			Logging.SaveLog("task", e.Exception);
			e.SetObserved();
		};
	}

	private static string Explain(Exception ex)
	{
		List<string> list = new List<string>();
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			string item = ex2.GetType().Name + ": " + ex2.Message;
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		list.Reverse();
		return string.Join("\n\n", list);
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		try
		{
			SQLitePCL.Batteries_V2.Init();
		}
		catch (Exception ex)
		{
			Logging.SaveLog("sqlitepcl init: " + ex.Message);
		}

		_alreadyRunning = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "STXEnterprises.SingleInstance", out var createdNew);
		if (!createdNew)
		{
			_alreadyRunning.Set();
			Shutdown();
			return;
		}
		base.OnStartup(e);
		MainWindow window;
		try
		{
			window = new MainWindow(MemoryExtensions.Contains(e.Args, "--tray"));
			MainWindow = window;
			window.Show();
		}
		catch (Exception ex)
		{
			Die("STX Enterprises couldn't open its window.", ex);
			return;
		}
		_windowUp = window.IsVisible;
		Logging.SaveLog($"startup: window shown={_windowUp} hidden={window.StartHidden}");
		Task.Run(() =>
		{
			while (_alreadyRunning.WaitOne())
			{
				Dispatcher.BeginInvoke((Action)(() =>
				{
					window.ShowFromTray();
				}));
			}
		});
	}
}

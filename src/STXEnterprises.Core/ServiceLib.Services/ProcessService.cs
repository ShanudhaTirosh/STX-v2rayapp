using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ServiceLib.Common;

namespace ServiceLib.Services;

public class ProcessService : IDisposable
{
	private readonly Process _process;

	private readonly Func<bool, string, Task>? _updateFunc;

	private bool _isDisposed;

	public int Id => _process.Id;

	public nint Handle => _process.Handle;

	public bool HasExited => _process.HasExited;

	public ProcessService(string fileName, string arguments, string workingDirectory, bool displayLog, bool redirectInput, Dictionary<string, string>? environmentVars, Func<bool, string, Task>? updateFunc)
	{
		_updateFunc = updateFunc;
		_process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				RedirectStandardInput = redirectInput,
				RedirectStandardOutput = displayLog,
				RedirectStandardError = displayLog,
				CreateNoWindow = true,
				StandardOutputEncoding = (displayLog ? Encoding.UTF8 : null),
				StandardErrorEncoding = (displayLog ? Encoding.UTF8 : null)
			},
			EnableRaisingEvents = true
		};
		if (environmentVars != null)
		{
			foreach (KeyValuePair<string, string> environmentVar in environmentVars)
			{
				_process.StartInfo.Environment[environmentVar.Key] = environmentVar.Value;
			}
		}
		if (displayLog)
		{
			RegisterEventHandlers();
		}
	}

	public async Task StartAsync(string pwd = null)
	{
		_process.Start();
		if (_process.StartInfo.RedirectStandardOutput)
		{
			_process.BeginOutputReadLine();
			_process.BeginErrorReadLine();
		}
		if (_process.StartInfo.RedirectStandardInput)
		{
			await Task.Delay(10);
			await _process.StandardInput.WriteLineAsync(pwd);
		}
	}

	public async Task StopAsync()
	{
		if (_process.HasExited)
		{
			return;
		}
		try
		{
			if (_process.StartInfo.RedirectStandardOutput)
			{
				try
				{
					_process.CancelOutputRead();
				}
				catch
				{
				}
				try
				{
					_process.CancelErrorRead();
				}
				catch
				{
				}
			}
			try
			{
				if (Utils.IsNonWindows())
				{
					_process.Kill(entireProcessTree: true);
				}
			}
			catch
			{
			}
			try
			{
				_process.Kill();
			}
			catch
			{
			}
			await Task.Delay(100);
		}
		catch (Exception ex)
		{
			await (_updateFunc?.Invoke(arg1: true, ex.Message));
		}
	}

	private void RegisterEventHandlers()
	{
		_process.OutputDataReceived += dataHandler;
		_process.ErrorDataReceived += dataHandler;
		_process.Exited += (object? s, EventArgs e) =>
		{
			try
			{
				_process.OutputDataReceived -= dataHandler;
				_process.ErrorDataReceived -= dataHandler;
			}
			catch
			{
			}
		};
		void dataHandler(object sender, DataReceivedEventArgs e)
		{
			if (e.Data.IsNotEmpty())
			{
				_updateFunc?.Invoke(arg1: false, e.Data + Environment.NewLine);
			}
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}
		try
		{
			if (!_process.HasExited)
			{
				try
				{
					_process.CancelOutputRead();
				}
				catch
				{
				}
				try
				{
					_process.CancelErrorRead();
				}
				catch
				{
				}
				_process.Kill();
			}
			_process.Dispose();
		}
		catch (Exception ex)
		{
			_updateFunc?.Invoke(arg1: true, ex.Message);
		}
		_isDisposed = true;
		GC.SuppressFinalize(this);
	}
}

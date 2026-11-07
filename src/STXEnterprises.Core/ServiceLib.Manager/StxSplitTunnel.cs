using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ServiceLib.Manager;

public static class StxSplitTunnel
{
	private static readonly Lock _gate = new Lock();

	private static List<string> _processes = new List<string>();

	public static ESplitTunnelMode Mode { get; private set; } = ESplitTunnelMode.All;

	public static List<string> Processes
	{
		get
		{
			using (_gate.EnterScope())
			{
				return _processes.ToList();
			}
		}
	}

	public static bool IsActive
	{
		get
		{
			using (_gate.EnterScope())
			{
				return Mode != ESplitTunnelMode.All && _processes.Count > 0;
			}
		}
	}

	public static void Set(ESplitTunnelMode mode, IEnumerable<string>? processes)
	{
		using (_gate.EnterScope())
		{
			Mode = mode;
			_processes = (from p in processes ?? Array.Empty<string>()
				select p.Trim() into p
				where p.Length > 0
				select (!p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) ? (p + ".exe") : p).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
	}
}

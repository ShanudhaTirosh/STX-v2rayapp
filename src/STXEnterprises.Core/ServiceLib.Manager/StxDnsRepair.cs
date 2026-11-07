using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Models.Configs;

namespace ServiceLib.Manager;

public static class StxDnsRepair
{
	private static readonly string[] Inherited = new string[3] { "119.29.29.29", "223.5.5.5", "119.29.29.29,223.5.5.5,https://doh.pub/dns-query" };

	public static async Task ApplyAsync(Config config)
	{
		try
		{
			SimpleDNSItem simpleDNSItem = config.SimpleDNSItem;
			if (simpleDNSItem != null)
			{
				string text = Global.DomainDirectDNSAddress.First();
				string text2 = Global.DomainPureIPDNSAddress.First();
				bool flag = false;
				if (simpleDNSItem.DirectDNS.IsNullOrEmpty() || MemoryExtensions.Contains(Inherited, simpleDNSItem.DirectDNS))
				{
					Logging.SaveLog("dns: direct resolver " + (simpleDNSItem.DirectDNS ?? "(unset)") + " -> " + text);
					simpleDNSItem.DirectDNS = text;
					flag = true;
				}
				if (simpleDNSItem.BootstrapDNS.IsNullOrEmpty() || MemoryExtensions.Contains(Inherited, simpleDNSItem.BootstrapDNS))
				{
					Logging.SaveLog("dns: bootstrap resolver " + (simpleDNSItem.BootstrapDNS ?? "(unset)") + " -> " + text2);
					simpleDNSItem.BootstrapDNS = text2;
					flag = true;
				}
				if (flag)
				{
					await ConfigHandler.SaveConfig(config);
				}
			}
		}
		catch (Exception ex)
		{
			Logging.SaveLog("dns repair", ex);
		}
	}
}

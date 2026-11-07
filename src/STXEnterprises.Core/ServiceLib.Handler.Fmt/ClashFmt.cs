using ServiceLib.Enums;
using ServiceLib.Models.Entities;

namespace ServiceLib.Handler.Fmt;

public class ClashFmt : BaseFmt
{
	public static ProfileItem? ResolveFull(string strData, string? subRemarks)
	{
		if (BaseFmt.Contains(strData, "rules", "-port", "proxies"))
		{
			string address = BaseFmt.WriteAllText(strData, "yaml");
			return new ProfileItem
			{
				CoreType = ECoreType.mihomo,
				Address = address,
				Remarks = (subRemarks ?? "clash_custom")
			};
		}
		return null;
	}
}

namespace ServiceLib.Handler.Fmt;

public class HtmlPageFmt : BaseFmt
{
	public static bool IsHtmlPage(string strData)
	{
		return BaseFmt.Contains(strData, "<html", "<!doctype html", "<head");
	}
}

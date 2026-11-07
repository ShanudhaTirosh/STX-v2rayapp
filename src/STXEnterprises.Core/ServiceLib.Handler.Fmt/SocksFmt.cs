using System;
using System.Linq;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models.Entities;
using ServiceLib.Resx;

namespace ServiceLib.Handler.Fmt;

public class SocksFmt : BaseFmt
{
	public static ProfileItem? Resolve(string str, out string msg)
	{
		msg = ResUI.ConfigurationFormatIncorrect;
		ProfileItem profileItem = ResolveSocksNew(str) ?? ResolveSocks(str);
		if (profileItem == null)
		{
			return null;
		}
		if (profileItem.Address.Length == 0 || profileItem.Port == 0)
		{
			return null;
		}
		profileItem.ConfigType = EConfigType.SOCKS;
		return profileItem;
	}

	public static string? ToUri(ProfileItem? item)
	{
		if (item == null)
		{
			return null;
		}
		string remark = string.Empty;
		if (item.Remarks.IsNotEmpty())
		{
			remark = "#" + Utils.UrlEncode(item.Remarks);
		}
		string userInfo = Utils.Base64Encode(item.Username + ":" + item.Password, removePadding: true);
		return BaseFmt.ToUri(EConfigType.SOCKS, item.Address, item.Port, userInfo, null, remark);
	}

	private static ProfileItem? ResolveSocks(string result)
	{
		ProfileItem profileItem = new ProfileItem
		{
			ConfigType = EConfigType.SOCKS
		};
		result = result.Substring(Global.ProtocolShares[EConfigType.SOCKS].Length);
		int num = result.IndexOf('#');
		checked
		{
			if (num > 0)
			{
				try
				{
					profileItem.Remarks = Utils.UrlDecode(result.Substring(num + 1));
				}
				catch
				{
				}
				result = result.Substring(0, num);
			}
			if (result.IndexOf('@') <= 0)
			{
				result = Utils.Base64Decode(result);
			}
			string[] array = result.Split('@');
			if (array.Length != 2)
			{
				return null;
			}
			string[] array2 = array.First().Split(':');
			int num2 = array.Last().LastIndexOf(":");
			if (array2.Length != 2 || num2 < 0)
			{
				return null;
			}
			profileItem.Address = array[1].Substring(0, num2);
			profileItem.Port = array[1].Substring(num2 + 1).ToInt();
			profileItem.Username = array2.First();
			profileItem.Password = array2[1];
			return profileItem;
		}
	}

	private static ProfileItem? ResolveSocksNew(string result)
	{
		Uri uri = Utils.TryUri(result);
		if (uri == null)
		{
			return null;
		}
		ProfileItem profileItem = new ProfileItem
		{
			Remarks = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
			Address = uri.IdnHost,
			Port = uri.Port
		};
		string text = Utils.UrlDecode(uri.UserInfo);
		if (text.IsNotEmpty())
		{
			string[] array = (text.Contains(':') ? text.Split(":", 2) : Utils.Base64Decode(text).Split(":", 2));
			if (array.Length == 2)
			{
				profileItem.Username = array.First();
				profileItem.Password = array.Last();
			}
		}
		return profileItem;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ServiceLib.Common;
using ServiceLib.Events;
using ServiceLib.Handler.Builder;

namespace ServiceLib.Manager;

public class NoticeManager
{
	private static readonly Lazy<NoticeManager> _instance = new Lazy<NoticeManager>(() => new NoticeManager());

	public static NoticeManager Instance => _instance.Value;

	public void Enqueue(string? content)
	{
		if (!content.IsNullOrEmpty())
		{
			AppEvents.SendSnackMsgRequested.Publish(content);
		}
	}

	public void SendMessage(string? content)
	{
		if (!content.IsNullOrEmpty())
		{
			AppEvents.SendMsgViewRequested.Publish(content);
		}
	}

	public void SendMessageEx(string? content)
	{
		if (!content.IsNullOrEmpty())
		{
			content = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {content}";
			SendMessage(content);
		}
	}

	public void SendMessageAndEnqueue(string? msg)
	{
		Enqueue(msg);
		SendMessage(msg);
	}

	public bool NotifyValidatorResult(NodeValidatorResult validatorResult)
	{
		List<string> errors = validatorResult.Errors;
		List<string> warnings = validatorResult.Warnings;
		int num = 0;
		string[] array = new string[errors.Count + warnings.Count];
		Span<string> span = CollectionsMarshal.AsSpan(errors);
		span.CopyTo(new Span<string>(array).Slice(num, span.Length));
		num += span.Length;
		Span<string> span2 = CollectionsMarshal.AsSpan(warnings);
		span2.CopyTo(new Span<string>(array).Slice(num, span2.Length));
		num += span2.Length;
		List<string> list = new List<string>(new _003C_003Ez__ReadOnlyArray<string>(array));
		if (list.Count == 0)
		{
			return false;
		}
		foreach (string item in list)
		{
			SendMessage(item);
		}
		Enqueue(Utils.List2String(list.Take(10).ToList(), wrap: true));
		return true;
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ServiceLib.Handler.Builder;

public record CoreConfigContextBuilderAllResult(CoreConfigContextBuilderResult MainResult, CoreConfigContextBuilderResult? PreSocksResult)
{
	public bool Success
	{
		get
		{
			if (MainResult.Success)
			{
				return PreSocksResult?.Success ?? true;
			}
			return false;
		}
	}

	public NodeValidatorResult CombinedValidatorResult
	{
		get
		{
			List<string> errors = MainResult.ValidatorResult.Errors;
			List<string> list = PreSocksResult?.ValidatorResult.Errors ?? new List<string>();
			int num = errors.Count + list.Count;
			List<string> list2 = new List<string>(num);
			CollectionsMarshal.SetCount(list2, num);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int num2 = 0;
			Span<string> span2 = CollectionsMarshal.AsSpan(errors);
			span2.CopyTo(span.Slice(num2, span2.Length));
			num2 += span2.Length;
			Span<string> span3 = CollectionsMarshal.AsSpan(list);
			span3.CopyTo(span.Slice(num2, span3.Length));
			num2 += span3.Length;
			List<string> errors2 = list2;
			List<string> warnings = MainResult.ValidatorResult.Warnings;
			list2 = PreSocksResult?.ValidatorResult.Warnings ?? new List<string>();
			num2 = warnings.Count + list2.Count;
			list = new List<string>(num2);
			CollectionsMarshal.SetCount(list, num2);
			span3 = CollectionsMarshal.AsSpan(list);
			num = 0;
			span2 = CollectionsMarshal.AsSpan(warnings);
			span2.CopyTo(span3.Slice(num, span2.Length));
			num += span2.Length;
			Span<string> span4 = CollectionsMarshal.AsSpan(list2);
			span4.CopyTo(span3.Slice(num, span4.Length));
			num += span4.Length;
			return new NodeValidatorResult(errors2, list);
		}
	}
}

using System.Collections.Generic;

namespace ServiceLib.Handler.Builder;

public record NodeValidatorResult(List<string> Errors, List<string> Warnings)
{
	public bool Success => Errors.Count == 0;

	public static NodeValidatorResult Empty()
	{
		return new NodeValidatorResult(new List<string>(), new List<string>());
	}
}

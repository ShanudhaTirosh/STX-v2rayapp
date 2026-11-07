using ServiceLib.Models.CoreConfigs;

namespace ServiceLib.Handler.Builder;

public record CoreConfigContextBuilderResult(CoreConfigContext Context, NodeValidatorResult ValidatorResult)
{
	public CoreConfigContext Context { get; set; } = Context;
	public bool Success => ValidatorResult.Success;
}

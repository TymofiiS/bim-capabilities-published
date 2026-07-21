using ExecutionContextContract = BIMCapabilities.Contracts.Execution.ExecutionContext;

namespace BIMCapabilities.Runtime.Context;

/// <summary>
/// In-memory execution context holder for runtime composition.
/// </summary>
public sealed class RuntimeContextSkeleton : IRuntimeContext
{
    public ExecutionContextContract? Current { get; private set; }

    public void SetContext(ExecutionContextContract context)
    {
        ArgumentGuard.ThrowIfNull(context);
        Current = context;
    }
}

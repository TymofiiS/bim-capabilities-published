using ExecutionContextContract = BIMCapabilities.Contracts.Execution.ExecutionContext;

namespace BIMCapabilities.Runtime.Context;

/// <summary>
/// Manages the active execution context for a runtime session.
/// </summary>
public interface IRuntimeContext
{
    ExecutionContextContract? Current { get; }

    void SetContext(ExecutionContextContract context);
}

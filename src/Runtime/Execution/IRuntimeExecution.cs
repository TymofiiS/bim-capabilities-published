using BIMCapabilities.Contracts.Execution;
using ExecutionContextContract = BIMCapabilities.Contracts.Execution.ExecutionContext;

namespace BIMCapabilities.Runtime.Execution;

/// <summary>
/// Composes execution plans and results without performing engine execution.
/// </summary>
public interface IRuntimeExecution
{
    ExecutionPlan CreatePlan(ExecutionContextContract context);

    ExecutionResult ComposeResult(ExecutionContextContract context, ExecutionPlan plan);
}

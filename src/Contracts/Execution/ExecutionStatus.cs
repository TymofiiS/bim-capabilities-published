namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Indicates the lifecycle state of an execution plan or result.
/// </summary>
public enum ExecutionStatus
{
    Pending,

    Running,

    Completed,

    Failed,

    Skipped,

    Cancelled
}

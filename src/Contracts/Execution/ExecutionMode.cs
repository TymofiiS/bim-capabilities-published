namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Defines how a BIMRule workflow should be executed.
/// </summary>
public enum ExecutionMode
{
    Validation,

    Fix,

    Review
}

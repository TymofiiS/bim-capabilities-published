namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Lifecycle status for a transaction.
/// </summary>
public enum TransactionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    RolledBack,
    Cancelled
}

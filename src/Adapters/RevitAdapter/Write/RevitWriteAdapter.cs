using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Composes the Revit Adapter write layer without performing Revit API writes.
/// </summary>
public sealed class RevitWriteAdapter : IRevitWriteAdapter
{
    public RevitWriteAdapter()
    {
        WriteRequests = new WriteRequestExecutorSkeleton();
        Transactions = new TransactionExecutorSkeleton();
        Diagnostics = new WriteDiagnosticsSkeleton();
        Results = new WriteResultCollectorSkeleton();
    }

    public IWriteRequestExecutor WriteRequests { get; }

    public ITransactionExecutor Transactions { get; }

    public IWriteDiagnostics Diagnostics { get; }

    public IWriteResultCollector Results { get; }
}

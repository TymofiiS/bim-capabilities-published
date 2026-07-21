namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Composition contract for the Revit Adapter write layer.
/// </summary>
public interface IRevitWriteAdapter
{
    IWriteRequestExecutor WriteRequests { get; }

    ITransactionExecutor Transactions { get; }

    IWriteDiagnostics Diagnostics { get; }

    IWriteResultCollector Results { get; }
}

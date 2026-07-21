using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Skeleton diagnostics collector for write layer composition.
/// </summary>
public sealed class WriteDiagnosticsSkeleton : IWriteDiagnostics
{
    private readonly List<WriteRequestDiagnostic> _writeDiagnostics = [];
    private readonly List<TransactionDiagnostic> _transactionDiagnostics = [];

    public IReadOnlyList<WriteRequestDiagnostic> GetWriteDiagnostics()
    {
        return _writeDiagnostics.ToArray();
    }

    public IReadOnlyList<TransactionDiagnostic> GetTransactionDiagnostics()
    {
        return _transactionDiagnostics.ToArray();
    }

    public void Record(WriteRequestDiagnostic diagnostic)
    {
        ArgumentGuard.ThrowIfNull(diagnostic);
        _writeDiagnostics.Add(diagnostic);
    }

    public void Record(TransactionDiagnostic diagnostic)
    {
        ArgumentGuard.ThrowIfNull(diagnostic);
        _transactionDiagnostics.Add(diagnostic);
    }

    public void Clear()
    {
        _writeDiagnostics.Clear();
        _transactionDiagnostics.Clear();
    }
}

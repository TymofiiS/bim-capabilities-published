namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Collects write and transaction diagnostics during adapter write composition.
/// </summary>
public interface IWriteDiagnostics
{
    IReadOnlyList<WriteRequestDiagnostic> GetWriteDiagnostics();

    IReadOnlyList<TransactionDiagnostic> GetTransactionDiagnostics();

    void Record(WriteRequestDiagnostic diagnostic);

    void Record(TransactionDiagnostic diagnostic);

    void Clear();
}

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Collects write and transaction results during adapter write composition.
/// </summary>
public interface IWriteResultCollector
{
    IReadOnlyList<WriteRequestResult> GetWriteResults();

    IReadOnlyList<TransactionResult> GetTransactionResults();

    void Collect(WriteRequestResult result);

    void Collect(TransactionResult result);

    void Clear();
}

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Executes transactions grouping write requests for the Revit Adapter write layer.
/// </summary>
public interface ITransactionExecutor
{
    TransactionResult Execute(TransactionRequest request);

    TransactionResult ExecuteBatch(TransactionBatch batch);
}

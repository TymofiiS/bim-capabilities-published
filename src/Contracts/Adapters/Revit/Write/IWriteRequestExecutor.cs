using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Executes individual write requests for the Revit Adapter write layer.
/// </summary>
public interface IWriteRequestExecutor
{
    WriteRequestResult Execute(WriteRequest request);

    WriteRequestResult ExecuteBatch(WriteRequestBatch batch);
}

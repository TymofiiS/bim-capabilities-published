namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Scope classification for a transaction.
/// </summary>
public enum TransactionScopeKind
{
    SingleObject,
    MultipleObjects,
    ModelScope,
    Custom
}

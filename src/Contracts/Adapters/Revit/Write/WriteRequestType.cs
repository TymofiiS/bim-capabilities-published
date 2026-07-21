namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Supported deterministic write operations requested by engines.
/// </summary>
public enum WriteRequestType
{
    ParameterCreate,
    ParameterUpdate,
    ParameterDelete,
    RenameFamily,
    RenameType,
    Custom
}

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Composition contract for the operational Revit Adapter read layer.
/// </summary>
public interface IRevitAdapter : IRevitReadAdapter
{
    RevitAdapterReadResult Read(RevitAdapterReadContext context);
}

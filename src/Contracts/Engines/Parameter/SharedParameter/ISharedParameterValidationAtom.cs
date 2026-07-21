using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

/// <summary>
/// Contract for the Parameter Engine shared parameter validation atom.
/// </summary>
public interface ISharedParameterValidationAtom
{
    string AtomId { get; }

    SharedParameterValidationResult Validate(SharedParameterValidationRequest request);
}

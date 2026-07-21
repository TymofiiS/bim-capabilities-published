using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.Existence;

/// <summary>
/// Contract for the Parameter Engine parameter existence validation atom.
/// </summary>
public interface IParameterExistenceValidationAtom
{
    string AtomId { get; }

    ParameterExistenceResult Validate(ParameterExistenceRequest request);
}

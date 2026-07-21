using ExistenceContracts = BIMCapabilities.Contracts.Engines.Parameter.Existence;

namespace BIMCapabilities.Engines.Parameter.Atoms.Existence;

/// <summary>
/// Validates that required parameters exist on target objects.
/// </summary>
public sealed class ParameterExistenceValidationAtom : ExistenceContracts.IParameterExistenceValidationAtom
{
    public const string ValidationAtomId = "parameter.validation.existence";

    public string AtomId => ValidationAtomId;

    public ExistenceContracts.ParameterExistenceResult Validate(ExistenceContracts.ParameterExistenceRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var findings = ParameterExistenceValidationAtomSupport.AnalyzeExistence(request);
        var evidence = ParameterExistenceValidationAtomSupport.BuildEvidence(request, AtomId, findings);
        return ParameterExistenceValidationAtomSupport.CreateResult(AtomId, request, findings, evidence);
    }
}

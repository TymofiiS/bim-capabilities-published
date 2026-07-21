using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Engines.Parameter.Atoms.Value;

/// <summary>
/// Validates that parameter values satisfy configured business rules.
/// </summary>
public sealed class ParameterValueValidationAtom : ValueContracts.IParameterValueValidationAtom
{
    public const string ValidationAtomId = "parameter.validation.value";

    public string AtomId => ValidationAtomId;

    public ValueContracts.ParameterValueValidationResult Validate(ValueContracts.ParameterValueValidationRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var findings = ParameterValueValidationAtomSupport.AnalyzeValues(request);
        var evidence = ParameterValueValidationAtomSupport.BuildEvidence(request, AtomId, findings);
        return ParameterValueValidationAtomSupport.CreateResult(AtomId, request, findings, evidence);
    }
}

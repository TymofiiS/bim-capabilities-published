using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;

/// <summary>
/// Validates that retrieved parameters match shared parameter definitions from a user-provided file.
/// </summary>
public sealed class SharedParameterValidationAtom : SharedParameterContracts.ISharedParameterValidationAtom
{
    public const string ValidationAtomId = "parameter.validation.shared-parameter";

    public string AtomId => ValidationAtomId;

    public SharedParameterContracts.SharedParameterValidationResult Validate(
        SharedParameterContracts.SharedParameterValidationRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.SharedParameterFile);

        var definitions = SharedParameterValidationAtomSupport.ResolveDefinitions(request);
        var findings = SharedParameterValidationAtomSupport.AnalyzeSharedParameters(request, definitions);
        var evidence = SharedParameterValidationAtomSupport.BuildEvidence(request, AtomId, findings);
        return SharedParameterValidationAtomSupport.CreateResult(AtomId, request, definitions, findings, evidence);
    }
}

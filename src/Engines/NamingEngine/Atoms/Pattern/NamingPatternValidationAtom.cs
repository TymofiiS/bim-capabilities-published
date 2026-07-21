using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Engines.Naming.Atoms.Pattern;

/// <summary>
/// Validates that object names follow configured naming pattern rules.
/// </summary>
public sealed class NamingPatternValidationAtom : PatternContracts.INamingPatternValidationAtom
{
    public const string ValidationAtomId = "naming.validation.pattern";

    public string AtomId => ValidationAtomId;

    public PatternContracts.NamingPatternValidationResult Validate(
        PatternContracts.NamingPatternValidationRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Rule);

        var findings = NamingPatternValidationAtomSupport.AnalyzePatterns(request);
        var evidence = NamingPatternValidationAtomSupport.BuildEvidence(request, AtomId, findings);
        return NamingPatternValidationAtomSupport.CreateResult(AtomId, request, findings, evidence);
    }
}

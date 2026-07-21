using PrefixContracts = BIMCapabilities.Contracts.Engines.Naming.Prefix;

namespace BIMCapabilities.Engines.Naming.Atoms.Prefix;

/// <summary>
/// Validates that object names start with a required prefix.
/// </summary>
public sealed class PrefixValidationAtom : PrefixContracts.IPrefixValidationAtom
{
    public const string ValidationAtomId = "naming.validation.prefix";

    public string AtomId => ValidationAtomId;

    public PrefixContracts.PrefixValidationResult Validate(PrefixContracts.PrefixValidationRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var findings = PrefixValidationAtomSupport.AnalyzePrefixes(request);
        var evidence = PrefixValidationAtomSupport.BuildEvidence(request, AtomId, findings);
        return PrefixValidationAtomSupport.CreateResult(AtomId, request, findings, evidence);
    }
}

using ImportedCadContracts = BIMCapabilities.Contracts.Engines.Family.ImportedCad;

namespace BIMCapabilities.Engines.Family.Atoms.ImportedCad;

/// <summary>
/// Detects imported CAD references from normalized family relationships.
/// </summary>
public sealed class ImportedCadDetectionAtom : ImportedCadContracts.IImportedCadDetectionAtom
{
    public const string DetectionAtomId = "family.detection.imported-cad";

    public string AtomId => DetectionAtomId;

    public ImportedCadContracts.ImportedCadDetectionResult Detect(ImportedCadContracts.ImportedCadDetectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var findings = ImportedCadDetectionAtomSupport.AnalyzeFamilies(request);
        var evidence = ImportedCadDetectionAtomSupport.BuildEvidence(request, AtomId, findings);
        return ImportedCadDetectionAtomSupport.CreateResult(AtomId, request, findings, evidence);
    }
}

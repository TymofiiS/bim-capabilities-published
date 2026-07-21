using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Family.ImportedCad;

/// <summary>
/// Contract for the Family Engine imported CAD detection atom.
/// </summary>
public interface IImportedCadDetectionAtom
{
    string AtomId { get; }

    ImportedCadDetectionResult Detect(ImportedCadDetectionRequest request);
}

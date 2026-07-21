namespace BIMCapabilities.Contracts.Engines.Naming.Pattern;

/// <summary>
/// Contract for the Naming Engine pattern validation atom.
/// </summary>
public interface INamingPatternValidationAtom
{
    string AtomId { get; }

    NamingPatternValidationResult Validate(NamingPatternValidationRequest request);
}

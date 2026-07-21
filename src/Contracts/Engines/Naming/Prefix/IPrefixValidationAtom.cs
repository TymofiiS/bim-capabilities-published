namespace BIMCapabilities.Contracts.Engines.Naming.Prefix;

/// <summary>
/// Contract for the Naming Engine prefix validation atom.
/// </summary>
public interface IPrefixValidationAtom
{
    string AtomId { get; }

    PrefixValidationResult Validate(PrefixValidationRequest request);
}

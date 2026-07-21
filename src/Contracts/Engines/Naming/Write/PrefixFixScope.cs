namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Declares which naming objects automatic prefix correction may rename.
/// </summary>
[Flags]
public enum PrefixFixScope
{
    None = 0,
    Type = 1,
    Family = 2,
    All = Type | Family
}

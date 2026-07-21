using BIMCapabilities.Contracts.Engines.Family;

namespace BIMCapabilities.Engines.Family;

/// <summary>
/// Assembly marker for the Family Engine project.
/// </summary>
public static class FamilyEngineAssembly
{
    /// <summary>
    /// Contract anchor ensuring the Family Engine project references Family Engine contracts.
    /// </summary>
    public static Type TargetSetContract => typeof(FamilyTargetSet);
}

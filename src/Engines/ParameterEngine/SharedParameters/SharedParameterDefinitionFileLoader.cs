using BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Engines.Parameter.SharedParameters;

/// <summary>
/// Loads shared parameter definitions from a company shared parameter file.
/// </summary>
public static class SharedParameterDefinitionFileLoader
{
    public static IReadOnlyList<SharedParameterContracts.SharedParameterDefinition> Load(string filePath)
    {
        return SharedParameterDefinitionLoader.Load(filePath);
    }
}

namespace BIMCapabilities.Contracts.Rules.Loading;

/// <summary>
/// Loads .bimrule files from disk into the BIMRule model.
/// </summary>
public interface IBimRuleLoader
{
    BimRuleLoadResult Load(string filePath);
}

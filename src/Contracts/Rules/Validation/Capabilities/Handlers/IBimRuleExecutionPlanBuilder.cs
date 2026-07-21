using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

/// <summary>
/// Accumulates execution-plan contributions from capability handlers.
/// </summary>
public interface IBimRuleExecutionPlanBuilder
{
    void EnableParameterCompliance();

    void EnableNamingCompliance();

    void EnableImportedCadExclusion();

    void EnableReportGeneration();

    void AddCategory(string categoryName);

    void SetRequiredParameters(string categoryName, IReadOnlyList<string> parameters);

    void SetParameterDefaults(string categoryName, IReadOnlyDictionary<string, string> parameterDefaults);

    void SetParameterFillRules(string categoryName, IReadOnlyDictionary<string, string> parameterFillRules);

    void SetParameterBindings(string categoryName, IReadOnlyDictionary<string, bool> parameterBindings);

    void SetRequiredPrefix(string categoryName, string prefix);

    void SetPrefixFixScope(string categoryName, PrefixFixScope prefixFixScope);

    void SetExcludeImportedCad(string categoryName, bool exclude);

    void AddInterpretationMarker(string marker);
}

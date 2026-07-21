using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Retains the latest launcher execution context for follow-up fix operations.
/// </summary>
internal static class LauncherSessionState
{
    internal static string? LastRuleFilePath { get; private set; }

    internal static ValidationPipelineResult? LastValidationResult { get; private set; }

    internal static BimRule? LastRule { get; private set; }

    internal static void Store(string ruleFilePath, ValidationPipelineResult validationResult)
    {
        LastRuleFilePath = ruleFilePath;
        LastValidationResult = validationResult;
        LastRule = validationResult.LoadResult.Rule;
    }

    internal static void Clear()
    {
        LastRuleFilePath = null;
        LastValidationResult = null;
        LastRule = null;
    }
}

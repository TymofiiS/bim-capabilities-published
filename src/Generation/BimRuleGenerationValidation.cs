using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;

namespace BIMCapabilities.Generation;

internal static class BimRuleGenerationValidation
{
    public static (bool Success, IReadOnlyList<BimRuleGenerationDiagnostic> Diagnostics) Validate(BimRule rule)
    {
        var diagnostics = new List<BimRuleGenerationDiagnostic>();

        var structuralResult = new BimRuleValidator().Validate(rule);
        AppendDiagnostics(diagnostics, structuralResult.Diagnostics, "BimRuleValidator");

        var versionResult = new BimRuleVersionValidator().Validate(rule);
        AppendDiagnostics(diagnostics, versionResult.Diagnostics, "BimRuleVersionValidator");

        var capabilityResult = new CapabilityCompatibilityValidator().Validate(rule);
        AppendDiagnostics(diagnostics, capabilityResult.Diagnostics, "CapabilityCompatibilityValidator");

        var configurationResult = new BimRuleConfigurationValidator().Validate(rule);
        AppendDiagnostics(diagnostics, configurationResult.Diagnostics, "BimRuleConfigurationValidator");

        var success = structuralResult.IsValid && versionResult.IsValid && capabilityResult.IsValid && configurationResult.IsValid;
        return (success, diagnostics);
    }

    public static (bool Success, IReadOnlyList<BimRuleGenerationDiagnostic> Diagnostics) ValidateFile(string filePath)
    {
        var loadResult = new BimRuleLoader().Load(filePath);
        if (loadResult.Rule is null)
        {
            return (false, loadResult.Diagnostics.Select(diagnostic => new BimRuleGenerationDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = "BimRuleLoader"
            }).ToList());
        }

        return Validate(loadResult.Rule);
    }

    private static void AppendDiagnostics(
        List<BimRuleGenerationDiagnostic> diagnostics,
        IEnumerable<BimRuleValidationDiagnostic> sourceDiagnostics,
        string source)
    {
        foreach (var diagnostic in sourceDiagnostics.Where(diagnostic => diagnostic.Severity == ValidationSeverity.Error))
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = source
            });
        }
    }

    private static void AppendDiagnostics(
        List<BimRuleGenerationDiagnostic> diagnostics,
        IEnumerable<VersionValidationDiagnostic> sourceDiagnostics,
        string source)
    {
        foreach (var diagnostic in sourceDiagnostics.Where(diagnostic => diagnostic.Severity == ValidationSeverity.Error))
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = source
            });
        }
    }

    private static void AppendDiagnostics(
        List<BimRuleGenerationDiagnostic> diagnostics,
        IEnumerable<CapabilityValidationDiagnostic> sourceDiagnostics,
        string source)
    {
        foreach (var diagnostic in sourceDiagnostics.Where(diagnostic => diagnostic.Severity == ValidationSeverity.Error))
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = source
            });
        }
    }
}

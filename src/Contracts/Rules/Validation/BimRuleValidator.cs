using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation;

/// <summary>
/// Validates structural requirements of the approved BIMRule contract.
/// </summary>
public sealed class BimRuleValidator : IBimRuleValidator
{
    public BimRuleValidationResult Validate(BimRule? rule)
    {
        var diagnostics = new List<BimRuleValidationDiagnostic>();

        if (rule is null)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.RuleMissing,
                "The BIMRule document is missing.",
                location: null);
            return CreateResult(diagnostics);
        }

        ValidateMetadata(rule.Metadata, diagnostics);
        ValidateEngines(rule.Engines, diagnostics);
        ValidateExecution(rule.Execution, diagnostics);
        ValidateReport(rule.Report, diagnostics);

        return CreateResult(diagnostics);
    }

    private static void ValidateMetadata(BimRuleMetadata? metadata, List<BimRuleValidationDiagnostic> diagnostics)
    {
        if (metadata is null)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.MetadataMissing,
                "The metadata section is required.",
                "metadata");
            return;
        }

        if (string.IsNullOrWhiteSpace(metadata.RuleId))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.RuleIdMissing,
                "RuleId is required.",
                "metadata.ruleId");
        }

        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.RuleNameMissing,
                "Rule name is required.",
                "metadata.name");
        }

        if (string.IsNullOrWhiteSpace(metadata.RuleVersion))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.RuleVersionMissing,
                "Rule version is required.",
                "metadata.ruleVersion");
        }

        if (string.IsNullOrWhiteSpace(metadata.ContractVersion))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.ContractVersionMissing,
                "Contract version is required.",
                "metadata.contractVersion");
        }
    }

    private static void ValidateEngines(IReadOnlyList<BimRuleEngine>? engines, List<BimRuleValidationDiagnostic> diagnostics)
    {
        if (engines is null)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.EnginesMissing,
                "The engines section is required.",
                "engines");
            return;
        }

        if (engines.Count == 0)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.EnginesEmpty,
                "At least one engine definition is required.",
                "engines");
            return;
        }

        for (var index = 0; index < engines.Count; index++)
        {
            var engine = engines[index];
            var locationPrefix = $"engines[{index}]";

            if (engine is null)
            {
                AddError(
                    diagnostics,
                    BimRuleValidationDiagnosticCodes.EnginesMissing,
                    "An engine definition is required.",
                    locationPrefix);
                continue;
            }

            if (string.IsNullOrWhiteSpace(engine.EngineId))
            {
                AddError(
                    diagnostics,
                    BimRuleValidationDiagnosticCodes.EngineIdMissing,
                    "EngineId is required for each engine definition.",
                    $"{locationPrefix}.engineId");
            }

            if (engine.Order <= 0)
            {
                AddError(
                    diagnostics,
                    BimRuleValidationDiagnosticCodes.EngineOrderInvalid,
                    "Engine order must be greater than zero.",
                    $"{locationPrefix}.order");
            }
        }
    }

    private static void ValidateExecution(BimRuleExecution? execution, List<BimRuleValidationDiagnostic> diagnostics)
    {
        if (execution is null)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.ExecutionMissing,
                "The execution section is required.",
                "execution");
            return;
        }

        if (string.IsNullOrWhiteSpace(execution.TargetPlatform))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.ExecutionTargetPlatformMissing,
                "Execution target platform is required.",
                "execution.targetPlatform");
        }

        if (string.IsNullOrWhiteSpace(execution.ExecutionMode))
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.ExecutionModeMissing,
                "Execution mode is required.",
                "execution.executionMode");
        }
    }

    private static void ValidateReport(BimRuleReport? report, List<BimRuleValidationDiagnostic> diagnostics)
    {
        if (report is null)
        {
            AddError(
                diagnostics,
                BimRuleValidationDiagnosticCodes.ReportMissing,
                "The report section is required.",
                "report");
        }
    }

    private static BimRuleValidationResult CreateResult(List<BimRuleValidationDiagnostic> diagnostics)
    {
        return new BimRuleValidationResult
        {
            Diagnostics = diagnostics
        };
    }

    private static void AddError(
        List<BimRuleValidationDiagnostic> diagnostics,
        string code,
        string message,
        string? location)
    {
        diagnostics.Add(new BimRuleValidationDiagnostic
        {
            Code = code,
            Severity = ValidationSeverity.Error,
            Message = message,
            Location = location
        });
    }
}

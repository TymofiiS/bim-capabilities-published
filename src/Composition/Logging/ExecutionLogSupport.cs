using BIMCapabilities.Contracts.Execution.Logging;
using BIMCapabilities.Contracts.Reports.Output;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using FamilyTargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;
using NamingComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Composition.Logging;

public static class ExecutionLogSupport
{
    internal static void WriteValidationStarted(IExecutionLog? log, string ruleId, IReadOnlyList<string> categories)
    {
        if (log is null)
        {
            return;
        }

        log.WriteInformation("validation-pipeline", $"Starting validation workflow for rule '{ruleId}'.");
        if (categories.Count > 0)
        {
            log.WriteInformation("validation-pipeline", $"Categories={string.Join(", ", categories)}");
        }
    }

    internal static void WriteCategoryResult(
        IExecutionLog? log,
        string categoryName,
        FamilyTargetSetContracts.FamilyTargetSetResult targetSetResult,
        ComplianceContracts.ParameterComplianceResult? parameterResult,
        NamingComplianceContracts.NamingComplianceResult? namingResult)
    {
        if (log is null)
        {
            return;
        }

        var familyCount = targetSetResult.Statistics?.TargetFamilies
            ?? targetSetResult.TargetSet.Families?.Count
            ?? 0;
        var typeCount = targetSetResult.TargetSet.FamilyTypes?.Count ?? 0;
        log.WriteInformation(
            "validation-pipeline",
            $"Category={categoryName}; Families={familyCount}; Types={typeCount}");

        if (parameterResult?.Summary is not null)
        {
            log.WriteInformation(
                "parameter-engine",
                $"Category={categoryName}; Compliance={parameterResult.Summary.CompliancePercentage:0.##}%; FailedChecks={parameterResult.Summary.FailedChecks}");
        }

        if (namingResult?.Summary is not null)
        {
            log.WriteInformation(
                "naming-engine",
                $"Category={categoryName}; Compliance={namingResult.Summary.CompliancePercentage:0.##}%; FailedChecks={namingResult.Summary.FailedChecks}");
        }
    }

    internal static void WriteValidationCompleted(IExecutionLog? log, ReportOutput? reportOutput)
    {
        if (log is null)
        {
            return;
        }

        var summary = reportOutput?.Sections
            .FirstOrDefault(section => section.Name == "Compliance Summary")
            ?.Content?.StructuredData;

        if (summary is null)
        {
            log.WriteInformation("validation-pipeline", "Validation workflow completed.");
            return;
        }

        var resultStatus = summary.GetValueOrDefault("resultStatus") ?? "Unknown";
        var issuesFound = summary.GetValueOrDefault("issuesFound") ?? "0";
        log.WriteInformation(
            "validation-pipeline",
            $"Validation workflow completed. Result={resultStatus}; IssuesFound={issuesFound}");
    }

    public static void WriteFixStarted(IExecutionLog? log, int writeRequestCount)
    {
        if (log is null)
        {
            return;
        }

        log.WriteInformation("fix-pipeline", $"Automatic correction started. WriteRequests={writeRequestCount}");
    }

    public static void WriteFixCompleted(IExecutionLog? log, int parametersAdded, int valuesAssigned, int affectedFamilies)
    {
        if (log is null)
        {
            return;
        }

        log.WriteInformation(
            "fix-pipeline",
            $"Automatic correction completed. ParametersAdded={parametersAdded}; ValuesAssigned={valuesAssigned}; AffectedFamilies={affectedFamilies}");
    }

    public static void WriteFixFailed(IExecutionLog? log, string message)
    {
        log?.WriteError("fix-pipeline", message);
    }

    public static void WriteFixFamilyUpdated(IExecutionLog? log, string familyName, int familyIndex, int familyCount)
    {
        if (log is null)
        {
            return;
        }

        log.WriteInformation(
            "revit-launcher",
            $"Family {familyIndex}/{familyCount}: '{familyName}' updated.");
    }
}

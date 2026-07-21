using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Engines.Family.TargetSet;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using NamingComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Composition.Validation;

internal static class ValidationPipelineScopeSupport
{
    internal const string CategoryScopeCode = "ValidationScope.Category";

    internal static DiagnosticRecord CreateCategoryScopeDiagnostic(
        string categoryName,
        ExecutionScope executionScope,
        FamilyTargetSetResult targetSetResult,
        ComplianceContracts.ParameterComplianceResult? parameterResult,
        NamingComplianceContracts.NamingComplianceResult? namingResult,
        DateTimeOffset executedAt,
        string correlationId,
        IReadOnlyDictionary<string, bool>? parameterBindings = null)
    {
        var familiesChecked = CountFamilies(targetSetResult);
        var typesChecked = CountTypes(targetSetResult);
        var instancesChecked = CountPlacedInstancesInTargetSet(targetSetResult);
        var hasInstanceBoundParameters = parameterBindings?.Any(binding => binding.Value) == true;
        var validationLevel = ResolveValidationLevel(
            typesChecked,
            familiesChecked,
            instancesChecked,
            hasInstanceBoundParameters);
        var objectsChecked = ResolveObjectsChecked(
            validationLevel,
            typesChecked,
            familiesChecked,
            instancesChecked,
            parameterResult,
            namingResult);

        var structuredMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["scopeCategory"] = categoryName,
            ["modelScope"] = executionScope.ScopeType,
            ["validationLevel"] = validationLevel,
            ["familiesChecked"] = familiesChecked.ToString(),
            ["typesChecked"] = typesChecked.ToString(),
            ["objectsChecked"] = objectsChecked.ToString(),
            ["placedInstances"] = instancesChecked.ToString(),
            ["instancesChecked"] = instancesChecked.ToString()
        };

        if (!string.IsNullOrWhiteSpace(executionScope.TargetDescription))
        {
            structuredMetadata["modelScopeDescription"] = executionScope.TargetDescription;
        }

        return new DiagnosticRecord
        {
            DiagnosticId = $"validation-scope-{NormalizeScopeKey(categoryName)}-{correlationId}",
            Timestamp = executedAt,
            Source = new DiagnosticSource
            {
                ComponentType = "ValidationPipeline",
                ComponentId = ValidationPipeline.PipelineId,
                Operation = "ValidationScope",
                Code = CategoryScopeCode
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message =
                $"Validation scope for {categoryName}: {validationLevel} level, {typesChecked} type(s), {familiesChecked} family/families, {objectsChecked} object(s) checked.",
            StructuredMetadata = structuredMetadata,
            CorrelationId = correlationId
        };
    }

    private static int CountFamilies(FamilyTargetSetResult targetSetResult)
    {
        if (targetSetResult.TargetSet.Families is { Count: > 0 } families)
        {
            return families.Count;
        }

        return targetSetResult.Statistics?.TargetFamilies ?? 0;
    }

    private static int CountTypes(FamilyTargetSetResult targetSetResult)
    {
        if (targetSetResult.TargetSet.FamilyTypes is { Count: > 0 } familyTypes)
        {
            return familyTypes.Count;
        }

        if (targetSetResult.TargetSet.Families is not { Count: > 0 } families)
        {
            return 0;
        }

        return families.Sum(family => family.FamilyTypes?.Count ?? 0);
    }

    private static int CountPlacedInstancesInTargetSet(FamilyTargetSetResult targetSetResult)
    {
        if (targetSetResult.TargetSet.PlacedInstances is { Count: > 0 } placedInstances)
        {
            return placedInstances.Count;
        }

        if (targetSetResult.TargetSet.FamilyTypes is { Count: > 0 } familyTypes)
        {
            return familyTypes.Sum(ParsePlacedInstanceCount);
        }

        if (targetSetResult.TargetSet.Families is not { Count: > 0 } families)
        {
            return 0;
        }

        return families.Sum(family => family.FamilyTypes?.Sum(ParsePlacedInstanceCount) ?? 0);
    }

    private static int ParsePlacedInstanceCount(NormalizedFamilyType familyType)
    {
        if (familyType.Metadata is null)
        {
            return 0;
        }

        return int.TryParse(familyType.Metadata.GetValueOrDefault("placedInstanceCount"), out var count) ? count : 0;
    }

    private static int ResolveObjectsChecked(
        string validationLevel,
        int typesChecked,
        int familiesChecked,
        int instancesChecked,
        ComplianceContracts.ParameterComplianceResult? parameterResult,
        NamingComplianceContracts.NamingComplianceResult? namingResult)
    {
        var parameterObjectsChecked = parameterResult?.Statistics?.ObjectsChecked ?? 0;
        var namingObjectsChecked = namingResult?.Statistics?.ObjectsChecked ?? 0;
        var engineObjectsChecked = Math.Max(parameterObjectsChecked, namingObjectsChecked);

        if (string.Equals(validationLevel, "Instance", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(instancesChecked, engineObjectsChecked);
        }

        return Math.Max(Math.Max(engineObjectsChecked, typesChecked), familiesChecked);
    }

    private static string ResolveValidationLevel(
        int typesChecked,
        int familiesChecked,
        int instancesChecked,
        bool hasInstanceBoundParameters)
    {
        if (hasInstanceBoundParameters && instancesChecked > 0)
        {
            return "Instance";
        }

        if (typesChecked > 0)
        {
            return "Type";
        }

        if (familiesChecked > 0)
        {
            return "Family";
        }

        return "Project";
    }

    private static string NormalizeScopeKey(string categoryName)
    {
        return new string(categoryName
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');
    }
}

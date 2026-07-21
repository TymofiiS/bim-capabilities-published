using BIMCapabilities.Composition.Interpretation;
using BIMCapabilities.Composition.Mapping;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Write;
using BIMCapabilities.Engines.Naming.Write;
using BIMCapabilities.Engines.Parameter.SharedParameters;
using BIMCapabilities.Engines.Parameter.Write;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using FamilyTargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;
using NamingComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Composition.Fix;

/// <summary>
/// Builds parameter and naming fix write requests from validation findings.
/// </summary>
public sealed class FixPipeline
{
    private readonly ParameterWriteRequestBuilder _parameterBuilder = new();
    private readonly NamingWriteRequestBuilder _namingBuilder = new();

    public FixPipelineResult BuildWriteRequests(FixPipelineRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var validationResult = request.ValidationResult;
        var rule = validationResult.LoadResult.Rule;
        if (rule is null || !validationResult.RuleValidationSucceeded)
        {
            return new FixPipelineResult
            {
                Succeeded = false,
                ErrorMessage = "Validation must succeed before fixes can be prepared."
            };
        }

        if (!rule.Execution.FixEnabled)
        {
            return new FixPipelineResult
            {
                Succeeded = false,
                ErrorMessage = "Fix is disabled for this rule. Set execution.fixEnabled to true."
            };
        }

        var executionPlan = BimRuleExecutionInterpreter.Interpret(rule);
        var sharedParameterFilePath = request.SharedParameterFilePathOverride
            ?? BimRuleExecutionInterpreter.ResolveSharedParameterFilePath(rule, null);
        IReadOnlyList<SharedParameterDefinition>? sharedDefinitions = null;
        if (!string.IsNullOrWhiteSpace(sharedParameterFilePath))
        {
            sharedDefinitions = SharedParameterDefinitionFileLoader.Load(sharedParameterFilePath);
        }

        var correlationId = request.CorrelationId ?? $"corr-fix-{Guid.NewGuid():N}";
        var executedAt = request.ExecutedAt ?? DateTimeOffset.UtcNow;
        var writeRequests = new List<WriteRequest>();
        var totalStatistics = new ParameterWriteRequestBuildStatistics();
        var affectedTypeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var defaultValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var namesRenamed = 0;
        var order = 1;

        foreach (var category in executionPlan.Categories)
        {
            if (!string.IsNullOrWhiteSpace(category.RequiredPrefix)
                && category.PrefixFixScope != PrefixFixScope.None)
            {
                var namingResult = ResolveNamingResult(category.CategoryName, validationResult);
                var targetSetResult = ResolveTargetSetResult(category.CategoryName, validationResult);
                if (namingResult is not null && targetSetResult is not null)
                {
                    var namingBuildResult = _namingBuilder.Build(new NamingWriteRequestBuildRequest
                    {
                        ComplianceResult = namingResult,
                        TargetSet = TargetSetMapper.ToNamingTargetSet(targetSetResult.TargetSet),
                        RequiredPrefixes = [category.RequiredPrefix!],
                        PatternRule = BimRuleNamingPatternSupport.CreatePatternRule(category.RequiredPrefix!),
                        PrefixFixScope = category.PrefixFixScope,
                        RuleId = rule.Metadata.RuleId,
                        CorrelationId = correlationId,
                        RequestedAt = executedAt
                    });

                    foreach (var namingRequest in namingBuildResult.WriteRequests ?? [])
                    {
                        writeRequests.Add(RewriteOrder(namingRequest, order++));
                    }

                    namesRenamed += namingBuildResult.WriteRequests?.Count ?? 0;
                }
            }

            if (category.RequiredParameters.Count == 0)
            {
                continue;
            }

            var parameterResult = ResolveParameterResult(category.CategoryName, validationResult);
            var parameterTargetSetResult = ResolveTargetSetResult(category.CategoryName, validationResult);
            if (parameterResult is null || parameterTargetSetResult is null)
            {
                continue;
            }

            var parameterBuildResult = _parameterBuilder.Build(new ParameterWriteRequestBuildRequest
            {
                ComplianceResult = parameterResult,
                TargetSet = TargetSetMapper.ToParameterTargetSet(parameterTargetSetResult.TargetSet),
                SharedParameterDefinitions = sharedDefinitions,
                ParameterDefaults = category.ParameterDefaults,
                ParameterFillRules = category.ParameterFillRules,
                ParameterBindings = category.ParameterBindings,
                RuleId = rule.Metadata.RuleId,
                CorrelationId = correlationId,
                RequestedAt = executedAt
            });

            foreach (var parameterRequest in parameterBuildResult.WriteRequests ?? [])
            {
                writeRequests.Add(RewriteOrder(parameterRequest, order++));
            }

            if (parameterBuildResult.Statistics is not null)
            {
                totalStatistics = MergeStatistics(totalStatistics, parameterBuildResult.Statistics);
            }

            foreach (var writeRequest in parameterBuildResult.WriteRequests ?? [])
            {
                affectedTypeIds.Add(writeRequest.TargetObject.Id);
                if (writeRequest.Payload?.TryGetValue("requestedValue", out var value) == true
                    && writeRequest.Payload.TryGetValue("parameterName", out var parameterName))
                {
                    defaultValues.Add($"{parameterName}={value}");
                }
            }
        }

        if (writeRequests.Count == 0)
        {
            return new FixPipelineResult
            {
                Succeeded = false,
                ErrorMessage = "No supported parameter fixes were found for the current validation findings."
            };
        }

        return new FixPipelineResult
        {
            Succeeded = true,
            WriteRequests = writeRequests,
            Statistics = totalStatistics,
            SharedParameterFilePath = sharedParameterFilePath,
            CorrectionSummary = new FixCorrectionSummary
            {
                ParametersAdded = totalStatistics.CreateRequests,
                ValuesAssigned = totalStatistics.CreateRequests + totalStatistics.UpdateRequests,
                AffectedTypes = affectedTypeIds.Count,
                NamesRenamed = namesRenamed,
                DefaultValuesApplied = defaultValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
            }
        };
    }

    private static WriteRequest RewriteOrder(WriteRequest source, int order) =>
        source with { Order = order };

    private static ComplianceContracts.ParameterComplianceResult? ResolveParameterResult(
        string categoryName,
        ValidationPipelineResult validationResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.DoorParameterResult;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.WindowParameterResult;
        }

        return null;
    }

    private static NamingComplianceContracts.NamingComplianceResult? ResolveNamingResult(
        string categoryName,
        ValidationPipelineResult validationResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.DoorNamingResult;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.WindowNamingResult;
        }

        return null;
    }

    private static FamilyTargetSetContracts.FamilyTargetSetResult? ResolveTargetSetResult(
        string categoryName,
        ValidationPipelineResult validationResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.DoorTargetSetResult;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return validationResult.WindowTargetSetResult;
        }

        return null;
    }

    private static ParameterWriteRequestBuildStatistics MergeStatistics(
        ParameterWriteRequestBuildStatistics current,
        ParameterWriteRequestBuildStatistics additional)
    {
        return new ParameterWriteRequestBuildStatistics
        {
            FindingsProcessed = current.FindingsProcessed + additional.FindingsProcessed,
            RequestsGenerated = current.RequestsGenerated + additional.RequestsGenerated,
            CreateRequests = current.CreateRequests + additional.CreateRequests,
            UpdateRequests = current.UpdateRequests + additional.UpdateRequests,
            DeleteRequests = current.DeleteRequests + additional.DeleteRequests,
            SkippedFindings = current.SkippedFindings + additional.SkippedFindings
        };
    }
}

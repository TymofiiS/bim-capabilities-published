using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;
using BIMCapabilities.Composition.Capabilities;
using BIMCapabilities.Composition.Capabilities.Handlers;
using BIMCapabilities.Composition.Interpretation;
using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Composition.Mapping;
using BIMCapabilities.Engines.Family.Generation;
using BIMCapabilities.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Parameter.Compliance;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;
using BIMCapabilities.Runtime;
using BIMCapabilities.Runtime.Diagnostics;
using BIMCapabilities.Runtime.Evidence;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using FamilyTargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;
using NamingComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;
using EngineSharedParameterFileReference = BIMCapabilities.Contracts.Engines.Parameter.ParameterSharedParameterFileReference;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Composition.Validation;

internal static class ValidationPipelineSupport
{
    internal static ValidationPipelineResult ExecuteWorkflow(
        ValidationPipelineRequest request,
        BimRule rule,
        RuntimeSkeleton runtime,
        DateTimeOffset executedAt,
        string correlationId)
    {
        var executionPlan = BimRuleExecutionInterpreter.Interpret(rule);
        ExecutionLogSupport.WriteValidationStarted(
            request.ExecutionLog,
            rule.Metadata.RuleId,
            executionPlan.Categories.Select(category => category.CategoryName).ToArray());
        string? sharedParameterFilePath = null;

        if (executionPlan.RunParameterCompliance)
        {
            sharedParameterFilePath = BimRuleExecutionInterpreter.ResolveSharedParameterFilePath(
                rule,
                request.SharedParameterFilePathOverride);

            if (string.IsNullOrWhiteSpace(sharedParameterFilePath))
            {
                throw new InvalidOperationException(
                    "Shared parameter file path is required when parameter.existence is configured.");
            }
        }

        var executionContext = CreateExecutionContext(request, rule, correlationId, executedAt);
        runtime.Context.SetContext(executionContext);
        RegisterMvpEngines(runtime);

        var plan = runtime.Execution.CreatePlan(executionContext);
        var familyGenerator = new FamilyTargetSetGenerator();
        var parameterEngine = new ParameterComplianceEngine();
        var namingEngine = new NamingComplianceEngine();

        FamilyTargetSetContracts.FamilyTargetSetResult? doorTargetSetResult = null;
        FamilyTargetSetContracts.FamilyTargetSetResult? windowTargetSetResult = null;
        ComplianceContracts.ParameterComplianceResult? doorParameterResult = null;
        ComplianceContracts.ParameterComplianceResult? windowParameterResult = null;
        NamingComplianceContracts.NamingComplianceResult? doorNamingResult = null;
        NamingComplianceContracts.NamingComplianceResult? windowNamingResult = null;

        var categoryIndex = 0;
        foreach (var category in executionPlan.Categories)
        {
            categoryIndex++;
            request.ProgressReporter?.Invoke(
                categoryIndex,
                executionPlan.Categories.Count,
                $"Validating {category.CategoryName}...");

            var targetSetResult = familyGenerator.Generate(
                CreateFamilyTargetSetRequest(
                    CreateTargetSetDefinition(category, executionPlan.RuleId),
                    executionPlan.RuleId,
                    correlationId,
                    executedAt),
                request.FamilyProvider);

            AssignTargetSetResult(
                category.CategoryName,
                targetSetResult,
                ref doorTargetSetResult,
                ref windowTargetSetResult);

            AddEvidence(runtime.Evidence, targetSetResult.Evidence);
            AddDiagnostics(runtime.Diagnostics, targetSetResult.Diagnostics, correlationId, "family-engine");

            ComplianceContracts.ParameterComplianceResult? categoryParameterResult = null;
            NamingComplianceContracts.NamingComplianceResult? categoryNamingResult = null;

            if (executionPlan.RunParameterCompliance &&
                category.RequiredParameters.Count > 0 &&
                !string.IsNullOrWhiteSpace(sharedParameterFilePath))
            {
                categoryParameterResult = parameterEngine.Evaluate(CreateParameterRequest(
                    TargetSetMapper.ToParameterTargetSet(targetSetResult.TargetSet),
                    sharedParameterFilePath!,
                    category.RequiredParameters,
                    ParameterExistenceCapabilityHandler.InferSharedParameterNames(category.RequiredParameters),
                    category.ParameterBindings,
                    executionPlan.RuleId,
                    correlationId,
                    executedAt));

                AssignParameterResult(
                    category.CategoryName,
                    categoryParameterResult,
                    ref doorParameterResult,
                    ref windowParameterResult);

                AddEvidence(runtime.Evidence, categoryParameterResult.Evidence);
                AddDiagnostics(runtime.Diagnostics, categoryParameterResult.Diagnostics, correlationId, "parameter-engine");
            }

            if (executionPlan.RunNamingCompliance &&
                !string.IsNullOrWhiteSpace(category.RequiredPrefix))
            {
                categoryNamingResult = namingEngine.Evaluate(new NamingComplianceContracts.NamingComplianceRequest
                {
                    TargetSet = TargetSetMapper.ToNamingTargetSet(targetSetResult.TargetSet),
                    RequiredPrefixes = [category.RequiredPrefix!],
                    PrefixFixScope = category.PrefixFixScope,
                    ExecutedAt = executedAt,
                    RuleId = executionPlan.RuleId,
                    CorrelationId = correlationId
                });

                AssignNamingResult(
                    category.CategoryName,
                    categoryNamingResult,
                    ref doorNamingResult,
                    ref windowNamingResult);

                AddEvidence(runtime.Evidence, categoryNamingResult.Evidence);
                AddDiagnostics(runtime.Diagnostics, categoryNamingResult.Diagnostics, correlationId, "naming-engine");
            }

            runtime.Diagnostics.Add(ValidationPipelineScopeSupport.CreateCategoryScopeDiagnostic(
                category.CategoryName,
                executionContext.Scope,
                targetSetResult,
                categoryParameterResult,
                categoryNamingResult,
                executedAt,
                correlationId,
                category.ParameterBindings));

            ExecutionLogSupport.WriteCategoryResult(
                request.ExecutionLog,
                category.CategoryName,
                targetSetResult,
                categoryParameterResult,
                categoryNamingResult);
        }

        request.ProgressReporter?.Invoke(1, 1, "Generating validation report...");

        runtime.Diagnostics.Add(new DiagnosticRecord
        {
            DiagnosticId = $"validation-pipeline-completed-{correlationId}",
            Timestamp = executedAt,
            Source = new DiagnosticSource
            {
                ComponentType = "ValidationPipeline",
                ComponentId = ValidationPipeline.PipelineId,
                Operation = "Execute",
                Code = "ValidationPipelineCompleted"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = "Validation pipeline completed successfully.",
            CorrelationId = correlationId
        });

        ReportOutput? reportOutput = null;
        HtmlRenderResult? htmlReport = null;
        JsonRenderResult? jsonReport = null;

        if (executionPlan.GenerateReport &&
            (rule.Report.GenerateHtmlReport || rule.Report.GenerateJsonReport))
        {
            var profile = new ComplianceReportProfile();
            reportOutput = profile.Prepare(new ReportProfileRequest
            {
                RuleId = rule.Metadata.RuleId,
                RuleName = rule.Metadata.Name,
                ReportTitle = rule.Report.ReportTitle ?? rule.Metadata.Name ?? "Compliance Report",
                Evidence = runtime.Evidence.Collection with { CorrelationId = correlationId },
                Diagnostics = runtime.Diagnostics.Collection with { CorrelationId = correlationId },
                ExecutionScope = executionContext.Scope,
                CorrelationId = correlationId,
                GeneratedAt = executedAt,
                FixEnabled = rule.Execution.FixEnabled,
                ParameterDefaults = MergeParameterDefaults(executionPlan.Categories),
                ParameterFillRules = MergeParameterFillRules(executionPlan.Categories),
                CategoryFixConfiguration = BuildCategoryFixConfiguration(executionPlan.Categories)
            });

            if (rule.Report.GenerateHtmlReport)
            {
                htmlReport = new HtmlReportRenderer().Render(reportOutput);
            }

            if (rule.Report.GenerateJsonReport)
            {
                jsonReport = new JsonReportRenderer().Render(reportOutput);
            }
        }

        ExecutionLogSupport.WriteValidationCompleted(request.ExecutionLog, reportOutput);

        var executionResult = new ExecutionResult
        {
            Status = ExecutionStatus.Completed,
            Diagnostics = runtime.Diagnostics.Collection with { CorrelationId = correlationId },
            Evidence = runtime.Evidence.Collection with { CorrelationId = correlationId },
            Summary = new ExecutionSummary
            {
                Status = ExecutionStatus.Completed,
                StartedAt = executedAt,
                CompletedAt = executedAt,
                TotalSteps = plan.Steps.Count,
                CompletedSteps = plan.Steps.Count,
                FailedSteps = 0,
                SkippedSteps = 0,
                Message = "Validation pipeline completed successfully."
            },
            Correlation = new ExecutionCorrelation
            {
                CorrelationId = correlationId,
                PlanId = plan.PlanId
            }
        };

        return new ValidationPipelineResult
        {
            LoadResult = new BimRuleLoadResult { Rule = rule },
            RuleValidationSucceeded = true,
            Plan = plan,
            ExecutionResult = executionResult,
            DoorTargetSetResult = doorTargetSetResult,
            WindowTargetSetResult = windowTargetSetResult,
            DoorParameterResult = doorParameterResult,
            WindowParameterResult = windowParameterResult,
            DoorNamingResult = doorNamingResult,
            WindowNamingResult = windowNamingResult,
            ReportOutput = reportOutput,
            HtmlReport = htmlReport,
            JsonReport = jsonReport
        };
    }

    internal static ValidationPipelineResult CreateFailedLoadResult(BimRuleLoadResult loadResult)
    {
        return new ValidationPipelineResult
        {
            LoadResult = loadResult,
            RuleValidationSucceeded = false
        };
    }

    internal static ValidationPipelineResult CreateFailedValidationResult(
        BimRuleLoadResult loadResult,
        BimRuleValidationResult? structureValidation,
        VersionValidationResult? versionValidation,
        CapabilityValidationResult? capabilityValidation)
    {
        return new ValidationPipelineResult
        {
            LoadResult = loadResult,
            StructureValidation = structureValidation,
            VersionValidation = versionValidation,
            CapabilityValidation = capabilityValidation,
            RuleValidationSucceeded = false
        };
    }

    private static FamilyTargetSetContracts.TargetSetDefinition CreateTargetSetDefinition(
        CategoryExecutionSpecification category,
        string ruleId)
    {
        return new FamilyTargetSetContracts.TargetSetDefinition
        {
            Name = $"{category.CategoryName} Families",
            Description = $"Families in category '{category.CategoryName}' subject to validation.",
            SelectionCriteria = new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = [category.CategoryName] }
            },
            ComplianceCriteria = new FamilyTargetSetContracts.TargetSetComplianceCriteria
            {
                ImportedCadMode = category.ExcludeImportedCad
                    ? FamilyTargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad
                    : FamilyTargetSetContracts.ImportedCadComplianceMode.None
            },
            Metadata = new Dictionary<string, string>
            {
                ["category"] = category.CategoryName,
                ["ruleId"] = ruleId
            }
        };
    }

    private static void AssignTargetSetResult(
        string categoryName,
        FamilyTargetSetContracts.FamilyTargetSetResult result,
        ref FamilyTargetSetContracts.FamilyTargetSetResult? doorTargetSetResult,
        ref FamilyTargetSetContracts.FamilyTargetSetResult? windowTargetSetResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            doorTargetSetResult = result;
            return;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            windowTargetSetResult = result;
        }
    }

    private static void AssignParameterResult(
        string categoryName,
        ComplianceContracts.ParameterComplianceResult result,
        ref ComplianceContracts.ParameterComplianceResult? doorParameterResult,
        ref ComplianceContracts.ParameterComplianceResult? windowParameterResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            doorParameterResult = result;
            return;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            windowParameterResult = result;
        }
    }

    private static void AssignNamingResult(
        string categoryName,
        NamingComplianceContracts.NamingComplianceResult result,
        ref NamingComplianceContracts.NamingComplianceResult? doorNamingResult,
        ref NamingComplianceContracts.NamingComplianceResult? windowNamingResult)
    {
        if (string.Equals(categoryName, "Doors", StringComparison.OrdinalIgnoreCase))
        {
            doorNamingResult = result;
            return;
        }

        if (string.Equals(categoryName, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            windowNamingResult = result;
        }
    }

    private static Contracts.Execution.ExecutionContext CreateExecutionContext(
        ValidationPipelineRequest request,
        BimRule rule,
        string correlationId,
        DateTimeOffset executedAt)
    {
        return new Contracts.Execution.ExecutionContext
        {
            Rule = rule,
            RuleSourcePath = request.RuleFilePath,
            Request = new ExecutionRequest
            {
                Mode = ExecutionMode.Validation,
                RequestedAt = executedAt,
                RequestedBy = "ValidationPipeline"
            },
            Scope = request.Scope,
            Environment = request.Environment,
            CorrelationId = correlationId
        };
    }

    private static void RegisterMvpEngines(RuntimeSkeleton runtime)
    {
        foreach (var registration in CreateMvpEngineRegistrations())
        {
            runtime.Registry.Register(registration);
        }
    }

    private static FamilyTargetSetContracts.FamilyTargetSetRequest CreateFamilyTargetSetRequest(
        FamilyTargetSetContracts.TargetSetDefinition definition,
        string ruleId,
        string correlationId,
        DateTimeOffset executedAt)
    {
        return new FamilyTargetSetContracts.FamilyTargetSetRequest
        {
            Definition = definition,
            ExecutedAt = executedAt,
            RuleId = ruleId,
            CorrelationId = correlationId
        };
    }

    private static ComplianceContracts.ParameterComplianceRequest CreateParameterRequest(
        global::BIMCapabilities.Contracts.Engines.Parameter.ParameterTargetSet targetSet,
        string sharedParameterFilePath,
        IReadOnlyList<string> requiredParameterNames,
        IReadOnlyList<string> sharedParameterNames,
        IReadOnlyDictionary<string, bool> parameterBindings,
        string ruleId,
        string correlationId,
        DateTimeOffset executedAt)
    {
        return new ComplianceContracts.ParameterComplianceRequest
        {
            TargetSet = targetSet,
            SharedParameterFile = new EngineSharedParameterFileReference { FilePath = sharedParameterFilePath },
            RequiredParameterNames = requiredParameterNames,
            SharedParameterNamesToValidate = sharedParameterNames,
            ParameterBindings = parameterBindings,
            ValueRules = requiredParameterNames
                .Select(name => new ValueContracts.ParameterValueRule
                {
                    ParameterName = name,
                    RequiredValue = true
                })
                .ToArray(),
            ExecutedAt = executedAt,
            RuleId = ruleId,
            CorrelationId = correlationId
        };
    }

    private static void AddEvidence(IRuntimeEvidence evidenceSink, IReadOnlyList<EvidenceRecord>? records)
    {
        if (records is null)
        {
            return;
        }

        foreach (var record in records)
        {
            evidenceSink.Add(record);
        }
    }

    private static void AddDiagnostics(
        IRuntimeDiagnostics diagnosticsSink,
        IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Family.FamilyEngineDiagnostic>? diagnostics,
        string correlationId,
        string engineId)
    {
        AddDiagnostics(diagnosticsSink, diagnostics?.Select(diagnostic => diagnostic.Message), correlationId, engineId, "FamilyEngine");
    }

    private static void AddDiagnostics(
        IRuntimeDiagnostics diagnosticsSink,
        IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Parameter.ParameterEngineDiagnostic>? diagnostics,
        string correlationId,
        string engineId)
    {
        AddDiagnostics(diagnosticsSink, diagnostics?.Select(diagnostic => diagnostic.Message), correlationId, engineId, "ParameterEngine");
    }

    private static void AddDiagnostics(
        IRuntimeDiagnostics diagnosticsSink,
        IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Naming.NamingEngineDiagnostic>? diagnostics,
        string correlationId,
        string engineId)
    {
        AddDiagnostics(diagnosticsSink, diagnostics?.Select(diagnostic => diagnostic.Message), correlationId, engineId, "NamingEngine");
    }

    private static void AddDiagnostics(
        IRuntimeDiagnostics diagnosticsSink,
        IEnumerable<string>? messages,
        string correlationId,
        string engineId,
        string componentType)
    {
        if (messages is null)
        {
            return;
        }

        var index = 0;
        foreach (var message in messages)
        {
            diagnosticsSink.Add(new DiagnosticRecord
            {
                DiagnosticId = $"{engineId}-diagnostic-{correlationId}-{index:D3}",
                Timestamp = DateTimeOffset.UtcNow,
                Source = new DiagnosticSource
                {
                    ComponentType = componentType,
                    ComponentId = engineId,
                    Operation = "Evaluate",
                    Code = "EngineDiagnostic"
                },
                Category = DiagnosticCategory.Execution,
                Severity = DiagnosticSeverity.Information,
                Message = message,
                CorrelationId = correlationId
            });
            index++;
        }
    }

    private static IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Registration.EngineRegistration> CreateMvpEngineRegistrations()
    {
        var registeredAt = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);

        return CapabilityPlatform.Default.Discovery.GetSupportedCapabilities()
            .GroupBy(definition => definition.EngineId, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateRegistration(
                group.Key,
                MapEngineType(group.Key),
                group.Select(definition => definition.CapabilityId)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                registeredAt))
            .OrderBy(registration => registration.Engine.EngineId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static global::BIMCapabilities.Contracts.Engines.Registration.EngineType MapEngineType(string engineId)
    {
        return engineId switch
        {
            "naming-engine" => global::BIMCapabilities.Contracts.Engines.Registration.EngineType.Naming,
            "parameter-engine" => global::BIMCapabilities.Contracts.Engines.Registration.EngineType.Parameter,
            "family-engine" => global::BIMCapabilities.Contracts.Engines.Registration.EngineType.Family,
            "report-engine" => global::BIMCapabilities.Contracts.Engines.Registration.EngineType.Report,
            _ => global::BIMCapabilities.Contracts.Engines.Registration.EngineType.Custom
        };
    }

    private static global::BIMCapabilities.Contracts.Engines.Registration.EngineRegistration CreateRegistration(
        string engineId,
        global::BIMCapabilities.Contracts.Engines.Registration.EngineType engineType,
        IReadOnlyList<string> capabilityNames,
        DateTimeOffset registeredAt)
    {
        return new global::BIMCapabilities.Contracts.Engines.Registration.EngineRegistration
        {
            RegisteredAt = registeredAt,
            Engine = new global::BIMCapabilities.Contracts.Engines.Registration.EngineDefinition
            {
                EngineId = engineId,
                Name = engineId,
                EngineType = engineType,
                Version = new global::BIMCapabilities.Contracts.Engines.Registration.EngineVersion
                {
                    Version = "1.0",
                    ConfigurationSchemaVersion = "1.0",
                    RuntimeCompatibilityVersion = "1.0"
                },
                Capabilities = capabilityNames
                    .Select(capabilityName => new global::BIMCapabilities.Contracts.Engines.Registration.EngineCapability
                    {
                        CapabilityName = capabilityName,
                        CapabilityVersion = "1.0",
                        CapabilityCategory = "Validation"
                    })
                    .ToArray()
            }
        };
    }

    private static IReadOnlyDictionary<string, string> MergeParameterDefaults(
        IReadOnlyList<CategoryExecutionSpecification> categories)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            foreach (var entry in category.ParameterDefaults)
            {
                defaults[entry.Key] = entry.Value;
            }
        }

        return defaults;
    }

    private static IReadOnlyDictionary<string, string> MergeParameterFillRules(
        IReadOnlyList<CategoryExecutionSpecification> categories)
    {
        var fillRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            foreach (var entry in category.ParameterFillRules)
            {
                fillRules[entry.Key] = entry.Value;
            }
        }

        return fillRules;
    }

    private static IReadOnlyDictionary<string, ReportCategoryFixConfiguration> BuildCategoryFixConfiguration(
        IReadOnlyList<CategoryExecutionSpecification> categories)
    {
        var configuration = new Dictionary<string, ReportCategoryFixConfiguration>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.RequiredPrefix)
                && category.PrefixFixScope == PrefixFixScope.None)
            {
                continue;
            }

            configuration[category.CategoryName] = new ReportCategoryFixConfiguration
            {
                RequiredPrefix = category.RequiredPrefix,
                PrefixFixScope = category.PrefixFixScope
            };
        }

        return configuration;
    }
}

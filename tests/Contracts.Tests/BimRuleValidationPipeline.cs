using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;

namespace BIMCapabilities.Contracts.Tests;

/// <summary>
/// Test-only orchestration of the complete BIMRule validation pipeline.
/// </summary>
internal sealed class BimRuleValidationPipeline
{
    private readonly BimRuleLoader _loader = new();
    private readonly BimRuleValidator _structureValidator = new();
    private readonly BimRuleVersionValidator _versionValidator = new();
    private readonly CapabilityCompatibilityValidator _capabilityValidator = new();
    private readonly BimRuleConfigurationValidator _configurationValidator = new();

    internal BimRulePipelineResult Validate(string fixturePath)
    {
        var loadResult = _loader.Load(fixturePath);
        if (!loadResult.Success || loadResult.Rule is null)
        {
            return new BimRulePipelineResult
            {
                LoadResult = loadResult
            };
        }

        var rule = loadResult.Rule;
        var structureResult = _structureValidator.Validate(rule);
        var versionResult = _versionValidator.Validate(rule);
        var capabilityResult = MergeCapabilityResults(
            _capabilityValidator.Validate(rule),
            _configurationValidator.Validate(rule));

        return new BimRulePipelineResult
        {
            LoadResult = loadResult,
            StructureResult = structureResult,
            VersionResult = versionResult,
            CapabilityResult = capabilityResult
        };
    }

    private static CapabilityValidationResult MergeCapabilityResults(
        CapabilityValidationResult first,
        CapabilityValidationResult second)
    {
        return new CapabilityValidationResult
        {
            Diagnostics = first.Diagnostics.Concat(second.Diagnostics).ToArray()
        };
    }
}

internal sealed record BimRulePipelineResult
{
    public required BimRuleLoadResult LoadResult { get; init; }

    public BimRuleValidationResult? StructureResult { get; init; }

    public VersionValidationResult? VersionResult { get; init; }

    public CapabilityValidationResult? CapabilityResult { get; init; }

    public bool LoadSucceeded => LoadResult.Success;

    public bool IsFullyValid =>
        LoadSucceeded &&
        StructureResult?.IsValid == true &&
        VersionResult?.IsValid == true &&
        CapabilityResult?.IsValid == true;

    public IReadOnlyList<BimRulePipelineDiagnostic> GetAggregatedDiagnostics()
    {
        var diagnostics = new List<BimRulePipelineDiagnostic>();

        foreach (var diagnostic in LoadResult.Diagnostics)
        {
            diagnostics.Add(new BimRulePipelineDiagnostic("Load", diagnostic.Code, diagnostic.Message));
        }

        if (StructureResult is not null)
        {
            foreach (var diagnostic in StructureResult.Diagnostics)
            {
                diagnostics.Add(new BimRulePipelineDiagnostic("Structure", diagnostic.Code, diagnostic.Message));
            }
        }

        if (VersionResult is not null)
        {
            foreach (var diagnostic in VersionResult.Diagnostics)
            {
                diagnostics.Add(new BimRulePipelineDiagnostic("Version", diagnostic.Code, diagnostic.Message));
            }
        }

        if (CapabilityResult is not null)
        {
            foreach (var diagnostic in CapabilityResult.Diagnostics)
            {
                diagnostics.Add(new BimRulePipelineDiagnostic("Capability", diagnostic.Code, diagnostic.Message));
            }
        }

        return diagnostics
            .OrderBy(diagnostic => diagnostic.Stage, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed record BimRulePipelineDiagnostic(string Stage, string Code, string Message);

internal static class BimRuleFixturePaths
{
    internal static string Get(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }
}

using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using FamilyTargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;
using NamingComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Composition.Validation;

/// <summary>
/// Outcome of the end-to-end validation pipeline.
/// </summary>
public sealed record ValidationPipelineResult
{
    public required BimRuleLoadResult LoadResult { get; init; }

    public BimRuleValidationResult? StructureValidation { get; init; }

    public VersionValidationResult? VersionValidation { get; init; }

    public CapabilityValidationResult? CapabilityValidation { get; init; }

    public bool RuleValidationSucceeded { get; init; }

    public ExecutionPlan? Plan { get; init; }

    public ExecutionResult? ExecutionResult { get; init; }

    public FamilyTargetSetContracts.FamilyTargetSetResult? DoorTargetSetResult { get; init; }

    public FamilyTargetSetContracts.FamilyTargetSetResult? WindowTargetSetResult { get; init; }

    public ComplianceContracts.ParameterComplianceResult? DoorParameterResult { get; init; }

    public ComplianceContracts.ParameterComplianceResult? WindowParameterResult { get; init; }

    public NamingComplianceContracts.NamingComplianceResult? DoorNamingResult { get; init; }

    public NamingComplianceContracts.NamingComplianceResult? WindowNamingResult { get; init; }

    public ReportOutput? ReportOutput { get; init; }

    public HtmlRenderResult? HtmlReport { get; init; }

    public JsonRenderResult? JsonReport { get; init; }
}

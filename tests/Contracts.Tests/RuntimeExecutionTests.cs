using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Contracts.Tests;

public class RuntimeExecutionTests
{
    private static readonly JsonSerializerOptions JsonOptions = ExecutionSerialization.Options;

    [Fact]
    public void Runtime_execution_contracts_are_data_only_types()
    {
        var runtimeExecutionTypes = new[]
        {
            typeof(ExecutionPlan),
            typeof(ExecutionStep),
            typeof(ExecutionResult),
            typeof(ExecutionSummary),
            typeof(ExecutionCorrelation)
        };

        Assert.All(runtimeExecutionTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void ExecutionPlan_can_be_constructed_with_required_properties()
    {
        var plan = RuntimeExecutionTestData.CreateDemoPlan();

        Assert.Equal("plan-001", plan.PlanId);
        Assert.Equal("STD-ARC-OPENINGS-V01", plan.RuleId);
        Assert.Equal("V01", plan.RuleVersion);
        Assert.Equal(ExecutionMode.Validation, plan.Mode);
        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("naming-engine", plan.Steps[0].Configuration!["engineId"]);
        Assert.Equal("Revit", plan.Metadata!["targetPlatform"]);
    }

    [Fact]
    public void ExecutionPlan_supports_json_round_trip_serialization()
    {
        var original = RuntimeExecutionTestData.CreateDemoPlan();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ExecutionPlan>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.PlanId, roundTrip.PlanId);
        Assert.Equal(original.RuleId, roundTrip.RuleId);
        Assert.Equal(original.Mode, roundTrip.Mode);
        Assert.Equal(original.Steps.Count, roundTrip.Steps.Count);
        Assert.Equal(original.Steps[1].StepId, roundTrip.Steps[1].StepId);
        Assert.Equal(original.Metadata!["failureBehavior"], roundTrip.Metadata!["failureBehavior"]);
    }

    [Fact]
    public void ExecutionStep_required_properties_can_be_populated()
    {
        var step = new ExecutionStep
        {
            StepId = "step-003",
            Name = "Report Generation",
            StepType = "Report",
            Order = 4,
            Configuration = new Dictionary<string, string>
            {
                ["reportProfile"] = "Compliance"
            }
        };

        Assert.Equal("Report", step.StepType);
        Assert.Equal(4, step.Order);
        Assert.Equal("Compliance", step.Configuration!["reportProfile"]);
    }

    [Theory]
    [InlineData(ExecutionStatus.Pending)]
    [InlineData(ExecutionStatus.Running)]
    [InlineData(ExecutionStatus.Completed)]
    [InlineData(ExecutionStatus.Failed)]
    [InlineData(ExecutionStatus.Skipped)]
    [InlineData(ExecutionStatus.Cancelled)]
    public void ExecutionStatus_supports_required_values(ExecutionStatus status)
    {
        var summary = new ExecutionSummary
        {
            Status = status,
            TotalSteps = 1
        };

        Assert.Equal(status, summary.Status);
    }

    [Fact]
    public void ExecutionResult_can_be_constructed_with_required_properties()
    {
        var result = RuntimeExecutionTestData.CreateCompletedResult();

        Assert.Equal(ExecutionStatus.Completed, result.Status);
        Assert.NotNull(result.Diagnostics);
        Assert.NotNull(result.Evidence);
        Assert.NotNull(result.Summary);
        Assert.Equal(2, result.Summary!.CompletedSteps);
        Assert.Equal("corr-001", result.Correlation!.CorrelationId);
        Assert.Equal("plan-001", result.Correlation.PlanId);
    }

    [Fact]
    public void ExecutionResult_supports_json_round_trip_serialization()
    {
        var original = RuntimeExecutionTestData.CreateCompletedResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ExecutionResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Status, roundTrip.Status);
        Assert.Equal(original.Diagnostics!.Records.Count, roundTrip.Diagnostics!.Records.Count);
        Assert.Equal(original.Evidence!.Records.Count, roundTrip.Evidence!.Records.Count);
        Assert.Equal(original.Summary!.Message, roundTrip.Summary!.Message);
        Assert.Equal(original.Correlation!.TraceId, roundTrip.Correlation!.TraceId);
    }

    [Fact]
    public void ExecutionSummary_required_properties_can_be_populated()
    {
        var summary = new ExecutionSummary
        {
            Status = ExecutionStatus.Failed,
            StartedAt = new DateTimeOffset(2026, 6, 19, 18, 0, 0, TimeSpan.Zero),
            CompletedAt = new DateTimeOffset(2026, 6, 19, 18, 1, 0, TimeSpan.Zero),
            TotalSteps = 4,
            CompletedSteps = 2,
            FailedSteps = 1,
            SkippedSteps = 1,
            Message = "Execution stopped after unrecoverable failure."
        };

        Assert.Equal(ExecutionStatus.Failed, summary.Status);
        Assert.Equal(1, summary.FailedSteps);
        Assert.Equal(1, summary.SkippedSteps);
    }

    [Fact]
    public void ExecutionCorrelation_required_properties_can_be_populated()
    {
        var correlation = new ExecutionCorrelation
        {
            CorrelationId = "corr-003",
            ParentCorrelationId = "parent-corr-003",
            TraceId = "trace-003",
            PlanId = "plan-003"
        };

        Assert.Equal("corr-003", correlation.CorrelationId);
        Assert.Equal("plan-003", correlation.PlanId);
    }

    [Fact]
    public void Runtime_execution_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ExecutionPlan).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void ExecutionPlan_steps_are_ordered()
    {
        var plan = RuntimeExecutionTestData.CreateDemoPlan();

        Assert.True(plan.Steps[0].Order < plan.Steps[1].Order);
        Assert.Equal("step-001", plan.Steps[0].StepId);
        Assert.Equal("step-002", plan.Steps[1].StepId);
    }
}

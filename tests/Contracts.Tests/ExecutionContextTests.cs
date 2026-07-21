using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules;
using ExecutionContextContract = BIMCapabilities.Contracts.Execution.ExecutionContext;

namespace BIMCapabilities.Contracts.Tests;

public class ExecutionContextTests
{
    private static readonly JsonSerializerOptions JsonOptions = ExecutionSerialization.Options;

    [Fact]
    public void Execution_contracts_are_data_only_types()
    {
        var executionTypes = typeof(ExecutionContextContract).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ExecutionContextContract).Namespace);

        Assert.All(executionTypes, type =>
        {
            if (type == typeof(ExecutionMode))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void ExecutionContext_can_be_constructed_with_required_properties()
    {
        var context = ExecutionContextTestData.CreateDemoContext();

        Assert.Equal("STD-ARC-OPENINGS-V01", context.Rule.Metadata.RuleId);
        Assert.Equal(@"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule", context.RuleSourcePath);
        Assert.Equal(ExecutionMode.Validation, context.Request.Mode);
        Assert.Equal("Category", context.Scope.ScopeType);
        Assert.Equal("All Doors", context.Scope.TargetDescription);
        Assert.Equal("Revit", context.Environment.Platform);
        Assert.Equal("corr-001", context.CorrelationId);
        Assert.Equal("parent-corr-001", context.ParentCorrelationId);
        Assert.Equal("trace-001", context.TraceId);
    }

    [Fact]
    public void ExecutionContext_supports_json_round_trip_serialization()
    {
        var original = ExecutionContextTestData.CreateDemoContext();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ExecutionContextContract>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Rule.Metadata.RuleId, roundTrip.Rule.Metadata.RuleId);
        Assert.Equal(original.RuleSourcePath, roundTrip.RuleSourcePath);
        Assert.Equal(original.Request.Mode, roundTrip.Request.Mode);
        Assert.Equal(original.Request.RequestedBy, roundTrip.Request.RequestedBy);
        Assert.Equal(original.Scope.ScopeType, roundTrip.Scope.ScopeType);
        Assert.Equal(original.Scope.TargetDescription, roundTrip.Scope.TargetDescription);
        Assert.Equal(original.Scope.Criteria!["category"], roundTrip.Scope.Criteria!["category"]);
        Assert.Equal(original.Environment.Platform, roundTrip.Environment.Platform);
        Assert.Equal(original.Environment.ModelName, roundTrip.Environment.ModelName);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.TraceId, roundTrip.TraceId);
    }

    [Theory]
    [InlineData(ExecutionMode.Validation)]
    [InlineData(ExecutionMode.Fix)]
    [InlineData(ExecutionMode.Review)]
    public void ExecutionMode_supports_required_execution_modes(ExecutionMode mode)
    {
        var request = new ExecutionRequest
        {
            Mode = mode,
            RequestedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal(mode, request.Mode);
    }

    [Fact]
    public void ExecutionRequest_required_properties_can_be_populated()
    {
        var request = new ExecutionRequest
        {
            Mode = ExecutionMode.Fix,
            DryRun = true,
            RequireUserApprovalBeforeModification = true,
            RequestedAt = new DateTimeOffset(2026, 6, 19, 8, 30, 0, TimeSpan.Zero),
            RequestedBy = "Reviewer"
        };

        Assert.Equal(ExecutionMode.Fix, request.Mode);
        Assert.True(request.DryRun);
        Assert.True(request.RequireUserApprovalBeforeModification);
        Assert.Equal("Reviewer", request.RequestedBy);
    }

    [Fact]
    public void ExecutionScope_required_properties_can_be_populated()
    {
        var scope = new ExecutionScope
        {
            ScopeType = "Selection",
            TargetDescription = "Current Selection"
        };

        Assert.Equal("Selection", scope.ScopeType);
        Assert.Equal("Current Selection", scope.TargetDescription);
        Assert.Null(scope.Criteria);
    }

    [Fact]
    public void ExecutionEnvironment_required_properties_can_be_populated()
    {
        var environment = new ExecutionEnvironment
        {
            Platform = "Revit",
            PlatformVersion = "2026",
            SessionId = "session-002",
            ModelIdentifier = "model-002",
            ModelName = "Project A.rvt"
        };

        Assert.Equal("Revit", environment.Platform);
        Assert.Equal("2026", environment.PlatformVersion);
        Assert.Equal("Project A.rvt", environment.ModelName);
    }

    [Fact]
    public void ExecutionContext_does_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ExecutionContextContract).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}

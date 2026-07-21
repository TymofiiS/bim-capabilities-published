using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Contracts.Tests;

public class DiagnosticTests
{
    private static readonly JsonSerializerOptions JsonOptions = DiagnosticSerialization.Options;

    [Fact]
    public void Diagnostic_contracts_are_data_only_types()
    {
        var diagnosticTypes = typeof(DiagnosticRecord).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(DiagnosticRecord).Namespace);

        Assert.All(diagnosticTypes, type =>
        {
            if (type == typeof(DiagnosticSeverity) || type == typeof(DiagnosticCategory))
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
    public void DiagnosticRecord_can_be_constructed_with_required_properties()
    {
        var record = DiagnosticTestData.CreateRuntimeExecutionFailure();

        Assert.Equal("diagnostic-001", record.DiagnosticId);
        Assert.Equal("Runtime", record.Source.ComponentType);
        Assert.Equal(DiagnosticCategory.Execution, record.Category);
        Assert.Equal(DiagnosticSeverity.Error, record.Severity);
        Assert.Equal("parameter-engine", record.StructuredMetadata!["engineId"]);
        Assert.Equal("STD-ARC-OPENINGS-V01", record.Context!.RuleId);
        Assert.Equal(ExecutionMode.Validation, record.Context.ExecutionMode);
        Assert.Equal("corr-001", record.CorrelationId);
        Assert.Equal("trace-001", record.TraceId);
    }

    [Fact]
    public void DiagnosticRecord_supports_json_round_trip_serialization()
    {
        var original = DiagnosticTestData.CreateRuntimeExecutionFailure();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<DiagnosticRecord>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.DiagnosticId, roundTrip.DiagnosticId);
        Assert.Equal(original.Source.Code, roundTrip.Source.Code);
        Assert.Equal(original.Category, roundTrip.Category);
        Assert.Equal(original.Severity, roundTrip.Severity);
        Assert.Equal(original.Message, roundTrip.Message);
        Assert.Equal(original.StructuredMetadata!["failureReason"], roundTrip.StructuredMetadata!["failureReason"]);
        Assert.Equal(original.Context!.EngineId, roundTrip.Context!.EngineId);
        Assert.Equal(original.Context.ExecutionMode, roundTrip.Context.ExecutionMode);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void DiagnosticCollection_can_hold_multiple_records()
    {
        var collection = DiagnosticTestData.CreateDemoCollection();

        Assert.Equal("diagnostics-001", collection.CollectionId);
        Assert.Equal("corr-001", collection.CorrelationId);
        Assert.Equal(2, collection.Records.Count);
        Assert.Contains(collection.Records, record => record.Category == DiagnosticCategory.Execution);
        Assert.Contains(collection.Records, record => record.Category == DiagnosticCategory.Launcher);
    }

    [Fact]
    public void DiagnosticCollection_supports_json_round_trip_serialization()
    {
        var original = DiagnosticTestData.CreateDemoCollection();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<DiagnosticCollection>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.CollectionId, roundTrip.CollectionId);
        Assert.Equal(original.Records.Count, roundTrip.Records.Count);
        Assert.Equal(original.Records[0].DiagnosticId, roundTrip.Records[0].DiagnosticId);
        Assert.Equal(original.Records[1].Severity, roundTrip.Records[1].Severity);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Trace)]
    [InlineData(DiagnosticSeverity.Information)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Error)]
    [InlineData(DiagnosticSeverity.Critical)]
    public void DiagnosticSeverity_supports_required_values(DiagnosticSeverity severity)
    {
        var record = new DiagnosticRecord
        {
            DiagnosticId = "severity-test",
            Source = new DiagnosticSource { ComponentType = "System" },
            Category = DiagnosticCategory.System,
            Severity = severity,
            Message = "Severity test."
        };

        Assert.Equal(severity, record.Severity);
    }

    [Theory]
    [InlineData(DiagnosticCategory.Runtime)]
    [InlineData(DiagnosticCategory.Configuration)]
    [InlineData(DiagnosticCategory.Validation)]
    [InlineData(DiagnosticCategory.Execution)]
    [InlineData(DiagnosticCategory.Adapter)]
    [InlineData(DiagnosticCategory.Launcher)]
    [InlineData(DiagnosticCategory.Report)]
    [InlineData(DiagnosticCategory.System)]
    public void DiagnosticCategory_supports_required_values(DiagnosticCategory category)
    {
        var record = new DiagnosticRecord
        {
            DiagnosticId = "category-test",
            Source = new DiagnosticSource { ComponentType = category.ToString() },
            Category = category,
            Severity = DiagnosticSeverity.Information,
            Message = "Category test."
        };

        Assert.Equal(category, record.Category);
    }

    [Fact]
    public void DiagnosticContext_required_properties_can_be_populated()
    {
        var context = new DiagnosticContext
        {
            RuleId = "STD-ARC-OPENINGS-V01",
            RuleSourcePath = @"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule",
            ExecutionMode = ExecutionMode.Fix,
            EngineId = "naming-engine",
            CapabilityId = "naming.prefix.validation",
            CorrelationId = "corr-002",
            ParentCorrelationId = "parent-corr-002",
            TraceId = "trace-002"
        };

        Assert.Equal("STD-ARC-OPENINGS-V01", context.RuleId);
        Assert.Equal(ExecutionMode.Fix, context.ExecutionMode);
        Assert.Equal("naming-engine", context.EngineId);
        Assert.Equal("trace-002", context.TraceId);
    }

    [Fact]
    public void DiagnosticSource_required_properties_can_be_populated()
    {
        var source = new DiagnosticSource
        {
            ComponentType = "Adapter",
            ComponentId = "revit-adapter",
            Operation = "ReadParameters",
            Code = "UnsupportedCapability"
        };

        Assert.Equal("Adapter", source.ComponentType);
        Assert.Equal("revit-adapter", source.ComponentId);
        Assert.Equal("UnsupportedCapability", source.Code);
    }

    [Fact]
    public void Diagnostic_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(DiagnosticRecord).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}

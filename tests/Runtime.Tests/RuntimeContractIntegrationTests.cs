using System.Reflection;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Runtime.Context;
using BIMCapabilities.Runtime.Diagnostics;
using BIMCapabilities.Runtime.Evidence;
using BIMCapabilities.Runtime.Execution;
using BIMCapabilities.Runtime.Registration;

namespace BIMCapabilities.Runtime.Tests;

public class RuntimeContractIntegrationTests
{
    [Fact]
    public void Runtime_skeleton_composes_all_required_services()
    {
        var runtime = new RuntimeSkeleton();

        Assert.IsAssignableFrom<IRuntimeContext>(runtime.Context);
        Assert.IsAssignableFrom<IRuntimeExecution>(runtime.Execution);
        Assert.IsAssignableFrom<IRuntimeRegistry>(runtime.Registry);
        Assert.IsAssignableFrom<IRuntimeDiagnostics>(runtime.Diagnostics);
        Assert.IsAssignableFrom<IRuntimeEvidence>(runtime.Evidence);
    }

    [Fact]
    public void Execution_context_integration_sets_current_context()
    {
        var runtime = new RuntimeSkeleton();
        var context = RuntimeIntegrationTestData.CreateExecutionContext();

        runtime.Context.SetContext(context);

        Assert.NotNull(runtime.Context.Current);
        Assert.Equal("STD-ARC-OPENINGS-V01", runtime.Context.Current!.Rule.Metadata.RuleId);
        Assert.Equal("corr-runtime-001", runtime.Context.Current.CorrelationId);
    }

    [Fact]
    public void Execution_plan_integration_maps_rule_engines_to_steps()
    {
        var runtime = new RuntimeSkeleton();
        var context = RuntimeIntegrationTestData.CreateExecutionContext();

        var plan = runtime.Execution.CreatePlan(context);

        Assert.Equal("STD-ARC-OPENINGS-V01", plan.RuleId);
        Assert.Equal(ExecutionMode.Validation, plan.Mode);
        Assert.Equal(4, plan.Steps.Count);
        Assert.Equal("naming-engine", plan.Steps[0].Configuration!["engineId"]);
        Assert.Equal("parameter-engine", plan.Steps[1].Configuration!["engineId"]);
        Assert.Equal("All Doors", plan.Metadata!["targetDescription"]);
    }

    [Fact]
    public void Engine_registration_integration_resolves_plan_engines()
    {
        var runtime = new RuntimeSkeleton();
        foreach (var registration in RuntimeIntegrationTestData.CreateMvpRegistrations())
        {
            runtime.Registry.Register(registration);
        }

        var plan = runtime.Execution.CreatePlan(RuntimeIntegrationTestData.CreateExecutionContext());

        foreach (var step in plan.Steps)
        {
            var engineId = step.Configuration!["engineId"];
            var registration = runtime.Registry.FindRegistration(engineId);
            Assert.NotNull(registration);
            Assert.Equal(engineId, registration!.Engine.EngineId);
        }
    }

    [Fact]
    public void Evidence_integration_collects_records_for_composed_result()
    {
        var runtime = new RuntimeSkeleton();
        runtime.Evidence.Add(RuntimeIntegrationTestData.CreateSampleEvidence());

        var context = RuntimeIntegrationTestData.CreateExecutionContext();
        var plan = runtime.Execution.CreatePlan(context);
        var result = runtime.Execution.ComposeResult(context, plan);

        Assert.NotNull(result.Evidence);
        Assert.Single(result.Evidence!.Records);
        Assert.Equal("runtime-evidence-001", result.Evidence.Records[0].EvidenceId);
    }

    [Fact]
    public void Diagnostics_integration_collects_records_for_composed_result()
    {
        var runtime = new RuntimeSkeleton();
        runtime.Diagnostics.Add(RuntimeIntegrationTestData.CreateSampleDiagnostic());

        var context = RuntimeIntegrationTestData.CreateExecutionContext();
        var plan = runtime.Execution.CreatePlan(context);
        var result = runtime.Execution.ComposeResult(context, plan);

        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Records, record => record.DiagnosticId == "runtime-diagnostic-sample-001");
        Assert.Contains(result.Diagnostics.Records, record => record.Source.Code == "RuntimeExecutionNotImplemented");
    }

    [Fact]
    public void Execution_result_integration_composes_contract_outcome()
    {
        var runtime = new RuntimeSkeleton();
        foreach (var registration in RuntimeIntegrationTestData.CreateMvpRegistrations())
        {
            runtime.Registry.Register(registration);
        }

        var context = RuntimeIntegrationTestData.CreateExecutionContext();
        runtime.Context.SetContext(context);
        var plan = runtime.Execution.CreatePlan(context);
        var result = runtime.Execution.ComposeResult(context, plan);

        Assert.Equal(ExecutionStatus.Pending, result.Status);
        Assert.Equal("plan-corr-runtime-001", result.Correlation!.PlanId);
        Assert.Equal("corr-runtime-001", result.Correlation.CorrelationId);
        Assert.Equal(4, result.Summary!.TotalSteps);
        Assert.Equal(0, result.Summary.CompletedSteps);
        Assert.Contains("not implemented", result.Summary.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void End_to_end_runtime_contract_workflow_operates_together()
    {
        var runtime = new RuntimeSkeleton();
        var context = RuntimeIntegrationTestData.CreateExecutionContext();

        runtime.Context.SetContext(context);
        foreach (var registration in RuntimeIntegrationTestData.CreateMvpRegistrations())
        {
            runtime.Registry.Register(registration);
        }

        runtime.Evidence.Add(RuntimeIntegrationTestData.CreateSampleEvidence());
        runtime.Diagnostics.Add(RuntimeIntegrationTestData.CreateSampleDiagnostic());

        var plan = runtime.Execution.CreatePlan(context);
        var result = runtime.Execution.ComposeResult(context, plan);

        Assert.Equal(context.Rule.Metadata.RuleId, plan.RuleId);
        Assert.Equal(plan.Steps.Count, runtime.Registry.Registrations.Count);
        Assert.NotNull(result.Diagnostics);
        Assert.NotNull(result.Evidence);
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.Correlation);
        Assert.DoesNotContain(result.Diagnostics!.Records, record => record.Source.Code == "EngineNotRegistered");
    }
}

public class RuntimeArchitectureTests
{
    [Fact]
    public void Runtime_assembly_does_not_reference_engine_adapter_or_launcher_projects()
    {
        var runtimeAssembly = typeof(RuntimeSkeleton).Assembly;
        var referencedAssemblies = runtimeAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Report", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_skeleton_types_do_not_execute_engines_or_adapters()
    {
        var runtimeTypes = typeof(RuntimeSkeleton).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("BIMCapabilities.Runtime", StringComparison.Ordinal) == true)
            .Where(type => type.IsClass && !type.IsAbstract);

        Assert.All(runtimeTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("ExecuteEngine", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("RunAdapter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Launch", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }
}

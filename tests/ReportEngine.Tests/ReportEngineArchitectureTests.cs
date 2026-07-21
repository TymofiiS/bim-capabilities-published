using System.Reflection;
using BIMCapabilities.Engines.Report.Profiles;

namespace BIMCapabilities.Engines.Report.Tests;

public class ReportEngineArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Family",
        "BIMCapabilities.Engines.Parameter",
        "BIMCapabilities.Engines.Naming",
        "BIMCapabilities.Adapters.Revit",
        "BIMCapabilities.Launchers.Revit"
    ];

    [Fact]
    public void Report_engine_project_references_only_contracts()
    {
        var assembly = typeof(ComplianceReportProfile).Assembly;
        var referencedProjectAssemblies = assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }

    [Fact]
    public void Report_engine_does_not_reference_forbidden_assemblies()
    {
        var assembly = typeof(ComplianceReportProfile).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Report_engine_test_project_does_not_reference_runtime_or_revit()
    {
        var assembly = typeof(ReportEngineEndToEndTests).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.Contains("BIMCapabilities.Engines.Report", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
    }

    [Fact]
    public void Report_engine_types_do_not_depend_on_runtime_execution_types()
    {
        var assembly = typeof(ComplianceReportProfile).Assembly;
        var runtimeTypeNames = new HashSet<string?>(StringComparer.Ordinal)
        {
            "BIMCapabilities.Runtime.Execution.ExecutionContext",
            "BIMCapabilities.Runtime.Execution.ExecutionResult",
            "BIMCapabilities.Runtime.Execution.ExecutionPlan"
        };

        foreach (var type in assembly.GetTypes())
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.DoesNotContain(property.PropertyType.FullName, runtimeTypeNames);
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(method.ReturnType.FullName, runtimeTypeNames);

                foreach (var parameter in method.GetParameters())
                {
                    Assert.DoesNotContain(parameter.ParameterType.FullName, runtimeTypeNames);
                }
            }
        }
    }
}

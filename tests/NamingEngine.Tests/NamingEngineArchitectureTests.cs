using System.Reflection;
using BIMCapabilities.Engines.Naming.Atoms.Pattern;
using BIMCapabilities.Engines.Naming.Atoms.Prefix;
using BIMCapabilities.Engines.Naming.Compliance;

namespace BIMCapabilities.Engines.Naming.Tests;

public class NamingEngineArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Family",
        "BIMCapabilities.Engines.Parameter",
        "BIMCapabilities.Adapters.Revit",
        "BIMCapabilities.Launchers.Revit",
        "BIMCapabilities.Engines.Report"
    ];

    [Fact]
    public void Naming_engine_project_references_only_contracts()
    {
        var assembly = typeof(NamingComplianceEngine).Assembly;
        var referencedProjectAssemblies = assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }

    [Fact]
    public void Naming_engine_does_not_reference_forbidden_assemblies()
    {
        var assembly = typeof(NamingComplianceEngine).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.DoesNotContain(
            referencedAssemblies,
            name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Naming_engine_test_project_does_not_reference_runtime_family_parameter_revit_or_report()
    {
        var assembly = typeof(NamingEngineEndToEndTests).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.Contains("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
    }

    [Fact]
    public void Naming_engine_types_do_not_depend_on_runtime_execution_types()
    {
        var assembly = typeof(NamingComplianceEngine).Assembly;
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

    [Fact]
    public void Naming_engine_public_surface_does_not_expose_correction_or_renaming()
    {
        var engineTypes = new[]
        {
            typeof(NamingComplianceEngine),
            typeof(PrefixValidationAtom),
            typeof(NamingPatternValidationAtom)
        };

        Assert.All(engineTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Rename", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }
}

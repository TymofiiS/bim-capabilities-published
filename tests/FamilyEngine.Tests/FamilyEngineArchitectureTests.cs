using System.Reflection;
using BIMCapabilities.Engines.Family;

namespace BIMCapabilities.Engines.Family.Tests;

public class FamilyEngineArchitectureTests
{
    [Fact]
    public void Family_engine_project_does_not_contain_selection_or_filtering_implementation()
    {
        var engineAssembly = Assembly.Load("BIMCapabilities.Engines.Family");
        var engineTypes = engineAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("BIMCapabilities.Engines.Family", StringComparison.Ordinal) == true)
            .Where(type => type.Namespace != "BIMCapabilities.Engines.Family.Atoms.Discovery")
            .Where(type => type.Namespace != "BIMCapabilities.Engines.Family.Atoms.Selection")
            .Where(type => type.Namespace != "BIMCapabilities.Engines.Family.Atoms.Filtering")
            .Where(type => type.Namespace != "BIMCapabilities.Engines.Family.Atoms.ImportedCad")
            .Where(type => type.Namespace != "BIMCapabilities.Engines.Family.Generation")
            .Where(type => type.IsClass && !type.IsAbstract)
            .ToArray();

        Assert.All(engineTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Select", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Filter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ImportedCad", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Family_engine_project_references_only_contracts()
    {
        var engineAssembly = typeof(FamilyEngineAssembly).Assembly;

        var referencedProjectAssemblies = engineAssembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }
}

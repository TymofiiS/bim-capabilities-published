using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Contracts.Tests;

public class RevitAdapterCompositionContractTests
{
    [Fact]
    public void IRevitAdapter_extends_read_adapter_and_defines_read_workflow()
    {
        Assert.True(typeof(IRevitReadAdapter).IsAssignableFrom(typeof(IRevitAdapter)));

        var readMethod = Assert.Single(typeof(IRevitAdapter).GetMethods(), method => method.Name == "Read");
        Assert.Equal(typeof(RevitAdapterReadResult), readMethod.ReturnType);
        Assert.Equal(typeof(RevitAdapterReadContext), readMethod.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void Revit_adapter_read_composition_contracts_are_data_only_types()
    {
        var compositionTypes = new[]
        {
            typeof(RevitAdapterReadContext),
            typeof(RevitAdapterReadResult),
            typeof(RevitAdapterStatistics),
            typeof(RevitAdapterReadMetadata),
            typeof(RevitAdapterReadDiagnostic)
        };

        Assert.All(compositionTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void Revit_adapter_read_composition_contracts_do_not_reference_adapter_or_engine_assemblies()
    {
        var contractsAssembly = typeof(RevitAdapterReadContext).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}

using System.Reflection;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class AdapterProjectTests
{
    [Fact]
    public void Revit_adapter_project_is_referenced()
    {
        var adapterAssembly = Assembly.Load("BIMCapabilities.Adapters.Revit");

        Assert.Equal("BIMCapabilities.Adapters.Revit", adapterAssembly.GetName().Name);
    }

    [Fact]
    public void Contracts_project_is_referenced()
    {
        var contractsAssembly = Assembly.Load("BIMCapabilities.Contracts");

        Assert.Equal("BIMCapabilities.Contracts", contractsAssembly.GetName().Name);
    }
}

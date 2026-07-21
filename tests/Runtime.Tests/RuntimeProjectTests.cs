using System.Reflection;

namespace BIMCapabilities.Runtime.Tests;

public class RuntimeProjectTests
{
    [Fact]
    public void Runtime_project_is_referenced()
    {
        var runtimeAssembly = Assembly.Load("BIMCapabilities.Runtime");

        Assert.Equal("BIMCapabilities.Runtime", runtimeAssembly.GetName().Name);
    }

    [Fact]
    public void Contracts_project_is_referenced()
    {
        var contractsAssembly = Assembly.Load("BIMCapabilities.Contracts");

        Assert.Equal("BIMCapabilities.Contracts", contractsAssembly.GetName().Name);
    }
}

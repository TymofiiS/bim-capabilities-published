using System.Reflection;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class EngineProjectTests
{
    [Fact]
    public void Engine_project_is_referenced()
    {
        var engineAssembly = Assembly.Load("BIMCapabilities.Engines.Parameter");

        Assert.Equal("BIMCapabilities.Engines.Parameter", engineAssembly.GetName().Name);
    }

    [Fact]
    public void Contracts_project_is_referenced()
    {
        var contractsAssembly = Assembly.Load("BIMCapabilities.Contracts");

        Assert.Equal("BIMCapabilities.Contracts", contractsAssembly.GetName().Name);
    }
}

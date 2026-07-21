using System.Reflection;

namespace BIMCapabilities.Engines.Naming.Tests;

public class EngineProjectTests
{
    [Fact]
    public void Engine_project_is_referenced()
    {
        var engineAssembly = Assembly.Load("BIMCapabilities.Engines.Naming");

        Assert.Equal("BIMCapabilities.Engines.Naming", engineAssembly.GetName().Name);
    }

    [Fact]
    public void Contracts_project_is_referenced()
    {
        var contractsAssembly = Assembly.Load("BIMCapabilities.Contracts");

        Assert.Equal("BIMCapabilities.Contracts", contractsAssembly.GetName().Name);
    }
}

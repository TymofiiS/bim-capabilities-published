using System.Reflection;

namespace BIMCapabilities.Contracts.Tests;

public class ContractsProjectTests
{
    [Fact]
    public void Contracts_project_is_referenced()
    {
        var contractsAssembly = Assembly.Load("BIMCapabilities.Contracts");

        Assert.Equal("BIMCapabilities.Contracts", contractsAssembly.GetName().Name);
    }
}

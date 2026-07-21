using System.Reflection;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Contracts.Tests;

public class NamingWriteRequestBuilderContractTests
{
    [Fact]
    public void Naming_write_request_builder_contracts_are_data_only_types()
    {
        var builderTypes = typeof(NamingWriteRequestBuildRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(NamingWriteRequestBuildRequest).Namespace)
            .Where(type => type != typeof(INamingWriteRequestBuilder))
            .Where(type => !type.IsEnum);

        Assert.All(builderTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void INamingWriteRequestBuilder_defines_build_contract()
    {
        var method = Assert.Single(
            typeof(INamingWriteRequestBuilder).GetMethods(),
            candidate => candidate.Name == nameof(INamingWriteRequestBuilder.Build));

        Assert.Equal(typeof(NamingWriteRequestBuildResult), method.ReturnType);
        Assert.Equal(typeof(NamingWriteRequestBuildRequest), method.GetParameters()[0].ParameterType);
    }
}

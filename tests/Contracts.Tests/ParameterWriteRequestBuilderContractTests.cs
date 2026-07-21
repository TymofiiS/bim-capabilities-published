using System.Reflection;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterWriteRequestBuilderContractTests
{
    [Fact]
    public void Parameter_write_request_builder_contracts_are_data_only_types()
    {
        var builderTypes = typeof(ParameterWriteRequestBuildRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ParameterWriteRequestBuildRequest).Namespace)
            .Where(type => type != typeof(IParameterWriteRequestBuilder))
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
    public void IParameterWriteRequestBuilder_defines_build_contract()
    {
        var method = Assert.Single(
            typeof(IParameterWriteRequestBuilder).GetMethods(),
            candidate => candidate.Name == nameof(IParameterWriteRequestBuilder.Build));

        Assert.Equal(typeof(ParameterWriteRequestBuildResult), method.ReturnType);
        Assert.Equal(typeof(ParameterWriteRequestBuildRequest), method.GetParameters()[0].ParameterType);
    }
}

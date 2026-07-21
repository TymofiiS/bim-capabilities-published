using BIMCapabilities.Composition.Capabilities.Handlers;

namespace BIMCapabilities.Integration.Tests;

public class ParameterBindingConfigurationTests
{
    [Theory]
    [InlineData("FireRating=type", "FireRating", false)]
    [InlineData("FireRating=instance", "FireRating", true)]
    [InlineData("FireRating=TYPE,RoomMarker=instance", "RoomMarker", true)]
    public void ParseParameterBindings_reads_type_and_instance_tokens(string value, string parameterName, bool expectedIsInstance)
    {
        var bindings = ParameterExistenceCapabilityHandler.ParseParameterBindings(value);

        Assert.True(bindings.TryGetValue(parameterName, out var isInstance));
        Assert.Equal(expectedIsInstance, isInstance);
    }

    [Fact]
    public void ParseParameterBindings_defaults_missing_parameters_to_type_at_fix_time()
    {
        var bindings = ParameterExistenceCapabilityHandler.ParseParameterBindings("FireRating=type");

        Assert.False(bindings["FireRating"]);
        Assert.False(ParameterWriteRequestBuilderSupportResolveIsInstance("RoomName", bindings));
    }

    [Fact]
    public void ParseParameterBindings_rejects_unknown_binding_values()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ParameterExistenceCapabilityHandler.ParseParameterBindings("FireRating=wrong"));

        Assert.Contains("type", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("instance", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParameterWriteRequestBuilderSupportResolveIsInstance(
        string parameterName,
        IReadOnlyDictionary<string, bool> bindings)
    {
        return bindings.TryGetValue(parameterName, out var isInstance) && isInstance;
    }
}

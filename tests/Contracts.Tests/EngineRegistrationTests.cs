using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Registration;

namespace BIMCapabilities.Contracts.Tests;

public class EngineRegistrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = EngineRegistrationSerialization.Options;

    [Fact]
    public void Engine_registration_contracts_are_data_only_types()
    {
        var registrationTypes = typeof(EngineDefinition).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(EngineDefinition).Namespace);

        Assert.All(registrationTypes, type =>
        {
            if (type == typeof(EngineType))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void EngineDefinition_can_be_constructed_with_required_properties()
    {
        var definition = EngineRegistrationTestData.CreateParameterEngineDefinition();

        Assert.Equal("parameter-engine", definition.EngineId);
        Assert.Equal("Parameter Engine", definition.Name);
        Assert.Equal(EngineType.Parameter, definition.EngineType);
        Assert.Equal("1.0", definition.Version.Version);
        Assert.Single(definition.Capabilities);
        Assert.Equal("parameter.existence", definition.Capabilities[0].CapabilityName);
        Assert.Equal("BIMCapabilities", definition.Metadata!["publisher"]);
    }

    [Fact]
    public void EngineDefinition_supports_json_round_trip_serialization()
    {
        var original = EngineRegistrationTestData.CreateParameterEngineDefinition();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EngineDefinition>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.EngineId, roundTrip.EngineId);
        Assert.Equal(original.EngineType, roundTrip.EngineType);
        Assert.Equal(original.Version.ConfigurationSchemaVersion, roundTrip.Version.ConfigurationSchemaVersion);
        Assert.Equal(original.Capabilities[0].CapabilityCategory, roundTrip.Capabilities[0].CapabilityCategory);
    }

    [Fact]
    public void EngineCapability_required_properties_can_be_populated()
    {
        var capability = new EngineCapability
        {
            CapabilityName = "naming.prefix.validation",
            CapabilityVersion = "1.0",
            Description = "Validates naming prefix conventions.",
            CapabilityCategory = "Validation"
        };

        Assert.Equal("naming.prefix.validation", capability.CapabilityName);
        Assert.Equal("Validation", capability.CapabilityCategory);
    }

    [Fact]
    public void EngineDescriptor_required_properties_can_be_populated()
    {
        var descriptor = EngineRegistrationTestData.CreateNamingEngineDescriptor();

        Assert.Equal("naming-engine", descriptor.EngineId);
        Assert.Equal(EngineType.Naming, descriptor.EngineType);
        Assert.Equal("Validates naming conventions.", descriptor.Description);
    }

    [Fact]
    public void EngineRegistration_can_be_constructed_with_required_properties()
    {
        var registration = EngineRegistrationTestData.CreateDemoRegistration();

        Assert.Equal("parameter-engine", registration.Engine.EngineId);
        Assert.Equal("Runtime", registration.RegistrationMetadata!["registeredBy"]);
        Assert.Equal(new DateTimeOffset(2026, 6, 19, 18, 0, 0, TimeSpan.Zero), registration.RegisteredAt);
    }

    [Fact]
    public void EngineRegistration_supports_json_round_trip_serialization()
    {
        var original = EngineRegistrationTestData.CreateDemoRegistration();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EngineRegistration>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Engine.EngineId, roundTrip.Engine.EngineId);
        Assert.Equal(original.RegisteredAt, roundTrip.RegisteredAt);
        Assert.Equal(original.RegistrationMetadata!["registrationSource"], roundTrip.RegistrationMetadata!["registrationSource"]);
    }

    [Theory]
    [InlineData(EngineType.Family)]
    [InlineData(EngineType.Parameter)]
    [InlineData(EngineType.Naming)]
    [InlineData(EngineType.Report)]
    [InlineData(EngineType.Custom)]
    public void EngineType_supports_required_values(EngineType engineType)
    {
        var definition = new EngineDefinition
        {
            EngineId = "custom-engine",
            Name = "Custom Engine",
            EngineType = engineType,
            Version = EngineRegistrationTestData.CreateDefaultVersion(),
            Capabilities = []
        };

        Assert.Equal(engineType, definition.EngineType);
    }

    [Fact]
    public void EngineVersion_required_properties_can_be_populated()
    {
        var version = new EngineVersion
        {
            Version = "2.1",
            ConfigurationSchemaVersion = "2.0",
            RuntimeCompatibilityVersion = "1.0"
        };

        Assert.Equal("2.1", version.Version);
        Assert.Equal("2.0", version.ConfigurationSchemaVersion);
    }

    [Fact]
    public void Mvp_engine_catalog_contains_four_engines()
    {
        var catalog = EngineRegistrationTestData.CreateMvpEngineCatalog();

        Assert.Equal(4, catalog.Count);
        Assert.Contains(catalog, engine => engine.EngineType == EngineType.Parameter);
        Assert.Contains(catalog, engine => engine.EngineType == EngineType.Naming);
        Assert.Contains(catalog, engine => engine.EngineType == EngineType.Family);
        Assert.Contains(catalog, engine => engine.EngineType == EngineType.Report);
    }

    [Fact]
    public void Engine_registration_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(EngineDefinition).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}

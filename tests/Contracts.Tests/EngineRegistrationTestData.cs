using BIMCapabilities.Contracts.Engines.Registration;

namespace BIMCapabilities.Contracts.Tests;

internal static class EngineRegistrationTestData
{
    internal static EngineVersion CreateDefaultVersion()
    {
        return new EngineVersion
        {
            Version = "1.0",
            ConfigurationSchemaVersion = "1.0",
            RuntimeCompatibilityVersion = "1.0"
        };
    }

    internal static EngineDefinition CreateParameterEngineDefinition()
    {
        return new EngineDefinition
        {
            EngineId = "parameter-engine",
            Name = "Parameter Engine",
            EngineType = EngineType.Parameter,
            Version = CreateDefaultVersion(),
            Metadata = new Dictionary<string, string>
            {
                ["publisher"] = "BIMCapabilities"
            },
            Capabilities =
            [
                new EngineCapability
                {
                    CapabilityName = "parameter.existence",
                    CapabilityVersion = "1.0",
                    Description = "Validates that required parameters exist.",
                    CapabilityCategory = "Validation"
                }
            ]
        };
    }

    internal static EngineDescriptor CreateNamingEngineDescriptor()
    {
        return new EngineDescriptor
        {
            EngineId = "naming-engine",
            Name = "Naming Engine",
            EngineType = EngineType.Naming,
            Version = CreateDefaultVersion(),
            Description = "Validates naming conventions."
        };
    }

    internal static EngineRegistration CreateDemoRegistration()
    {
        return new EngineRegistration
        {
            Engine = CreateParameterEngineDefinition(),
            RegisteredAt = new DateTimeOffset(2026, 6, 19, 18, 0, 0, TimeSpan.Zero),
            RegistrationMetadata = new Dictionary<string, string>
            {
                ["registeredBy"] = "Runtime",
                ["registrationSource"] = "MvpCatalog"
            }
        };
    }

    internal static IReadOnlyList<EngineDefinition> CreateMvpEngineCatalog()
    {
        return
        [
            CreateParameterEngineDefinition(),
            new EngineDefinition
            {
                EngineId = "naming-engine",
                Name = "Naming Engine",
                EngineType = EngineType.Naming,
                Version = CreateDefaultVersion(),
                Capabilities =
                [
                    new EngineCapability
                    {
                        CapabilityName = "naming.prefix.validation",
                        CapabilityVersion = "1.0",
                        CapabilityCategory = "Validation"
                    }
                ]
            },
            new EngineDefinition
            {
                EngineId = "family-engine",
                Name = "Family Engine",
                EngineType = EngineType.Family,
                Version = CreateDefaultVersion(),
                Capabilities =
                [
                    new EngineCapability
                    {
                        CapabilityName = "family.imported-cad",
                        CapabilityVersion = "1.0",
                        CapabilityCategory = "Validation"
                    }
                ]
            },
            new EngineDefinition
            {
                EngineId = "report-engine",
                Name = "Report Engine",
                EngineType = EngineType.Report,
                Version = CreateDefaultVersion(),
                Capabilities =
                [
                    new EngineCapability
                    {
                        CapabilityName = "report.compliance",
                        CapabilityVersion = "1.0",
                        CapabilityCategory = "Report"
                    }
                ]
            }
        ];
    }
}

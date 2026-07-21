namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Canonical capability catalog definitions for the current BIMCapabilities implementation.
/// </summary>
public static class CapabilityCatalogDefinitions
{
    public static IReadOnlyList<CapabilityDefinition> All { get; } =
    [
        new CapabilityDefinition
        {
            EngineId = "naming-engine",
            CapabilityId = "naming.prefix.validation",
            DisplayName = "Naming Prefix Validation",
            Description = "Validates that family type names in configured categories use the required prefix pattern.",
            Status = CapabilityCompatibilityStatus.Supported,
            HandlerId = CapabilityHandlerIds.NamingPrefixValidation,
            ImplementationAtomId = "naming.validation.prefix",
            ConfigurationSchema = new CapabilityConfigurationSchema
            {
                Keys =
                [
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.prefix",
                        Description = "Required naming prefix for the category (for example Doors.prefix = DR_).",
                        Required = true
                    },
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.prefixFix",
                        Description = "Automatic prefix correction scope during fix: type, family, or both.",
                        Required = false
                    }
                ]
            },
            Examples =
            [
                new CapabilityExample
                {
                    Description = "Require DR_ prefix on door family types.",
                    Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Doors.prefix"] = "DR_"
                    }
                }
            ]
        },
        new CapabilityDefinition
        {
            EngineId = "naming-engine",
            CapabilityId = "naming.prefix.legacy",
            DisplayName = "Naming Prefix Validation (Legacy)",
            Description = "Deprecated naming prefix validation capability. Use naming.prefix.validation instead.",
            Status = CapabilityCompatibilityStatus.Deprecated,
            HandlerId = CapabilityHandlerIds.NamingPrefixValidation,
            ReplacementCapabilityId = "naming.prefix.validation",
            ConfigurationSchema = new CapabilityConfigurationSchema
            {
                Keys =
                [
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.prefix",
                        Description = "Required naming prefix for the category.",
                        Required = true
                    }
                ]
            }
        },
        new CapabilityDefinition
        {
            EngineId = "parameter-engine",
            CapabilityId = "parameter.existence",
            DisplayName = "Parameter Existence",
            Description = "Validates that configured parameters exist and have values on family types in each category.",
            Status = CapabilityCompatibilityStatus.Supported,
            HandlerId = CapabilityHandlerIds.ParameterExistence,
            ImplementationAtomId = "parameter.validation.existence",
            ConfigurationSchema = new CapabilityConfigurationSchema
            {
                Keys =
                [
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.parameters",
                        Description = "Comma-separated required parameter names for the category.",
                        Required = true
                    },
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.parameterDefaults",
                        Description = "Comma-separated literal default values for validation and fix (for example FireRating=EI60,RoomName=Undefined). Placeholders and expressions are not supported.",
                        Required = false
                    },
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.parameterFillRules",
                        Description = "Comma-separated fill rules for empty parameter values during fix (for example Model=from:FamilyTypeName, RoomName=from:FamilyName, Mark=from:Type Mark).",
                        Required = false
                    },
                    new CapabilityConfigurationKey
                    {
                        Key = "{Category}.parameterBinding",
                        Description = "Comma-separated shared-parameter binding for automatic correction (for example FireRating=type,RoomMarker=instance). Defaults to type when omitted.",
                        Required = false
                    }
                ]
            },
            Examples =
            [
                new CapabilityExample
                {
                    Description = "Require FireRating on all door family types.",
                    Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Doors.parameters"] = "FireRating"
                    }
                },
                new CapabilityExample
                {
                    Description = "Require Manufacturer on furniture family types.",
                    Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Furniture.parameters"] = "Manufacturer"
                    }
                }
            ]
        },
        new CapabilityDefinition
        {
            EngineId = "family-engine",
            CapabilityId = "family.imported-cad",
            DisplayName = "Imported CAD Exclusion",
            Description = "Excludes families containing imported CAD geometry from configured categories.",
            Status = CapabilityCompatibilityStatus.Supported,
            HandlerId = CapabilityHandlerIds.FamilyImportedCad,
            ImplementationAtomId = "family.detection.imported-cad",
            ConfigurationSchema = new CapabilityConfigurationSchema
            {
                Keys =
                [
                    new CapabilityConfigurationKey
                    {
                        Key = "excludeImportedCad.categories",
                        Description = "Comma-separated category names where imported CAD families are excluded.",
                        Required = true
                    }
                ]
            },
            Examples =
            [
                new CapabilityExample
                {
                    Description = "Exclude imported CAD from doors and windows.",
                    Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["excludeImportedCad.categories"] = "Doors,Windows"
                    }
                }
            ]
        },
        new CapabilityDefinition
        {
            EngineId = "report-engine",
            CapabilityId = "report.compliance",
            DisplayName = "Compliance Report",
            Description = "Generates HTML and JSON compliance reports from collected evidence and diagnostics.",
            Status = CapabilityCompatibilityStatus.Supported,
            HandlerId = CapabilityHandlerIds.ReportCompliance,
            ImplementationAtomId = "report.compliance",
            ConfigurationSchema = new CapabilityConfigurationSchema(),
            Examples =
            [
                new CapabilityExample
                {
                    Description = "Generate a compliance report with no additional configuration.",
                    Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                }
            ]
        }
    ];
}

using System.Text.RegularExpressions;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Rejects undocumented or hallucinated capability configuration in .bimrule files.
/// </summary>
public sealed partial class BimRuleConfigurationValidator
{
    private static readonly Regex PlaceholderTokenPattern =
#if REVIT2024
        new(@"\{[^}]+\}", RegexOptions.Compiled);
#else
        PlaceholderTokenRegex();
#endif

    private readonly CapabilityRegistry _registry;

    public BimRuleConfigurationValidator()
        : this(BimRuleCapabilityRegistry.Default)
    {
    }

    public BimRuleConfigurationValidator(CapabilityRegistry registry)
    {
        ArgumentGuard.ThrowIfNull(registry);
        _registry = registry;
    }

    public CapabilityValidationResult Validate(BimRule? rule)
    {
        var diagnostics = new List<CapabilityValidationDiagnostic>();
        if (rule?.Engines is null)
        {
            return new CapabilityValidationResult { Diagnostics = diagnostics };
        }

        foreach (var engine in rule.Engines)
        {
            if (engine?.Capabilities is null)
            {
                continue;
            }

            foreach (var capability in engine.Capabilities)
            {
                ValidateCapabilityConfiguration(engine.EngineId, capability, diagnostics);
            }
        }

        return new CapabilityValidationResult { Diagnostics = diagnostics };
    }

    private void ValidateCapabilityConfiguration(
        string engineId,
        BimRuleCapabilityReference? capability,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        if (capability is null || string.IsNullOrWhiteSpace(capability.AtomId))
        {
            return;
        }

        if (!_registry.TryGetDefinition(engineId, capability.AtomId.Trim(), out var definition)
            || definition is null)
        {
            return;
        }

        if (capability.Configuration is null || capability.Configuration.Count == 0)
        {
            return;
        }

        var allowedSchemaKeys = definition.ConfigurationSchema?.Keys ?? [];

        foreach (var entry in capability.Configuration)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            if (!IsAllowedConfigurationKey(entry.Key, allowedSchemaKeys))
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationKeyUnknown,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Configuration key '{entry.Key}' is not supported for capability '{capability.AtomId}'. " +
                        $"Use only documented keys from the capability registry.",
                    CapabilityName = capability.AtomId,
                    ExpectedCapability = FormatAllowedKeys(allowedSchemaKeys),
                    ActualCapability = entry.Key,
                    EngineId = engineId
                });
                continue;
            }

            ValidateConfigurationValue(engineId, capability.AtomId, entry.Key, entry.Value, diagnostics);
        }
    }

    private static bool IsAllowedConfigurationKey(
        string actualKey,
        IReadOnlyList<CapabilityConfigurationKey> schemaKeys)
    {
        foreach (var schemaKey in schemaKeys)
        {
            if (ConfigurationKeyMatches(actualKey, schemaKey.Key))
            {
                return true;
            }
        }

        return schemaKeys.Count == 0;
    }

    internal static bool ConfigurationKeyMatches(string actualKey, string schemaKey)
    {
        if (string.Equals(actualKey, schemaKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return schemaKey switch
        {
            "{Category}.prefix" => HasCategorySuffix(actualKey, "prefix"),
            "{Category}.parameters" => HasCategorySuffix(actualKey, "parameters"),
            "{Category}.parameterDefaults" => HasCategorySuffix(actualKey, "parameterDefaults"),
            "{Category}.parameterFillRules" => HasCategorySuffix(actualKey, "parameterFillRules"),
            "{Category}.parameterBinding" => HasCategorySuffix(actualKey, "parameterBinding"),
            "{Category}.prefixFix" => HasCategorySuffix(actualKey, "prefixFix"),
            _ => false
        };
    }

    private static bool HasCategorySuffix(string actualKey, string suffix) =>
        actualKey.EndsWith('.' + suffix, StringComparison.OrdinalIgnoreCase)
        && actualKey.Length > suffix.Length + 1;

    private static void ValidateConfigurationValue(
        string engineId,
        string capabilityId,
        string configurationKey,
        string? configurationValue,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(configurationValue))
        {
            return;
        }

        if (PlaceholderTokenPattern.IsMatch(configurationValue)
            && !configurationValue.Contains("from:", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.ConfigurationPlaceholderForbidden,
                Severity = ValidationSeverity.Error,
                Message =
                    $"Configuration value for '{configurationKey}' contains unsupported placeholder syntax " +
                    $"(for example '{{TypeName}}'). Use parameterFillRules with from:FamilyTypeName instead.",
                CapabilityName = capabilityId,
                ExpectedCapability = "Model=from:FamilyTypeName in parameterFillRules",
                ActualCapability = configurationValue,
                EngineId = engineId
            });
            return;
        }

        if (configurationKey.EndsWith(".parameterFillRules", StringComparison.OrdinalIgnoreCase))
        {
            ValidateParameterFillRules(engineId, capabilityId, configurationKey, configurationValue, diagnostics);
            return;
        }

        if (configurationKey.EndsWith(".prefixFix", StringComparison.OrdinalIgnoreCase))
        {
            ValidatePrefixFixScope(engineId, capabilityId, configurationKey, configurationValue, diagnostics);
            return;
        }

        if (configurationKey.EndsWith(".parameterBinding", StringComparison.OrdinalIgnoreCase))
        {
            ValidateParameterBinding(engineId, capabilityId, configurationKey, configurationValue, diagnostics);
            return;
        }

        if (configurationKey.EndsWith(".parameterDefaults", StringComparison.OrdinalIgnoreCase))
        {
            ValidateParameterDefaults(engineId, capabilityId, configurationKey, configurationValue, diagnostics);
        }
    }

    private static void ValidateParameterDefaults(
        string engineId,
        string capabilityId,
        string configurationKey,
        string configurationValue,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        foreach (var token in StringParsing.SplitCommaSeparated(configurationValue))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Configuration value for '{configurationKey}' must use ParameterName=LiteralValue pairs " +
                        $"(for example FireRating=EI60). Invalid token: '{token}'.",
                    CapabilityName = capabilityId,
                    ExpectedCapability = "FireRating=EI60,RoomName=Undefined",
                    ActualCapability = token,
                    EngineId = engineId
                });
            }
        }
    }

    private static void ValidateParameterBinding(
        string engineId,
        string capabilityId,
        string configurationKey,
        string configurationValue,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        foreach (var token in StringParsing.SplitCommaSeparated(configurationValue))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Configuration value for '{configurationKey}' must use ParameterName=type or " +
                        $"ParameterName=instance pairs. Invalid token: '{token}'.",
                    CapabilityName = capabilityId,
                    ExpectedCapability = "FireRating=type,RoomMarker=instance",
                    ActualCapability = token,
                    EngineId = engineId
                });
                continue;
            }

            var binding = token[(separatorIndex + 1)..].Trim();
            if (!string.Equals(binding, "type", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(binding, "instance", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Parameter binding '{token}' is not supported. Use 'type' or 'instance' only.",
                    CapabilityName = capabilityId,
                    ExpectedCapability = "type or instance",
                    ActualCapability = binding,
                    EngineId = engineId
                });
            }
        }
    }

    private static void ValidateParameterFillRules(
        string engineId,
        string capabilityId,
        string configurationKey,
        string configurationValue,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        foreach (var token in StringParsing.SplitCommaSeparated(configurationValue))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Configuration value for '{configurationKey}' must use ParameterName=from:Source pairs " +
                        $"(for example Model=from:FamilyTypeName). Invalid token: '{token}'.",
                    CapabilityName = capabilityId,
                    ExpectedCapability = "Model=from:FamilyTypeName",
                    ActualCapability = token,
                    EngineId = engineId
                });
                continue;
            }

            var fillRule = token[(separatorIndex + 1)..].Trim();
            if (!fillRule.StartsWith("from:", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new CapabilityValidationDiagnostic
                {
                    Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"Parameter fill rule '{token}' must start with 'from:' " +
                        $"(for example Model=from:FamilyTypeName).",
                    CapabilityName = capabilityId,
                    ExpectedCapability = "from:FamilyTypeName, from:FamilyName, from:OtherParameter",
                    ActualCapability = fillRule,
                    EngineId = engineId
                });
            }
        }
    }

    private static void ValidatePrefixFixScope(
        string engineId,
        string capabilityId,
        string configurationKey,
        string configurationValue,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        foreach (var token in StringParsing.SplitCommaSeparated(configurationValue))
        {
            if (token.Equals("type", StringComparison.OrdinalIgnoreCase)
                || token.Equals("family", StringComparison.OrdinalIgnoreCase)
                || token.Equals("both", StringComparison.OrdinalIgnoreCase)
                || token.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid,
                Severity = ValidationSeverity.Error,
                Message =
                    $"Prefix fix scope '{token}' is not supported. Use type, family, or both.",
                CapabilityName = capabilityId,
                ExpectedCapability = "type, family, or both",
                ActualCapability = token,
                EngineId = engineId
            });
        }
    }

    private static string? FormatAllowedKeys(IReadOnlyList<CapabilityConfigurationKey> schemaKeys)
    {
        if (schemaKeys.Count == 0)
        {
            return null;
        }

        return string.Join(", ", schemaKeys.Select(key => key.Key));
    }

#if !REVIT2024
    [GeneratedRegex(@"\{[^}]+\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderTokenRegex();
#endif
}

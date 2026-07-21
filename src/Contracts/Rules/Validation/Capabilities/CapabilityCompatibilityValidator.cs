using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Validates that BIMRule capability references are known to the current implementation.
/// </summary>
public sealed class CapabilityCompatibilityValidator : ICapabilityCompatibilityValidator
{
    private readonly CapabilityRegistry _registry;

    public CapabilityCompatibilityValidator()
        : this(BimRuleCapabilityRegistry.Default)
    {
    }

    public CapabilityCompatibilityValidator(CapabilityRegistry registry)
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

        var seenCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var engineIndex = 0; engineIndex < rule.Engines.Count; engineIndex++)
        {
            var engine = rule.Engines[engineIndex];
            if (engine?.Capabilities is null)
            {
                continue;
            }

            for (var capabilityIndex = 0; capabilityIndex < engine.Capabilities.Count; capabilityIndex++)
            {
                var capability = engine.Capabilities[capabilityIndex];
                ValidateCapabilityReference(engine.EngineId, capability, seenCapabilities, diagnostics);
            }
        }

        return new CapabilityValidationResult { Diagnostics = diagnostics };
    }

    private void ValidateCapabilityReference(
        string engineId,
        BimRuleCapabilityReference? capability,
        ISet<string> seenCapabilities,
        List<CapabilityValidationDiagnostic> diagnostics)
    {
        if (capability is null || string.IsNullOrWhiteSpace(capability.AtomId))
        {
            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.CapabilityMissing,
                Severity = ValidationSeverity.Error,
                Message = "A capability reference requires an atom identifier.",
                CapabilityName = capability?.AtomId,
                ExpectedCapability = null,
                ActualCapability = capability?.AtomId,
                EngineId = engineId
            });
            return;
        }

        var capabilityId = capability.AtomId.Trim();
        var capabilityKey = CapabilityRegistry.CreateKey(engineId, capabilityId);

        if (!seenCapabilities.Add(capabilityKey))
        {
            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.CapabilityDuplicate,
                Severity = ValidationSeverity.Error,
                Message = $"Capability '{capabilityId}' is referenced more than once for engine '{engineId}'.",
                CapabilityName = capabilityId,
                ExpectedCapability = capabilityId,
                ActualCapability = capabilityId,
                EngineId = engineId
            });
            return;
        }

        if (!_registry.TryGetDefinition(engineId, capabilityId, out var definition) || definition is null)
        {
            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.CapabilityUnknown,
                Severity = ValidationSeverity.Error,
                Message = $"Capability '{capabilityId}' is not registered for engine '{engineId}'.",
                CapabilityName = capabilityId,
                ExpectedCapability = GetExpectedCapabilitiesForEngine(engineId),
                ActualCapability = capabilityId,
                EngineId = engineId
            });
            return;
        }

        if (definition.Status == CapabilityCompatibilityStatus.Deprecated)
        {
            diagnostics.Add(new CapabilityValidationDiagnostic
            {
                Code = CapabilityValidationDiagnosticCodes.CapabilityDeprecated,
                Severity = ValidationSeverity.Warning,
                Message = $"Capability '{capabilityId}' is deprecated for engine '{engineId}'.",
                CapabilityName = capabilityId,
                ExpectedCapability = GetSupportedReplacement(engineId, capabilityId),
                ActualCapability = capabilityId,
                EngineId = engineId
            });
        }
    }

    private string? GetExpectedCapabilitiesForEngine(string engineId)
    {
        var capabilities = _registry.Definitions
            .Where(definition => string.Equals(definition.EngineId, engineId, StringComparison.OrdinalIgnoreCase))
            .Select(definition => definition.CapabilityId)
            .OrderBy(capabilityId => capabilityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return capabilities.Length == 0 ? null : string.Join(", ", capabilities);
    }

    private string? GetSupportedReplacement(string engineId, string capabilityId)
    {
        if (_registry.TryGetDefinition(engineId, capabilityId, out var definition) &&
            !string.IsNullOrWhiteSpace(definition?.ReplacementCapabilityId))
        {
            return definition.ReplacementCapabilityId;
        }

        var supportedCapability = _registry.Definitions
            .FirstOrDefault(definition =>
                string.Equals(definition.EngineId, engineId, StringComparison.OrdinalIgnoreCase) &&
                definition.Status == CapabilityCompatibilityStatus.Supported);

        return supportedCapability?.CapabilityId ?? capabilityId;
    }
}

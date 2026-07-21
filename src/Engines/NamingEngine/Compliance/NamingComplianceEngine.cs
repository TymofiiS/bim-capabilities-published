using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Naming.Prefix;
using BIMCapabilities.Engines.Naming.Atoms.Pattern;
using BIMCapabilities.Engines.Naming.Atoms.Prefix;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;
using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;
using PrefixContracts = BIMCapabilities.Contracts.Engines.Naming.Prefix;

namespace BIMCapabilities.Engines.Naming.Compliance;

/// <summary>
/// Orchestrates prefix and naming pattern validation atoms.
/// </summary>
public sealed class NamingComplianceEngine : ComplianceContracts.INamingComplianceEngine
{
    public const string ComplianceEngineId = "naming.compliance";

    private readonly PrefixContracts.IPrefixValidationAtom _prefixAtom;
    private readonly PatternContracts.INamingPatternValidationAtom _patternAtom;

    public NamingComplianceEngine()
        : this(new PrefixValidationAtom(), new NamingPatternValidationAtom())
    {
    }

    internal NamingComplianceEngine(
        PrefixContracts.IPrefixValidationAtom prefixAtom,
        PatternContracts.INamingPatternValidationAtom patternAtom)
    {
        _prefixAtom = prefixAtom;
        _patternAtom = patternAtom;
    }

    public string EngineId => ComplianceEngineId;

    public ComplianceContracts.NamingComplianceResult Evaluate(ComplianceContracts.NamingComplianceRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var prefixResult = RunPrefixValidation(request);
        var patternResult = RunPatternValidation(request);

        return NamingComplianceEngineSupport.CreateResult(
            EngineId,
            request,
            prefixResult,
            patternResult);
    }

    private PrefixContracts.PrefixValidationResult? RunPrefixValidation(
        ComplianceContracts.NamingComplianceRequest request)
    {
        if (request.RequiredPrefixes is not { Count: > 0 })
        {
            return null;
        }

        return _prefixAtom.Validate(new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = request.TargetSet,
            RequiredPrefixes = request.RequiredPrefixes,
            CaseSensitive = request.CaseSensitive,
            PrefixFixScope = request.PrefixFixScope,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    private PatternContracts.NamingPatternValidationResult? RunPatternValidation(
        ComplianceContracts.NamingComplianceRequest request)
    {
        if (request.PatternRule is null)
        {
            return null;
        }

        return _patternAtom.Validate(new PatternContracts.NamingPatternValidationRequest
        {
            TargetSet = request.TargetSet,
            Rule = request.PatternRule,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }
}

using BIMCapabilities.Contracts.Engines.Parameter.Existence;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Value;
using BIMCapabilities.Engines.Parameter.Atoms.Existence;
using BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;
using BIMCapabilities.Engines.Parameter.Atoms.Value;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using ExistenceContracts = BIMCapabilities.Contracts.Engines.Parameter.Existence;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Engines.Parameter.Compliance;

/// <summary>
/// Orchestrates parameter existence, shared parameter, and value validation atoms.
/// </summary>
public sealed class ParameterComplianceEngine : ComplianceContracts.IParameterComplianceEngine
{
    public const string ComplianceEngineId = "parameter.compliance";

    private readonly ExistenceContracts.IParameterExistenceValidationAtom _existenceAtom;
    private readonly SharedParameterContracts.ISharedParameterValidationAtom _sharedParameterAtom;
    private readonly ValueContracts.IParameterValueValidationAtom _valueAtom;

    public ParameterComplianceEngine()
        : this(
            new ParameterExistenceValidationAtom(),
            new SharedParameterValidationAtom(),
            new ParameterValueValidationAtom())
    {
    }

    internal ParameterComplianceEngine(
        ExistenceContracts.IParameterExistenceValidationAtom existenceAtom,
        SharedParameterContracts.ISharedParameterValidationAtom sharedParameterAtom,
        ValueContracts.IParameterValueValidationAtom valueAtom)
    {
        _existenceAtom = existenceAtom;
        _sharedParameterAtom = sharedParameterAtom;
        _valueAtom = valueAtom;
    }

    public string EngineId => ComplianceEngineId;

    public ComplianceContracts.ParameterComplianceResult Evaluate(ComplianceContracts.ParameterComplianceRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var existenceResult = RunExistenceValidation(request);
        var sharedParameterResult = RunSharedParameterValidation(request);
        var valueResult = RunValueValidation(request);

        return ParameterComplianceEngineSupport.CreateResult(
            EngineId,
            request,
            existenceResult,
            sharedParameterResult,
            valueResult);
    }

    private ExistenceContracts.ParameterExistenceResult? RunExistenceValidation(
        ComplianceContracts.ParameterComplianceRequest request)
    {
        if (request.RequiredParameterNames is not { Count: > 0 })
        {
            return null;
        }

        return _existenceAtom.Validate(new ExistenceContracts.ParameterExistenceRequest
        {
            TargetSet = request.TargetSet,
            ParameterQueryResult = request.ParameterQueryResult,
            RequiredParameterNames = request.RequiredParameterNames,
            ParameterBindings = request.ParameterBindings,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    private SharedParameterContracts.SharedParameterValidationResult? RunSharedParameterValidation(
        ComplianceContracts.ParameterComplianceRequest request)
    {
        if (request.SharedParameterFile is null
            || request.SharedParameterNamesToValidate is not { Count: > 0 })
        {
            return null;
        }

        return _sharedParameterAtom.Validate(new SharedParameterContracts.SharedParameterValidationRequest
        {
            TargetSet = request.TargetSet,
            ParameterQueryResult = request.ParameterQueryResult,
            SharedParameterFile = request.SharedParameterFile,
            ParameterNamesToValidate = request.SharedParameterNamesToValidate,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    private ValueContracts.ParameterValueValidationResult? RunValueValidation(
        ComplianceContracts.ParameterComplianceRequest request)
    {
        if (request.ValueRules is not { Count: > 0 })
        {
            return null;
        }

        return _valueAtom.Validate(new ValueContracts.ParameterValueValidationRequest
        {
            TargetSet = request.TargetSet,
            ParameterQueryResult = request.ParameterQueryResult,
            Rules = request.ValueRules,
            ParameterBindings = request.ParameterBindings,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }
}

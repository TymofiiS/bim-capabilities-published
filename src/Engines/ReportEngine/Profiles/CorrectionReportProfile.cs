using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Profiles;

/// <summary>
/// Prepares correction report output after executable parameter fixes.
/// </summary>
public sealed class CorrectionReportProfile : IReportProfile
{
    public ReportProfileType ProfileType => ReportProfileType.Fix;

    public ReportProfile Profile { get; } = CorrectionReportProfileDefinition.Create();

    public ReportOutput Prepare(ReportProfileRequest request)
    {
        throw new NotSupportedException("Use Prepare(CorrectionReportRequest) for correction reports.");
    }

    public ReportOutput Prepare(CorrectionReportRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ruleId"] = request.RuleId,
            ["ruleName"] = request.RuleName,
            ["parametersAdded"] = request.ParametersAdded.ToString(),
            ["valuesAssigned"] = request.ValuesAssigned.ToString(),
            ["namesRenamed"] = request.NamesRenamed.ToString(),
            ["affectedTypes"] = request.AffectedTypes.ToString(),
            ["affectedFamilies"] = request.AffectedFamilies.ToString(),
            ["affectedInstances"] = request.AffectedInstances.ToString(),
            ["defaultValueCount"] = request.DefaultValuesApplied.Count.ToString(),
            ["executionDate"] = request.GeneratedAt.ToString("u")
        };

        for (var index = 0; index < request.DefaultValuesApplied.Count; index++)
        {
            structuredData[$"defaultValue[{index}]"] = request.DefaultValuesApplied[index];
        }

        return new ReportOutput
        {
            ReportId = $"correction-{request.RuleId}-{request.GeneratedAt:yyyyMMddHHmmss}",
            Title = "Correction Report",
            ProfileId = CorrectionReportProfileDefinition.ProfileId,
            GeneratedAt = request.GeneratedAt,
            Metadata = new ReportMetadata
            {
                RuleId = request.RuleId,
                ProfileId = CorrectionReportProfileDefinition.ProfileId,
                CorrelationId = request.CorrelationId,
                GeneratedBy = "CorrectionReportProfile"
            },
            Sections =
            [
                new ReportSection
                {
                    Name = CorrectionReportProfileSections.CorrectionSummary,
                    Order = 1,
                    Content = new ReportContent
                    {
                        StructuredData = structuredData
                    }
                }
            ]
        };
    }
}

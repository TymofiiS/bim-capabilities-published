using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class LargeCorrectionFixture
{
    internal const int ParameterIssueCount = 25;
    internal const int NamingIssueCount = 25;

    internal static ParameterWriteRequestBuildRequest CreateParameterBuildRequest()
    {
        var families = new List<NormalizedFamily>(ParameterIssueCount);
        var findings = new List<ParameterComplianceFinding>(ParameterIssueCount);

        for (var index = 0; index < ParameterIssueCount; index++)
        {
            var familyId = $"family-large-param-{index:D3}";
            var familyName = $"HTL_Door_Large_{index:D3}";
            families.Add(WriteLayerFixtureBuilder.CreateDoorFamily(familyId, familyName));
            findings.Add(new ParameterComplianceFinding
            {
                ValidationStage = "existence",
                ObjectId = familyId,
                ObjectKind = "family",
                ObjectName = familyName,
                ParameterName = "FireRating",
                Passed = false,
                Status = "Missing",
                Message = "Required parameter 'FireRating' is missing."
            });
        }

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(
            new ParameterComplianceResult
            {
                EngineId = "parameter.compliance",
                Findings = findings
            },
            WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-large-parameter", families.ToArray()));
    }

    internal static NamingWriteRequestBuildRequest CreateNamingBuildRequest()
    {
        var families = new List<NormalizedFamily>(NamingIssueCount);
        var findings = new List<NamingComplianceFinding>(NamingIssueCount);
        var correctionIntents = new List<NamingWriteCorrectionIntent>(NamingIssueCount);

        for (var index = 0; index < NamingIssueCount; index++)
        {
            var familyId = $"family-large-name-{index:D3}";
            var currentName = $"Window_Large_{index:D3}";
            var proposedName = $"WN_WindowLarge{index:D3}";
            families.Add(WriteLayerFixtureBuilder.CreateWindowFamily(familyId, currentName));
            findings.Add(new NamingComplianceFinding
            {
                ValidationStage = "prefix",
                ObjectId = familyId,
                ObjectKind = "family",
                ObjectName = currentName,
                Passed = false,
                Status = "MissingPrefix",
                Message = $"Naming validation failed for '{currentName}'."
            });
            correctionIntents.Add(new NamingWriteCorrectionIntent
            {
                ObjectId = familyId,
                ProposedName = proposedName
            });
        }

        return new NamingWriteRequestBuildRequest
        {
            ComplianceResult = new NamingComplianceResult
            {
                EngineId = "naming.compliance",
                Findings = findings
            },
            TargetSet = WriteLayerFixtureBuilder.CreateNamingTargetSet(
                "fixture-large-naming",
                families.ToArray()),
            RequiredPrefixes = [WriteLayerFixtureBuilder.WindowPrefix],
            PatternRule = WriteLayerFixtureBuilder.CreateWindowPatternRule(),
            CorrectionIntents = correctionIntents,
            PrefixFixScope = PrefixFixScope.All,
            RequestedAt = WriteLayerFixtureBuilder.RequestedAt,
            RuleId = WriteLayerFixtureBuilder.RuleId,
            CorrelationId = WriteLayerFixtureBuilder.CorrelationId
        };
    }
}

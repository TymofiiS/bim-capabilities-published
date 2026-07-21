using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Generation.Parsing;

namespace BIMCapabilities.Generation;

internal static class BimRuleDocumentBuilder
{
    public static BimRule Build(ParsedRuleSpecification specification, BimRuleGenerationRequest request)
    {
        var generatedAt = request.GeneratedAt ?? DateTimeOffset.UtcNow;
        var author = string.IsNullOrWhiteSpace(request.Author) ? "BIM Assistant" : request.Author;

        var rule = new BimRule
        {
            Metadata = new BimRuleMetadata
            {
                RuleId = specification.RuleId,
                Name = specification.RuleId,
                RuleVersion = specification.RuleVersion,
                ContractVersion = "1.0",
                Description = BuildDescription(specification),
                Domain = specification.Domain,
                Status = "Approved",
                Author = author,
                CreatedAt = generatedAt
            },
            ExternalReferences = BuildExternalReferences(specification),
            Engines = BuildEngines(specification),
            Execution = new BimRuleExecution
            {
                TargetPlatform = "Revit",
                ExecutionMode = "Validation",
                ValidationEnabled = true,
                FixEnabled = false,
                DryRun = false,
                RequireUserApprovalBeforeModification = false,
                FailureBehavior = "StopOnFirstUnrecoverableFailure"
            },
            Report = new BimRuleReport
            {
                GenerateHtmlReport = true,
                GenerateJsonReport = true,
                IncludeEvidence = true,
                ReportTitle = $"{specification.Domain} Compliance Report",
                ComplianceSummaryProfile = "Compliance",
                ResultGrouping = "Engine"
            }
        };

        return rule;
    }

    private static string BuildDescription(ParsedRuleSpecification specification)
    {
        var categories = string.Join(" and ", specification.Categories.Select(category => category.CategoryName.ToLowerInvariant()));
        return specification.Topic switch
        {
            "EQUIPMENT" => $"MEP equipment family acceptance standard covering {categories}.",
            "FURNITURE" => $"Furniture family acceptance standard covering {categories}.",
            _ => $"Door and window family acceptance standard covering {categories}."
        };
    }

    private static List<BimRuleExternalReference> BuildExternalReferences(ParsedRuleSpecification specification)
    {
        if (string.IsNullOrWhiteSpace(specification.SharedParameterFilePath))
        {
            return [];
        }

        return
        [
            new BimRuleExternalReference
            {
                ReferenceType = "SharedParameterFile",
                Location = specification.SharedParameterFilePath,
                Purpose = "Resolve required shared parameters.",
                IsRequired = true,
                ConsumerEngine = "parameter-engine"
            }
        ];
    }

    private static List<BimRuleEngine> BuildEngines(ParsedRuleSpecification specification)
    {
        var namingConfiguration = BuildNamingConfiguration(specification);
        var parameterConfiguration = BuildParameterConfiguration(specification);
        var familyConfiguration = BuildFamilyConfiguration(specification);

        return
        [
            new BimRuleEngine
            {
                EngineId = "naming-engine",
                Order = 1,
                Capabilities =
                [
                    new BimRuleCapabilityReference
                    {
                        AtomId = "naming.prefix.validation",
                        Configuration = namingConfiguration
                    }
                ]
            },
            new BimRuleEngine
            {
                EngineId = "parameter-engine",
                Order = 2,
                Capabilities =
                [
                    new BimRuleCapabilityReference
                    {
                        AtomId = "parameter.existence",
                        Configuration = parameterConfiguration
                    }
                ]
            },
            new BimRuleEngine
            {
                EngineId = "family-engine",
                Order = 3,
                Capabilities =
                [
                    new BimRuleCapabilityReference
                    {
                        AtomId = "family.imported-cad",
                        Configuration = familyConfiguration
                    }
                ]
            },
            new BimRuleEngine
            {
                EngineId = "report-engine",
                Order = 4,
                Capabilities =
                [
                    new BimRuleCapabilityReference
                    {
                        AtomId = "report.compliance"
                    }
                ]
            }
        ];
    }

    private static Dictionary<string, string> BuildNamingConfiguration(ParsedRuleSpecification specification)
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in specification.Categories.Where(category => !string.IsNullOrWhiteSpace(category.RequiredPrefix)))
        {
            configuration[$"{category.CategoryName}.prefix"] = category.RequiredPrefix!;
        }

        return configuration;
    }

    private static Dictionary<string, string> BuildParameterConfiguration(ParsedRuleSpecification specification)
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in specification.Categories)
        {
            if (category.RequiredParameters.Count == 0)
            {
                continue;
            }

            configuration[$"{category.CategoryName}.parameters"] = string.Join(",", category.RequiredParameters);
        }

        return configuration;
    }

    private static Dictionary<string, string> BuildFamilyConfiguration(ParsedRuleSpecification specification)
    {
        var categories = specification.Categories
            .Where(category => category.ExcludeImportedCad)
            .Select(category => category.CategoryName)
            .ToList();

        if (categories.Count == 0)
        {
            return [];
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["excludeImportedCad.categories"] = string.Join(",", categories)
        };
    }
}

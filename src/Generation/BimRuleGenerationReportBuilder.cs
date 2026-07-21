using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Generation.Parsing;

namespace BIMCapabilities.Generation;

internal static class BimRuleGenerationReportBuilder
{
    public static BimRuleGenerationReport Build(ParsedRuleSpecification specification)
    {
        var namingRules = specification.Categories
            .Where(category => !string.IsNullOrWhiteSpace(category.RequiredPrefix))
            .Select(category => $"{category.CategoryName} name starts with {category.RequiredPrefix}")
            .ToList();

        var complianceRules = specification.Categories
            .Where(category => category.ExcludeImportedCad)
            .Select(category => $"{category.CategoryName} do not contain imported CAD")
            .ToList();

        if (specification.GenerateReport)
        {
            complianceRules.Add("Generate validation report.");
        }

        return new BimRuleGenerationReport
        {
            GeneratedRuleName = specification.RuleId,
            DetectedCategories = specification.Categories.Select(category => category.CategoryName).ToList(),
            DetectedParameters = specification.Categories
                .SelectMany(category => category.RequiredParameters)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            DetectedNamingRules = namingRules,
            DetectedComplianceRules = complianceRules,
            SharedParameterFilePath = specification.SharedParameterFilePath
        };
    }
}

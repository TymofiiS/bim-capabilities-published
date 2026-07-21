using System.Text.RegularExpressions;

namespace BIMCapabilities.Generation.Parsing;

/// <summary>
/// Deterministic natural-language parser for BIMRule generation demos and tests.
/// </summary>
internal static partial class NaturalLanguageRuleParser
{
    private static readonly Regex WindowsSectionPattern = WindowsSectionRegex();
    private static readonly Regex DoorsSectionPattern = DoorsSectionRegex();
    private static readonly Regex FurnitureSectionPattern = FurnitureSectionRegex();
    private static readonly Regex EquipmentSectionPattern = EquipmentSectionRegex();
    private static readonly Regex SharedParameterPathPattern = SharedParameterPathRegex();
    private static readonly Regex PrefixPattern = PrefixRegex();
    private static readonly Regex ParameterPattern = ParameterRegex();

    public static ParsedRuleSpecification Parse(string prompt)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(prompt);

        var normalized = prompt.Replace("\r\n", "\n", StringComparison.Ordinal);
        var discipline = ResolveDiscipline(normalized);
        var topic = ResolveTopic(normalized, discipline);
        var sharedParameterPath = ExtractSharedParameterPath(normalized);
        var generateReport = !normalized.Contains("no report", StringComparison.OrdinalIgnoreCase);

        var categories = new List<CategorySpecification>();

        if (topic == "EQUIPMENT")
        {
            categories.AddRange(ParseEquipmentSections(normalized));
        }
        else if (topic == "FURNITURE")
        {
            categories.AddRange(ParseFurnitureSections(normalized));
        }
        else
        {
            categories.AddRange(ParseOpeningSections(normalized));
        }

        if (categories.Count == 0)
        {
            categories.AddRange(CreateDefaultCategories(discipline, topic));
        }

        return new ParsedRuleSpecification
        {
            DisciplineCode = discipline,
            Topic = topic,
            SharedParameterFilePath = sharedParameterPath,
            GenerateReport = generateReport,
            Categories = categories
        };
    }

    private static string ResolveDiscipline(string prompt)
    {
        if (ContainsAny(prompt, "mep", "mechanical", "electrical", "plumbing", "equipment"))
        {
            return "MEP";
        }

        if (ContainsAny(prompt, "interior", "int ", " int\n", "int-"))
        {
            return "INT";
        }

        return "ARC";
    }

    private static string ResolveTopic(string prompt, string discipline)
    {
        if (discipline == "MEP" || ContainsAny(prompt, "equipment", "mechanical equipment", "mep equipment"))
        {
            return "EQUIPMENT";
        }

        if (ContainsAny(prompt, "furniture", "furnishing", "furnishings"))
        {
            return "FURNITURE";
        }

        return "OPENINGS";
    }

    private static IEnumerable<CategorySpecification> ParseOpeningSections(string prompt)
    {
        var doors = ParseCategorySection(prompt, DoorsSectionPattern, "Doors", defaultPrefix: "DR_");
        if (doors is not null)
        {
            yield return doors;
        }

        var windows = ParseCategorySection(prompt, WindowsSectionPattern, "Windows", defaultPrefix: "WN_");
        if (windows is not null)
        {
            yield return windows;
        }
    }

    private static IEnumerable<CategorySpecification> ParseFurnitureSections(string prompt)
    {
        var furniture = ParseCategorySection(prompt, FurnitureSectionPattern, "Furniture", defaultPrefix: "FR_");
        if (furniture is not null)
        {
            yield return new CategorySpecification
            {
                CategoryName = furniture.CategoryName,
                RequiredParameters = furniture.RequiredParameters,
                ExcludeImportedCad = furniture.ExcludeImportedCad
            };
        }
    }

    private static IEnumerable<CategorySpecification> ParseEquipmentSections(string prompt)
    {
        var equipment = ParseCategorySection(prompt, EquipmentSectionPattern, "Mechanical Equipment", defaultPrefix: "ME_");
        if (equipment is not null)
        {
            yield return equipment;
        }
    }

    private static CategorySpecification? ParseCategorySection(
        string prompt,
        Regex sectionPattern,
        string categoryName,
        string defaultPrefix)
    {
        var match = sectionPattern.Match(prompt);
        if (!match.Success)
        {
            return null;
        }

        var sectionBody = match.Groups["body"].Value;
        var prefix = ExtractPrefix(sectionBody) ?? defaultPrefix;
        var parameters = ExtractParameters(sectionBody);
        var excludeImportedCad = sectionBody.Contains("imported cad", StringComparison.OrdinalIgnoreCase)
            || sectionBody.Contains("do not contain imported", StringComparison.OrdinalIgnoreCase)
            || sectionBody.Contains("no imported cad", StringComparison.OrdinalIgnoreCase);

        return new CategorySpecification
        {
            CategoryName = categoryName,
            RequiredPrefix = prefix,
            RequiredParameters = parameters,
            ExcludeImportedCad = excludeImportedCad
        };
    }

    private static IEnumerable<CategorySpecification> CreateDefaultCategories(string discipline, string topic)
    {
        if (topic == "EQUIPMENT")
        {
            yield return new CategorySpecification
            {
                CategoryName = "Mechanical Equipment",
                RequiredPrefix = "ME_",
                RequiredParameters = ["Manufacturer", "ModelNumber"],
                ExcludeImportedCad = true
            };

            yield break;
        }

        if (topic == "FURNITURE")
        {
            yield return new CategorySpecification
            {
                CategoryName = "Furniture",
                RequiredPrefix = null,
                RequiredParameters = ["Manufacturer"],
                ExcludeImportedCad = true
            };

            yield break;
        }

        yield return new CategorySpecification
        {
            CategoryName = "Doors",
            RequiredPrefix = "DR_",
            RequiredParameters = discipline == "INT"
                ? ["RoomName", "FinishType"]
                : ["RoomName", "FireRating"],
            ExcludeImportedCad = true
        };

        yield return new CategorySpecification
        {
            CategoryName = "Windows",
            RequiredPrefix = "WN_",
            RequiredParameters = discipline == "INT"
                ? ["RoomName", "FinishType"]
                : ["RoomName", "AcousticRating"],
            ExcludeImportedCad = true
        };
    }

    private static string? ExtractSharedParameterPath(string prompt)
    {
        var match = SharedParameterPathPattern.Match(prompt);
        if (match.Success)
        {
            return match.Groups["path"].Value.Trim();
        }

        if (prompt.Contains("shared parameter", StringComparison.OrdinalIgnoreCase)
            && prompt.Contains("CompanySharedParameters.txt", StringComparison.OrdinalIgnoreCase))
        {
            return @"D:\Company\SharedParameters.txt";
        }

        return null;
    }

    private static string? ExtractPrefix(string sectionBody)
    {
        var match = PrefixPattern.Match(sectionBody);
        return match.Success ? match.Groups["prefix"].Value : null;
    }

    private static List<string> ExtractParameters(string sectionBody)
    {
        var parameters = new List<string>();
        foreach (Match match in ParameterPattern.Matches(sectionBody))
        {
            var parameter = match.Groups["parameter"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(parameter)
                && !parameter.Contains("imported", StringComparison.OrdinalIgnoreCase)
                && !parameter.Contains("cad", StringComparison.OrdinalIgnoreCase)
                && !parameters.Contains(parameter, StringComparer.OrdinalIgnoreCase))
            {
                parameters.Add(parameter);
            }
        }

        return parameters;
    }

    private static bool ContainsAny(string prompt, params string[] values)
    {
        foreach (var value in values)
        {
            if (prompt.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"(?is)\bdoors\s*:\s*(?<body>.*?)(?=\n\s*\w+\s*:|$)", RegexOptions.Compiled)]
    private static partial Regex DoorsSectionRegex();

    [GeneratedRegex(@"(?is)\bwindows\s*:\s*(?<body>.*?)(?=\n\s*\w+\s*:|$)", RegexOptions.Compiled)]
    private static partial Regex WindowsSectionRegex();

    [GeneratedRegex(@"(?is)\bfurniture\s*:\s*(?<body>.*?)(?=\n\s*\w+\s*:|$)", RegexOptions.Compiled)]
    private static partial Regex FurnitureSectionRegex();

    [GeneratedRegex(@"(?is)\b(?:mechanical\s+equipment|equipment)\s*:\s*(?<body>.*?)(?=\n\s*\w+\s*:|$)", RegexOptions.Compiled)]
    private static partial Regex EquipmentSectionRegex();

    [GeneratedRegex(@"(?<path>[A-Za-z]:\\[^\r\n""']+\.txt)", RegexOptions.Compiled)]
    private static partial Regex SharedParameterPathRegex();

    [GeneratedRegex(@"(?i)(?:name\s+)?(?:starts\s+with|prefix(?:\s+is)?)\s+(?<prefix>[A-Za-z0-9_]+)", RegexOptions.Compiled)]
    private static partial Regex PrefixRegex();

    [GeneratedRegex(@"(?i)(?:contain|contains|require|required)\s+(?<parameter>[A-Za-z][A-Za-z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex ParameterRegex();
}

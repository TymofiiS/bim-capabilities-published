namespace BIMCapabilities.Generation.Parsing;

internal sealed class CategorySpecification
{
    public required string CategoryName { get; init; }

    public string? RequiredPrefix { get; init; }

    public List<string> RequiredParameters { get; init; } = [];

    public bool ExcludeImportedCad { get; init; }
}

internal sealed class ParsedRuleSpecification
{
    public required string DisciplineCode { get; init; }

    public required string Topic { get; init; }

    public string RuleVersion { get; init; } = "V01";

    public string? SharedParameterFilePath { get; init; }

    public bool GenerateReport { get; init; } = true;

    public List<CategorySpecification> Categories { get; init; } = [];

    public string RuleId => $"STD-{DisciplineCode}-{Topic}-{RuleVersion}";

    public string Domain => Topic switch
    {
        "EQUIPMENT" => "MEP Equipment",
        "FURNITURE" => "Furniture",
        _ => "Openings"
    };
}

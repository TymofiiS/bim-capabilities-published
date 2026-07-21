namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Object scope supported by Naming Engine validation operations.
/// </summary>
public enum NamingEngineObjectScope
{
    Instance,

    Type,

    Family,

    Category,

    Model
}

/// <summary>
/// Case rule applied to naming validation criteria.
/// </summary>
public enum NamingCaseRule
{
    Unspecified,

    UpperCase,

    LowerCase,

    PascalCase,

    CamelCase,

    TitleCase
}

/// <summary>
/// Category scope applied to naming validation criteria.
/// </summary>
public sealed record NamingCategoryScopeCriteria
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Object scope applied to naming validation criteria.
/// </summary>
public sealed record NamingObjectScopeCriteria
{
    public NamingEngineObjectScope? Scope { get; init; }

    public IReadOnlyList<string>? ObjectIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyTypeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Definition of a single naming validation requirement.
/// </summary>
public sealed record NamingCriteriaDefinition
{
    public string? RequiredPrefix { get; init; }

    public string? RequiredSuffix { get; init; }

    public string? NamingPattern { get; init; }

    public string? RegularExpression { get; init; }

    public NamingCaseRule? CaseRule { get; init; }

    public string? CustomRuleIdentifier { get; init; }

    public NamingCategoryScopeCriteria? CategoryScope { get; init; }

    public NamingObjectScopeCriteria? ObjectScope { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Criteria applied when validating naming for Naming Engine operations.
/// </summary>
public sealed record NamingValidationCriteria
{
    public IReadOnlyList<NamingCriteriaDefinition>? Rules { get; init; }

    public string? RequiredPrefix { get; init; }

    public string? RequiredSuffix { get; init; }

    public string? NamingPattern { get; init; }

    public string? RegularExpression { get; init; }

    public NamingCaseRule? CaseRule { get; init; }

    public string? CustomRuleIdentifier { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

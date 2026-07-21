namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Object scope supported by Parameter Engine validation operations.
/// </summary>
public enum ParameterEngineObjectScope
{
    Instance,

    Type,

    Family,

    Category,

    Model
}

/// <summary>
/// Category scope applied to parameter validation criteria.
/// </summary>
public sealed record ParameterCategoryScopeCriteria
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Object scope applied to parameter validation criteria.
/// </summary>
public sealed record ParameterObjectScopeCriteria
{
    public ParameterEngineObjectScope? Scope { get; init; }

    public IReadOnlyList<string>? ObjectIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyTypeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Shared parameter file reference supplied by rule configuration.
/// </summary>
public sealed record ParameterSharedParameterFileReference
{
    public required string FilePath { get; init; }

    public string? FileVersion { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Definition of a single parameter validation requirement.
/// </summary>
public sealed record ParameterCriteriaDefinition
{
    public required string ParameterName { get; init; }

    public bool Required { get; init; } = true;

    public bool? SharedParameterRequired { get; init; }

    public bool? ValueRequired { get; init; }

    public ParameterCategoryScopeCriteria? CategoryScope { get; init; }

    public ParameterObjectScopeCriteria? ObjectScope { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Criteria applied when validating parameters for Parameter Engine operations.
/// </summary>
public sealed record ParameterValidationCriteria
{
    public IReadOnlyList<ParameterCriteriaDefinition>? Parameters { get; init; }

    public ParameterSharedParameterFileReference? SharedParameterFile { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

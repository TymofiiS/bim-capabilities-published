namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Parameter name filter criteria for parameter retrieval.
/// </summary>
public sealed record ParameterNameFilter
{
    public IReadOnlyList<string>? ExactNames { get; init; }

    public string? NamePattern { get; init; }
}

/// <summary>
/// Shared parameter filter criteria for parameter retrieval.
/// </summary>
public sealed record SharedParameterFilter
{
    public IReadOnlyList<string>? SharedParameterNames { get; init; }

    public IReadOnlyList<string>? SharedParameterGuids { get; init; }

    public string? SharedParameterFilePath { get; init; }

    public bool? MustExist { get; init; }
}

/// <summary>
/// Value-based filter criteria for parameter retrieval.
/// </summary>
public sealed record ParameterValueFilter
{
    public string? ExpectedValue { get; init; }

    public bool? MustHaveValue { get; init; }

    public bool? MustBeEmpty { get; init; }
}

/// <summary>
/// Category-based filter criteria for parameter retrieval.
/// </summary>
public sealed record ParameterCategoryFilter
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Object-based filter criteria for parameter retrieval.
/// </summary>
public sealed record ParameterObjectFilter
{
    public IReadOnlyList<string>? ObjectIdentifiers { get; init; }

    public string? ObjectKind { get; init; }
}

/// <summary>
/// Filter criteria applied to a parameter retrieval query.
/// </summary>
public sealed record ParameterQueryFilter
{
    public ParameterNameFilter? ParameterName { get; init; }

    public SharedParameterFilter? SharedParameter { get; init; }

    public ParameterValueFilter? Value { get; init; }

    public ParameterCategoryFilter? Category { get; init; }

    public ParameterObjectFilter? Object { get; init; }
}

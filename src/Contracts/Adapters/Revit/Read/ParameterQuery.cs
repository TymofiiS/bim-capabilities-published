namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Input for retrieving normalized parameters from the Revit Adapter read layer.
/// </summary>
public sealed record ParameterQuery
{
    public IReadOnlyList<string>? ParameterNames { get; init; }

    public IReadOnlyList<string>? SharedParameterNames { get; init; }

    public IReadOnlyList<string>? SharedParameterGuids { get; init; }

    public IReadOnlyList<string>? BuiltInParameterNames { get; init; }

    public IReadOnlyList<string>? Categories { get; init; }

    public ParameterQueryScope? Scope { get; init; }

    public ParameterObjectScope? ObjectScope { get; init; }

    public ParameterQueryFilter? Filter { get; init; }

    public ParameterSharedParameterFileReference? SharedParameterFile { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }
}

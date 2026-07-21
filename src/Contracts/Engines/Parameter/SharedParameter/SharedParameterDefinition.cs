namespace BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

/// <summary>
/// Expected shared parameter definition loaded from a user-provided shared parameter file.
/// </summary>
public sealed record SharedParameterDefinition
{
    public required string Name { get; init; }

    public string? Guid { get; init; }

    public string? DataType { get; init; }

    public string? Group { get; init; }
}

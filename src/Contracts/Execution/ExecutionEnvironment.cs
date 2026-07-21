namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Platform-neutral description of the execution session and target model.
/// </summary>
public sealed record ExecutionEnvironment
{
    public required string Platform { get; init; }

    public string? PlatformVersion { get; init; }

    public string? SessionId { get; init; }

    public string? ModelIdentifier { get; init; }

    public string? ModelName { get; init; }
}

namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Diagnostic emitted while converting naming findings into write requests.
/// </summary>
public sealed record NamingWriteRequestBuildDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public NamingWriteRequestBuildDiagnosticSeverity Severity { get; init; }

    public string? ObjectId { get; init; }

    public string? CurrentName { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

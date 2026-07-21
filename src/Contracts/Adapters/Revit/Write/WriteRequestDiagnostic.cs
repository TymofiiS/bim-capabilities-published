namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Diagnostic emitted during write request preparation or execution.
/// </summary>
public sealed record WriteRequestDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public WriteRequestDiagnosticSeverity Severity { get; init; }

    public string? RequestId { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

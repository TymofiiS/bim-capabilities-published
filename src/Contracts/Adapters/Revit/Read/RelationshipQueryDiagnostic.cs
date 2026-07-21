namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Diagnostic emitted during relationship retrieval.
/// </summary>
public sealed record RelationshipQueryDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public RelationshipQueryDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

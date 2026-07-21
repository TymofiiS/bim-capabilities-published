namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Diagnostic emitted during family retrieval.
/// </summary>
public sealed record FamilyQueryDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public FamilyQueryDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

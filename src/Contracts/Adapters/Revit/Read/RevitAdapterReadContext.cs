using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Input for executing a coordinated Revit Adapter read workflow.
/// </summary>
public sealed record RevitAdapterReadContext
{
    public FamilyQuery? FamilyQuery { get; init; }

    public ParameterQuery? ParameterQuery { get; init; }

    public RelationshipQuery? RelationshipQuery { get; init; }

    public IReadOnlyList<ObjectTranslationQuery>? TranslationQueries { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }
}

/// <summary>
/// Diagnostic emitted during a coordinated Revit Adapter read workflow.
/// </summary>
public sealed record RevitAdapterReadDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public RevitAdapterReadDiagnosticSeverity Severity { get; init; }

    public string? Source { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

/// <summary>
/// Severity classification for Revit Adapter read diagnostics.
/// </summary>
public enum RevitAdapterReadDiagnosticSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Aggregate statistics for a coordinated Revit Adapter read workflow.
/// </summary>
public sealed record RevitAdapterStatistics
{
    public int FamiliesRetrieved { get; init; }

    public int ParametersRetrieved { get; init; }

    public int RelationshipsRetrieved { get; init; }

    public int TranslationsRetrieved { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Metadata describing a coordinated Revit Adapter read workflow execution.
/// </summary>
public sealed record RevitAdapterReadMetadata
{
    public string? CorrelationId { get; init; }

    public DateTimeOffset ExecutedAt { get; init; }

    public string? AdapterId { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Result of a coordinated Revit Adapter read workflow.
/// </summary>
public sealed record RevitAdapterReadResult
{
    public FamilyQueryResult? Families { get; init; }

    public ParameterQueryResult? Parameters { get; init; }

    public RelationshipQueryResult? Relationships { get; init; }

    public IReadOnlyList<ObjectTranslationResult>? Translations { get; init; }

    public RevitAdapterStatistics? Statistics { get; init; }

    public RevitAdapterReadMetadata? Metadata { get; init; }

    public IReadOnlyList<RevitAdapterReadDiagnostic>? Diagnostics { get; init; }
}

using BIMCapabilities.Contracts.Engines.Naming;

namespace BIMCapabilities.Contracts.Engines.Naming.Pattern;

/// <summary>
/// Pattern validation rule applied to object names.
/// </summary>
public sealed record NamingPatternRule
{
    public string? RegularExpression { get; init; }

    public string? TokenizedPattern { get; init; }

    public int? MinimumLength { get; init; }

    public int? MaximumLength { get; init; }

    public string? AllowedCharacters { get; init; }

    public IReadOnlyList<string>? ForbiddenCharacters { get; init; }

    public NamingCaseRule? CaseRule { get; init; }

    public bool AllowNumericTokenStart { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

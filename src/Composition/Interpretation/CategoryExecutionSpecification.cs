using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Composition.Interpretation;

internal sealed record CategoryExecutionSpecification
{
    public required string CategoryName { get; init; }

    public IReadOnlyList<string> RequiredParameters { get; init; } = [];

    public IReadOnlyDictionary<string, string> ParameterDefaults { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> ParameterFillRules { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Shared-parameter binding per parameter name. <c>true</c> = instance, <c>false</c> = type (default).
    /// </summary>
    public IReadOnlyDictionary<string, bool> ParameterBindings { get; init; }
        = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public string? RequiredPrefix { get; init; }

    public PrefixFixScope PrefixFixScope { get; init; }

    public bool ExcludeImportedCad { get; init; }
}

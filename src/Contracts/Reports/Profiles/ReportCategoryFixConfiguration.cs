using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Per-category automatic correction settings reflected in compliance reports.
/// </summary>
public sealed record ReportCategoryFixConfiguration
{
    public string? RequiredPrefix { get; init; }

    public PrefixFixScope PrefixFixScope { get; init; }
}

using System.Text.Json;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Rendering;

namespace BIMCapabilities.Engines.Report.Rendering;

/// <summary>
/// Renders prepared report output into deterministic JSON.
/// </summary>
public sealed class JsonReportRenderer : IJsonReportRenderer
{
    public string Format => "json";

    public JsonRenderResult Render(ReportOutput report)
    {
        ArgumentGuard.ThrowIfNull(report);

        var normalized = Normalize(report);
        var json = JsonSerializer.Serialize(normalized, ReportOutputJsonSerialization.Options);

        return new JsonRenderResult
        {
            Json = json,
            DocumentContent = json,
            Title = report.Title,
            ContentType = "application/json; charset=utf-8"
        };
    }

    private static ReportOutput Normalize(ReportOutput report)
    {
        return report with
        {
            Metadata = NormalizeMetadata(report.Metadata),
            Sections = report.Sections
                .OrderBy(section => section.Order)
                .ThenBy(section => section.Name, StringComparer.Ordinal)
                .Select(NormalizeSection)
                .ToArray()
        };
    }

    private static ReportMetadata? NormalizeMetadata(ReportMetadata? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        return metadata with
        {
            Properties = SortDictionary(metadata.Properties)
        };
    }

    private static ReportSection NormalizeSection(ReportSection section)
    {
        if (section.Content is null)
        {
            return section;
        }

        return section with
        {
            Content = section.Content with
            {
                StructuredData = SortDictionary(section.Content.StructuredData)
            }
        };
    }

    private static IReadOnlyDictionary<string, string>? SortDictionary(IReadOnlyDictionary<string, string>? dictionary)
    {
        if (dictionary is null)
        {
            return null;
        }

        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in dictionary.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            sorted[pair.Key] = pair.Value;
        }

        return sorted;
    }
}

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Engines.Report.Tests.Verification;

internal static partial class ReportRendererConsistencyVerifier
{
    private static readonly HashSet<string> CoordinatorHtmlSections =
    [
        "Compliance Summary",
        "Project Impact",
        "Business Impact",
        "Root Cause",
        "Automatic Correction Preview",
        "Grouped Findings",
        "Recommendations"
    ];

    internal static void AssertConsistent(ReportOutput output, string html, string json)
    {
        AssertValidJson(json);
        AssertValidHtml(html);

        Assert.Contains(output.Title, html, StringComparison.Ordinal);
        Assert.Contains($"\"title\": \"{EscapeJsonString(output.Title)}\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"reportId\": \"{output.ReportId}\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"profileId\": \"{output.ProfileId}\"", json, StringComparison.Ordinal);

        if (output.Metadata?.RuleId is not null)
        {
            Assert.Contains($"\"ruleId\": \"{output.Metadata.RuleId}\"", json, StringComparison.Ordinal);
        }

        foreach (var section in output.Sections.OrderBy(section => section.Order))
        {
            Assert.Contains($"\"name\": \"{section.Name}\"", json, StringComparison.Ordinal);

            if (CoordinatorHtmlSections.Contains(section.Name))
            {
                var heading = MapCoordinatorHtmlHeading(section.Name);
                if (heading is not null && SectionShouldRenderInHtml(section))
                {
                    Assert.Contains(heading, html, StringComparison.Ordinal);
                }
            }

            if (section.Content?.Text is not null)
            {
                Assert.Contains(section.Content.Text, json, StringComparison.Ordinal);
            }

            if (section.Content?.StructuredData is not null)
            {
                foreach (var pair in section.Content.StructuredData)
                {
                    if (CoordinatorHtmlSections.Contains(section.Name)
                        && ShouldRenderStructuredValueInCoordinatorHtml(section.Name, pair.Key))
                    {
                        AssertContainsEncoded(pair.Value, html);
                    }

                    Assert.Contains($"\"{pair.Key}\": \"{EscapeJsonString(pair.Value)}\"", json, StringComparison.Ordinal);
                }
            }

            if (section.Content?.EvidenceReferences is not null)
            {
                foreach (var reference in section.Content.EvidenceReferences)
                {
                    Assert.Contains($"\"referenceId\": \"{reference.ReferenceId}\"", json, StringComparison.Ordinal);

                    if (reference.Description is not null)
                    {
                        Assert.Contains(reference.Description, json, StringComparison.Ordinal);
                    }
                }
            }

            if (section.Content?.DiagnosticReferences is not null)
            {
                foreach (var reference in section.Content.DiagnosticReferences)
                {
                    Assert.Contains($"\"referenceId\": \"{reference.ReferenceId}\"", json, StringComparison.Ordinal);

                    if (reference.Description is not null)
                    {
                        Assert.Contains(reference.Description, json, StringComparison.Ordinal);
                    }
                }
            }

            if (section.Content?.Attachments is not null)
            {
                foreach (var attachment in section.Content.Attachments)
                {
                    Assert.Contains($"\"attachmentId\": \"{attachment.AttachmentId}\"", json, StringComparison.Ordinal);
                    Assert.Contains($"\"fileName\": \"{attachment.FileName}\"", json, StringComparison.Ordinal);
                }
            }
        }
    }

    private static bool SectionShouldRenderInHtml(ReportSection section)
    {
        if (section.Name == "Automatic Correction Preview")
        {
            var data = section.Content?.StructuredData;
            return data?.GetValueOrDefault("available") == "True"
                && int.TryParse(data.GetValueOrDefault("correctionPreviewLineCount"), out var count)
                && count > 0;
        }

        return true;
    }

    private static bool ShouldRenderStructuredValueInCoordinatorHtml(string sectionName, string key)
    {
        return sectionName switch
        {
        "Compliance Summary" => key is "ruleName" or "executionDate" or "resultStatus"
                or "checkedObjects" or "passedObjects" or "failedObjects" or "issuesFound" or "compliancePercentage",
            "Project Impact" => key.Contains("Line[", StringComparison.Ordinal),
            "Business Impact" => key.Contains("Line[", StringComparison.Ordinal),
            "Root Cause" => key is "findingCount" or "narrative",
            "Automatic Correction Preview" => key.Contains("Line[", StringComparison.Ordinal),
            "Grouped Findings" => key.EndsWith(".issueTitle", StringComparison.Ordinal)
                || key.EndsWith(".count", StringComparison.Ordinal)
                || key.EndsWith(".whyFailed", StringComparison.Ordinal)
                || key.Contains(".fixStep[", StringComparison.Ordinal)
                || key.Contains(".familyGroup[", StringComparison.Ordinal)
                || key.EndsWith(".severity", StringComparison.Ordinal),
            "Recommendations" => key.StartsWith("recommendation[", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string? MapCoordinatorHtmlHeading(string sectionName)
    {
        return sectionName switch
        {
            "Compliance Summary" => "Result Summary",
            "Project Impact" => "Project Impact",
            "Business Impact" => "Business Impact",
            "Root Cause" => "Root Cause",
            "Automatic Correction Preview" => "Automatic Correction Available",
            "Grouped Findings" => "Grouped Findings",
            "Recommendations" => "Recommendations",
            _ => null
        };
    }

    internal static void AssertValidJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    internal static void AssertValidHtml(string html)
    {
        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(HtmlScriptPattern(), html);
    }

    private static void AssertContainsEncoded(string expected, string html)
    {
        Assert.Contains(WebUtility.HtmlEncode(expected), html, StringComparison.Ordinal);
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    [GeneratedRegex("<script", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlScriptPattern();
}

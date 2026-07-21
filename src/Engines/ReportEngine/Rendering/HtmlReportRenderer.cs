using System.Net;
using System.Text;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Engines.Report.Profiles;

namespace BIMCapabilities.Engines.Report.Rendering;

/// <summary>
/// Renders prepared report output into a coordinator-facing HTML5 document.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    public string Format => "html";

    public HtmlRenderResult Render(ReportOutput report)
    {
        ArgumentGuard.ThrowIfNull(report);

        var html = string.Equals(report.ProfileId, CorrectionReportProfileDefinition.ProfileId, StringComparison.Ordinal)
            ? BuildCorrectionDocument(report)
            : BuildDocument(report);

        return new HtmlRenderResult
        {
            Html = html,
            FileContent = html,
            Title = report.Title,
            ContentType = "text/html; charset=utf-8"
        };
    }

    private static string BuildCorrectionDocument(ReportOutput report)
    {
        var builder = new StringBuilder();
        var summary = FindSection(report, CorrectionReportProfileSections.CorrectionSummary)?.Content?.StructuredData;

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("""<html lang="en">""");
        builder.AppendLine("<head>");
        builder.AppendLine("""  <meta charset="utf-8">""");
        builder.AppendLine("""  <meta name="viewport" content="width=device-width, initial-scale=1">""");
        builder.AppendLine($"  <title>{Encode(report.Title)}</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine(GetStyles());
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("""  <main class="validation-report">""");
        builder.AppendLine("    <header class=\"report-header\">");
        builder.AppendLine("      <p class=\"eyebrow\">Correction Report</p>");
        builder.AppendLine($"      <h1>{Encode(report.Title)}</h1>");
        builder.AppendLine("    </header>");

        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Rule Information</h2>");
        builder.AppendLine("      <dl class=\"info-grid\">");
        AppendInfoItem(builder, "Rule Name", summary?.GetValueOrDefault("ruleName"));
        AppendInfoItem(builder, "Execution Date", summary?.GetValueOrDefault("executionDate") ?? report.GeneratedAt.ToString("u"));
        builder.AppendLine("      </dl>");
        builder.AppendLine("    </section>");

        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Correction Complete</h2>");
        builder.AppendLine("      <dl class=\"info-grid\">");
        AppendInfoItem(builder, "Parameters Added", summary?.GetValueOrDefault("parametersAdded"));
        AppendInfoItem(builder, "Values Assigned", summary?.GetValueOrDefault("valuesAssigned"));
        AppendInfoItem(builder, "Names Renamed", summary?.GetValueOrDefault("namesRenamed"));
        AppendInfoItem(builder, "Affected Types", summary?.GetValueOrDefault("affectedTypes"));
        AppendInfoItem(builder, "Affected Families", summary?.GetValueOrDefault("affectedFamilies"));
        AppendInfoItem(builder, "Affected Instances", summary?.GetValueOrDefault("affectedInstances"));
        builder.AppendLine("      </dl>");

        if (int.TryParse(summary?.GetValueOrDefault("defaultValueCount"), out var defaultValueCount) && defaultValueCount > 0)
        {
            builder.AppendLine("      <h3>Default Value Applied</h3>");
            builder.AppendLine("      <ul>");
            for (var index = 0; index < defaultValueCount; index++)
            {
                var value = summary?.GetValueOrDefault($"defaultValue[{index}]");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    builder.AppendLine($"        <li>{Encode(value)}</li>");
                }
            }

            builder.AppendLine("      </ul>");
        }

        builder.AppendLine("    </section>");
        builder.AppendLine("  </main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static string BuildDocument(ReportOutput report)
    {
        var builder = new StringBuilder();
        var summary = FindSection(report, ComplianceReportProfileSections.ComplianceSummary)?.Content?.StructuredData;
        var projectImpact = FindSection(report, ComplianceReportProfileSections.ProjectImpact)?.Content?.StructuredData;
        var businessImpact = FindSection(report, ComplianceReportProfileSections.BusinessImpact)?.Content?.StructuredData;
        var rootCause = FindSection(report, ComplianceReportProfileSections.RootCause)?.Content?.StructuredData;
        var correctionPreview = FindSection(report, ComplianceReportProfileSections.AutomaticCorrectionPreview)?.Content?.StructuredData;
        var validationScope = FindSection(report, ComplianceReportProfileSections.ValidationScope)?.Content?.StructuredData;
        var groupedFindings = FindSection(report, ComplianceReportProfileSections.GroupedFindings)?.Content?.StructuredData;
        var recommendations = FindSection(report, ComplianceReportProfileSections.Recommendations)?.Content?.StructuredData;

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("""<html lang="en">""");
        builder.AppendLine("<head>");
        builder.AppendLine("""  <meta charset="utf-8">""");
        builder.AppendLine("""  <meta name="viewport" content="width=device-width, initial-scale=1">""");
        builder.AppendLine($"  <title>{Encode(report.Title)}</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine(GetStyles());
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("""  <main class="validation-report">""");
        builder.AppendLine("    <header class=\"report-header\">");
        builder.AppendLine("      <p class=\"eyebrow\">Validation Report</p>");
        builder.AppendLine($"      <h1>{Encode(report.Title)}</h1>");
        builder.AppendLine("    </header>");

        RenderRuleInformation(builder, summary, report.GeneratedAt);
        RenderResultSummary(builder, summary);
        RenderStructuredSection(builder, "Project Impact", projectImpact, "projectImpact");
        RenderStructuredSection(builder, "Business Impact", businessImpact, "businessImpact");
        RenderRootCause(builder, rootCause);
        RenderStructuredSection(builder, "Automatic Correction Available", correctionPreview, "correctionPreview");
        RenderValidationScope(builder, validationScope);
        RenderGroupedFindings(builder, groupedFindings);
        RenderRecommendations(builder, recommendations);

        builder.AppendLine("  </main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void RenderRuleInformation(StringBuilder builder, IReadOnlyDictionary<string, string>? summary, DateTimeOffset generatedAt)
    {
        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Rule Information</h2>");
        builder.AppendLine("      <dl class=\"info-grid\">");
        AppendInfoItem(builder, "Rule Name", summary?.GetValueOrDefault("ruleName"));
        AppendInfoItem(builder, "Execution Date", summary?.GetValueOrDefault("executionDate") ?? generatedAt.ToString("u"));
        builder.AppendLine("      </dl>");
        builder.AppendLine("    </section>");
    }

    private static void RenderStructuredSection(
        StringBuilder builder,
        string title,
        IReadOnlyDictionary<string, string>? structuredData,
        string linePrefix)
    {
        if (structuredData is null
            || !structuredData.TryGetValue($"{linePrefix}LineCount", out var countText)
            || !int.TryParse(countText, out var lineCount)
            || lineCount == 0)
        {
            return;
        }

        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine($"      <h2>{Encode(title)}</h2>");
        builder.AppendLine("      <dl class=\"info-grid\">");
        for (var index = 0; index < lineCount; index++)
        {
            var label = structuredData.GetValueOrDefault($"{linePrefix}Line[{index}].label");
            var value = structuredData.GetValueOrDefault($"{linePrefix}Line[{index}].value");
            AppendInfoItem(builder, label, value);
        }

        builder.AppendLine("      </dl>");
        builder.AppendLine("    </section>");
    }

    private static void RenderRootCause(StringBuilder builder, IReadOnlyDictionary<string, string>? rootCause)
    {
        var narrative = rootCause?.GetValueOrDefault("narrative");
        if (string.IsNullOrWhiteSpace(narrative))
        {
            return;
        }

        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Root Cause</h2>");
        foreach (var line in narrative.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine("      <br>");
                continue;
            }

            builder.AppendLine($"      <p>{Encode(line.TrimEnd())}</p>");
        }

        builder.AppendLine("    </section>");
    }

    private static void RenderValidationScope(StringBuilder builder, IReadOnlyDictionary<string, string>? validationScope)
    {
        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Validation Scope</h2>");

        if (validationScope is null || !validationScope.TryGetValue("scopeLineCount", out var countText))
        {
            builder.AppendLine("      <p class=\"empty-state\">Validation scope information is unavailable for this report.</p>");
            builder.AppendLine("    </section>");
            return;
        }

        var lineCount = int.TryParse(countText, out var parsedCount) ? parsedCount : 0;
        builder.AppendLine("      <dl class=\"info-grid scope-grid\">");
        for (var index = 0; index < lineCount; index++)
        {
            var label = validationScope.GetValueOrDefault($"scopeLine[{index}].label");
            var value = validationScope.GetValueOrDefault($"scopeLine[{index}].value");
            AppendInfoItem(builder, label, value);
        }

        builder.AppendLine("      </dl>");

        var explanation = validationScope.GetValueOrDefault("whyCountsDiffer");
        if (!string.IsNullOrWhiteSpace(explanation))
        {
            builder.AppendLine("      <div class=\"scope-explanation\">");
            builder.AppendLine("        <h3>Why Counts Differ</h3>");
            builder.AppendLine($"        <p>{Encode(explanation)}</p>");
            builder.AppendLine("      </div>");
        }

        builder.AppendLine("    </section>");
    }

    private static void RenderResultSummary(StringBuilder builder, IReadOnlyDictionary<string, string>? summary)
    {
        var resultStatus = summary?.GetValueOrDefault("resultStatus") ?? "Unknown";
        var statusClass = string.Equals(resultStatus, "Pass", StringComparison.OrdinalIgnoreCase) ? "status-pass" : "status-fail";

        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Result Summary</h2>");
        builder.AppendLine($"      <p class=\"result-badge {statusClass}\">{Encode(resultStatus)}</p>");
        builder.AppendLine("      <div class=\"summary-grid\">");
        AppendMetricCard(builder, "Checked Objects", summary?.GetValueOrDefault("checkedObjects") ?? "0");
        AppendMetricCard(builder, "Passed Objects", summary?.GetValueOrDefault("passedObjects") ?? "0");
        AppendMetricCard(builder, "Failed Objects", summary?.GetValueOrDefault("failedObjects") ?? "0");
        AppendMetricCard(builder, "Issues Found", summary?.GetValueOrDefault("issuesFound") ?? "0");
        AppendMetricCard(builder, "Compliance", $"{summary?.GetValueOrDefault("compliancePercentage") ?? "0"}%");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </section>");
    }

    private static void RenderGroupedFindings(StringBuilder builder, IReadOnlyDictionary<string, string>? groupedFindings)
    {
        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Grouped Findings</h2>");

        var groupCount = int.TryParse(groupedFindings?.GetValueOrDefault("issueGroupCount"), out var parsedCount) ? parsedCount : 0;
        if (groupCount == 0)
        {
            builder.AppendLine("      <p class=\"empty-state\">No compliance issues were detected.</p>");
            builder.AppendLine("    </section>");
            return;
        }

        for (var index = 0; index < groupCount; index++)
        {
            var prefix = $"group[{index}]";
            var issueTitle = groupedFindings!.GetValueOrDefault($"{prefix}.issueTitle") ?? "Compliance issue";
            var severity = groupedFindings.GetValueOrDefault($"{prefix}.severity") ?? "Error";
            var count = groupedFindings.GetValueOrDefault($"{prefix}.count") ?? "0";
            var severityClass = severity.ToLowerInvariant();

            builder.AppendLine("      <article class=\"finding-card\">");
            builder.AppendLine("        <div class=\"finding-header\">");
            builder.AppendLine($"          <h3>{Encode(issueTitle)}</h3>");
            builder.AppendLine($"          <span class=\"severity severity-{Encode(severityClass)}\">{Encode(severity)}</span>");
            builder.AppendLine("        </div>");
            builder.AppendLine($"        <p class=\"finding-count\">Count: {Encode(count)}</p>");

            var whyFailed = groupedFindings.GetValueOrDefault($"{prefix}.whyFailed");
            if (!string.IsNullOrWhiteSpace(whyFailed))
            {
                builder.AppendLine("        <div class=\"finding-guidance\">");
                builder.AppendLine("          <h4>Why</h4>");
                builder.AppendLine($"          <p>{Encode(whyFailed)}</p>");
                builder.AppendLine("        </div>");
            }

            var fixStepCount = int.TryParse(groupedFindings.GetValueOrDefault($"{prefix}.fixStepCount"), out var parsedFixSteps)
                ? parsedFixSteps
                : 0;
            if (fixStepCount > 0)
            {
                builder.AppendLine("        <div class=\"finding-guidance\">");
                builder.AppendLine("          <h4>How to fix</h4>");
                builder.AppendLine("          <ol class=\"fix-steps\">");
                for (var fixStepIndex = 0; fixStepIndex < fixStepCount; fixStepIndex++)
                {
                    var fixStep = groupedFindings.GetValueOrDefault($"{prefix}.fixStep[{fixStepIndex}]") ?? string.Empty;
                    builder.AppendLine($"            <li>{Encode(fixStep)}</li>");
                }

                builder.AppendLine("          </ol>");
                builder.AppendLine("        </div>");
            }

            builder.AppendLine("        <div class=\"affected-list\">");
            builder.AppendLine("          <h4>Affected Families</h4>");

            var familyGroupCount = int.TryParse(groupedFindings.GetValueOrDefault($"{prefix}.familyGroupCount"), out var parsedFamilyGroups)
                ? parsedFamilyGroups
                : 0;

            for (var familyIndex = 0; familyIndex < familyGroupCount; familyIndex++)
            {
                var familyPrefix = $"{prefix}.familyGroup[{familyIndex}]";
                var familyName = groupedFindings.GetValueOrDefault($"{familyPrefix}.familyName") ?? "Unknown family";
                builder.AppendLine("          <article class=\"finding-card\">");
                builder.AppendLine($"            <h5>Family: {Encode(familyName)}</h5>");

                var typeCount = int.TryParse(groupedFindings.GetValueOrDefault($"{familyPrefix}.typeCount"), out var parsedTypeCount)
                    ? parsedTypeCount
                    : 0;
                if (typeCount > 0)
                {
                    builder.AppendLine("            <p>Affected Types:</p>");
                    builder.AppendLine("            <ul>");
                    for (var typeIndex = 0; typeIndex < typeCount; typeIndex++)
                    {
                        var typeName = groupedFindings.GetValueOrDefault($"{familyPrefix}.type[{typeIndex}]") ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(typeName))
                        {
                            builder.AppendLine($"              <li>{Encode(typeName)}</li>");
                        }
                    }

                    builder.AppendLine("            </ul>");
                }

                var placedInstances = groupedFindings.GetValueOrDefault($"{familyPrefix}.placedInstanceCount");
                if (!string.IsNullOrWhiteSpace(placedInstances) && placedInstances != "0")
                {
                    builder.AppendLine($"            <p>Placed Instances: {Encode(placedInstances)}</p>");
                }

                builder.AppendLine("          </article>");
            }

            builder.AppendLine("        </div>");
            builder.AppendLine("      </article>");
        }

        builder.AppendLine("    </section>");
    }

    private static void RenderRecommendations(StringBuilder builder, IReadOnlyDictionary<string, string>? recommendations)
    {
        builder.AppendLine("    <section class=\"report-section\">");
        builder.AppendLine("      <h2>Recommendations</h2>");

        var recommendationCount = int.TryParse(recommendations?.GetValueOrDefault("recommendationCount"), out var parsedCount) ? parsedCount : 0;
        if (recommendationCount == 0)
        {
            builder.AppendLine("      <p class=\"empty-state\">No remediation is required.</p>");
            builder.AppendLine("    </section>");
            return;
        }

        builder.AppendLine("      <ol class=\"recommendations\">");
        for (var index = 0; index < recommendationCount; index++)
        {
            var recommendation = recommendations!.GetValueOrDefault($"recommendation[{index}]") ?? string.Empty;
            builder.AppendLine($"        <li>{Encode(recommendation)}</li>");
        }

        builder.AppendLine("      </ol>");
        builder.AppendLine("    </section>");
    }

    private static ReportSection? FindSection(ReportOutput report, string sectionName)
    {
        return report.Sections.FirstOrDefault(section => section.Name == sectionName);
    }

    private static void AppendInfoItem(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.AppendLine($"        <dt>{Encode(label)}</dt>");
        builder.AppendLine($"        <dd>{Encode(value)}</dd>");
    }

    private static void AppendMetricCard(StringBuilder builder, string label, string value)
    {
        builder.AppendLine("        <div class=\"metric-card\">");
        builder.AppendLine($"          <span class=\"metric-label\">{Encode(label)}</span>");
        builder.AppendLine($"          <strong class=\"metric-value\">{Encode(value)}</strong>");
        builder.AppendLine("        </div>");
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string GetStyles()
    {
        return """
            :root {
              color-scheme: light;
              --text: #1f2937;
              --muted: #6b7280;
              --border: #d1d5db;
              --surface: #ffffff;
              --surface-muted: #f8fafc;
              --pass: #166534;
              --fail: #b91c1c;
              --warning: #b45309;
            }

            * { box-sizing: border-box; }

            body {
              margin: 0;
              font-family: "Segoe UI", Arial, sans-serif;
              line-height: 1.5;
              color: var(--text);
              background: #eef2f7;
            }

            .validation-report {
              max-width: 960px;
              margin: 0 auto;
              padding: 2rem 1.5rem 3rem;
            }

            .report-header {
              background: var(--surface);
              border: 1px solid var(--border);
              border-radius: 12px;
              padding: 1.5rem;
              margin-bottom: 1.5rem;
            }

            .eyebrow {
              margin: 0 0 0.5rem;
              text-transform: uppercase;
              letter-spacing: 0.08em;
              font-size: 0.75rem;
              color: var(--muted);
            }

            h1, h2, h3, h4 {
              margin: 0 0 0.75rem;
              line-height: 1.2;
            }

            .report-section {
              background: var(--surface);
              border: 1px solid var(--border);
              border-radius: 12px;
              padding: 1.5rem;
              margin-bottom: 1.5rem;
            }

            .info-grid {
              display: grid;
              grid-template-columns: max-content 1fr;
              gap: 0.5rem 1.5rem;
              margin: 0;
            }

            .info-grid dt {
              font-weight: 600;
              color: var(--muted);
            }

            .info-grid dd {
              margin: 0;
            }

            .result-badge {
              display: inline-block;
              padding: 0.35rem 0.75rem;
              border-radius: 999px;
              font-weight: 700;
              margin-bottom: 1rem;
            }

            .status-pass {
              background: #dcfce7;
              color: var(--pass);
            }

            .status-fail {
              background: #fee2e2;
              color: var(--fail);
            }

            .summary-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
              gap: 1rem;
            }

            .metric-card {
              background: var(--surface-muted);
              border: 1px solid var(--border);
              border-radius: 10px;
              padding: 1rem;
            }

            .metric-label {
              display: block;
              color: var(--muted);
              font-size: 0.875rem;
              margin-bottom: 0.35rem;
            }

            .metric-value {
              font-size: 1.5rem;
            }

            .finding-card {
              border: 1px solid var(--border);
              border-radius: 10px;
              padding: 1rem;
              margin-top: 1rem;
              background: var(--surface-muted);
            }

            .finding-header {
              display: flex;
              justify-content: space-between;
              gap: 1rem;
              align-items: start;
            }

            .severity {
              display: inline-block;
              padding: 0.2rem 0.55rem;
              border-radius: 999px;
              font-size: 0.75rem;
              font-weight: 700;
              text-transform: uppercase;
            }

            .severity-error, .severity-critical {
              background: #fee2e2;
              color: var(--fail);
            }

            .severity-warning {
              background: #fef3c7;
              color: var(--warning);
            }

            .finding-count {
              margin: 0.5rem 0 1rem;
              color: var(--muted);
            }

            .finding-guidance {
              margin-bottom: 1rem;
            }

            .finding-guidance h4 {
              margin: 0 0 0.35rem;
              font-size: 0.875rem;
              text-transform: uppercase;
              letter-spacing: 0.04em;
              color: var(--muted);
            }

            .finding-guidance p {
              margin: 0;
            }

            .fix-steps {
              margin: 0;
              padding-left: 1.25rem;
            }

            .affected-list ul, .recommendations {
              margin: 0;
              padding-left: 1.25rem;
            }

            .empty-state {
              margin: 0;
              color: var(--muted);
            }

            .scope-explanation {
              margin-top: 1.25rem;
              padding-top: 1rem;
              border-top: 1px solid var(--border);
            }

            .scope-explanation h3 {
              font-size: 1rem;
            }

            @media print {
              body { background: #ffffff; }
              .validation-report { max-width: none; padding: 0; }
              .report-section, .report-header, .finding-card {
                break-inside: avoid;
                box-shadow: none;
              }
            }
            """;
    }
}

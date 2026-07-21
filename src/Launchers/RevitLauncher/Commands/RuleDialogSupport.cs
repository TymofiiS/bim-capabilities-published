using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Launchers.Revit.Commands;

internal static class RuleDialogSupport
{
    internal static TaskDialog CreateTaskDialog(string title, string mainInstruction, string? mainContent = null)
    {
        var dialog = new TaskDialog("BimCapabilities.TaskDialog")
        {
            Title = title,
            MainInstruction = mainInstruction,
            CommonButtons = TaskDialogCommonButtons.Close
        };

        if (!string.IsNullOrWhiteSpace(mainContent))
        {
            dialog.MainContent = mainContent;
        }

        return dialog;
    }

    internal static TaskDialog CreateResultTaskDialog(string title, string statusLine)
    {
        return CreateTaskDialog(title, title, statusLine);
    }

    internal static void ShowError(string mainInstruction, string? details = null)
    {
        CreateTaskDialog(mainInstruction, mainInstruction, details).Show();
    }
    internal static string BuildFindingsSummary(ReportOutput? reportOutput, string issuesFound)
    {
        if (!int.TryParse(issuesFound, out var count) || count == 0)
        {
            return string.Empty;
        }

        var groupedFindings = reportOutput?.Sections
            .FirstOrDefault(section => section.Name == "Grouped Findings")
            ?.Content?.StructuredData;

        if (groupedFindings is null
            || !int.TryParse(groupedFindings.GetValueOrDefault("issueGroupCount"), out var groupCount)
            || groupCount == 0)
        {
            return $"Findings: {issuesFound} issue group(s) require attention.";
        }

        var titles = new List<string>();
        for (var index = 0; index < groupCount && index < 3; index++)
        {
            var title = groupedFindings.GetValueOrDefault($"group[{index}].issueTitle");
            if (!string.IsNullOrWhiteSpace(title))
            {
                titles.Add(title);
            }
        }

        var summary = titles.Count > 0
            ? string.Join("; ", titles)
            : $"{issuesFound} issue group(s)";

        if (groupCount > titles.Count)
        {
            summary += $" (+{groupCount - titles.Count} more)";
        }

        return summary;
    }

    internal static string? BuildCorrectionDescription(BimRule? rule)
    {
        if (rule?.Execution.FixEnabled != true)
        {
            return null;
        }

        var defaultValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var engine in rule.Engines ?? [])
        {
            foreach (var capability in engine.Capabilities ?? [])
            {
                if (capability.Configuration is null)
                {
                    continue;
                }

                foreach (var entry in capability.Configuration)
                {
                    if (entry.Key.EndsWith(".parameterDefaults", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var token in StringParsing.SplitCommaSeparated(entry.Value))
                        {
                            var separatorIndex = token.IndexOf('=');
                            if (separatorIndex > 0)
                            {
                                defaultValues[token[..separatorIndex].Trim()] = token[(separatorIndex + 1)..].Trim();
                            }
                        }

                        continue;
                    }

                    if (entry.Key.EndsWith(".parameterBinding", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var token in StringParsing.SplitCommaSeparated(entry.Value))
                        {
                            var separatorIndex = token.IndexOf('=');
                            if (separatorIndex > 0)
                            {
                                bindings[token[..separatorIndex].Trim()] = token[(separatorIndex + 1)..].Trim();
                            }
                        }
                    }
                }
            }
        }

        if (defaultValues.Count == 0 && bindings.Count == 0)
        {
            return "Create missing parameters defined in this rule and assign configured default values.";
        }

        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        parameterNames.UnionWith(defaultValues.Keys);
        parameterNames.UnionWith(bindings.Keys);

        var descriptions = parameterNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(parameterName =>
            {
                var parts = new List<string>();
                if (defaultValues.TryGetValue(parameterName, out var defaultValue))
                {
                    parts.Add(defaultValue);
                }

                var binding = bindings.TryGetValue(parameterName, out var configuredBinding)
                    ? configuredBinding
                    : "type";
                parts.Add($"({binding})");
                return $"{parameterName}={string.Join(" ", parts)}";
            })
            .ToArray();

        return "Create missing parameters and assign configured defaults with explicit type/instance binding: "
            + string.Join(", ", descriptions)
            + ".";
    }

    internal static bool CanApplyAutomaticCorrection(BimRule? rule, string resultStatus, string issuesFound)
    {
        return rule?.Execution.FixEnabled == true
            && string.Equals(resultStatus, "Fail", StringComparison.OrdinalIgnoreCase)
            && issuesFound != "0";
    }
}

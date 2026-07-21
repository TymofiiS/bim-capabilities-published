using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Rules.Loading;

namespace BIMCapabilities.Launchers.Revit.Diagnostics;

internal static class LauncherDiagnosticFormatter
{
    internal static string FormatLoadFailure(BimRuleLoadResult loadResult)
    {
        if (loadResult.Diagnostics.Count == 0)
        {
            return "The selected BIMRule file could not be loaded.";
        }

        return string.Join(
            Environment.NewLine,
            loadResult.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    }

    internal static string FormatValidationFailure(ValidationPipelineResult pipelineResult)
    {
        var messages = new List<string>();

        AppendValidationMessages(messages, "Structure", pipelineResult.StructureValidation?.Diagnostics);
        AppendValidationMessages(messages, "Version", pipelineResult.VersionValidation?.Diagnostics);
        AppendValidationMessages(messages, "Capability", pipelineResult.CapabilityValidation?.Diagnostics);

        return messages.Count == 0
            ? "The selected BIMRule file failed validation."
            : string.Join(Environment.NewLine, messages);
    }

    internal static string FormatExecutionFailure(Exception exception)
    {
        return $"Validation execution failed: {exception.Message}";
    }

    private static void AppendValidationMessages<TDiagnostic>(
        ICollection<string> messages,
        string stage,
        IReadOnlyList<TDiagnostic>? diagnostics)
        where TDiagnostic : class
    {
        if (diagnostics is null)
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            var code = diagnostic.GetType().GetProperty("Code")?.GetValue(diagnostic)?.ToString();
            var message = diagnostic.GetType().GetProperty("Message")?.GetValue(diagnostic)?.ToString();
            messages.Add($"{stage}: {code}: {message}");
        }
    }
}

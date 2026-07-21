using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Resolves rule-relative external resource paths for launcher execution.
/// </summary>
public static class LauncherPathResolver
{
    public static string? ResolveSharedParameterFilePath(
        string ruleFilePath,
        BimRule rule,
        string? overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var referencedPath = rule.ExternalReferences?
            .FirstOrDefault(reference =>
                string.Equals(reference.ReferenceType, "SharedParameterFile", StringComparison.OrdinalIgnoreCase))
            ?.Location;

        if (string.IsNullOrWhiteSpace(referencedPath))
        {
            return null;
        }

        if (Path.IsPathRooted(referencedPath) && File.Exists(referencedPath))
        {
            return referencedPath;
        }

        var ruleDirectory = Path.GetDirectoryName(ruleFilePath)
            ?? throw new InvalidOperationException("Rule file path must include a directory.");

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(ruleDirectory, referencedPath)),
            Path.GetFullPath(Path.Combine(ruleDirectory, "..", referencedPath)),
            Path.GetFullPath(Path.Combine(ruleDirectory, "..", "..", referencedPath))
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    public static string ResolveReportDirectory(string? correlationId = null)
    {
        var directoryName = string.IsNullOrWhiteSpace(correlationId)
            ? $"run-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : correlationId;

        return Path.Combine(
            Path.GetTempPath(),
            "BIMCapabilities",
            directoryName);
    }
}

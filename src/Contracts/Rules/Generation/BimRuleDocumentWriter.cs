using System.Text.Json;

namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Serializes BIMRule models to .bimrule JSON documents.
/// </summary>
public static class BimRuleDocumentWriter
{
    public static string Serialize(BimRule rule)
    {
        ArgumentGuard.ThrowIfNull(rule);

        return JsonSerializer.Serialize(rule, CreateWriteOptions());
    }

    public static void Write(BimRule rule, string filePath)
    {
        ArgumentGuard.ThrowIfNull(rule);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, Serialize(rule));
    }

    private static JsonSerializerOptions CreateWriteOptions()
    {
        return new JsonSerializerOptions(BimRuleSerialization.Options)
        {
            WriteIndented = true
        };
    }
}

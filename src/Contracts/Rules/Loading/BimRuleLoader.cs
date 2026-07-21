using System.Text.Json;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Loading;

/// <summary>
/// Reads and deserializes .bimrule files into the BIMRule model.
/// </summary>
public sealed class BimRuleLoader : IBimRuleLoader
{
    public BimRuleLoadResult Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.FileNotFound,
                "A file path is required.",
                filePath);
        }

        if (!File.Exists(filePath))
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.FileNotFound,
                $"The rule file was not found: {filePath}",
                filePath);
        }

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.FileNotFound,
                $"The rule file could not be read: {exception.Message}",
                filePath);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.FileEmpty,
                "The rule file is empty.",
                filePath);
        }

        try
        {
            using var _ = JsonDocument.Parse(content);
        }
        catch (JsonException exception)
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.InvalidFormat,
                $"The rule file is not valid JSON: {exception.Message}",
                filePath);
        }

        try
        {
            var rule = JsonSerializer.Deserialize<BimRule>(content, BimRuleSerialization.Options);
            if (rule is null)
            {
                return Failure(
                    BimRuleLoadDiagnosticCodes.DeserializationFailure,
                    "The rule file did not deserialize into a BIMRule model.",
                    filePath);
            }

            return new BimRuleLoadResult { Rule = rule };
        }
        catch (JsonException exception)
        {
            return Failure(
                BimRuleLoadDiagnosticCodes.DeserializationFailure,
                $"The rule file could not be deserialized into a BIMRule model: {exception.Message}",
                filePath);
        }
    }

    private static BimRuleLoadResult Failure(string code, string message, string? filePath)
    {
        return new BimRuleLoadResult
        {
            Diagnostics =
            [
                new BimRuleLoadDiagnostic
                {
                    Code = code,
                    Message = message,
                    FilePath = filePath
                }
            ]
        };
    }
}

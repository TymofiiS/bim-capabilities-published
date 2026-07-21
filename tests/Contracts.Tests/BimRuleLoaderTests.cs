using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Loading;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleLoaderTests
{
    private readonly BimRuleLoader _loader = new();

    [Fact]
    public void Load_returns_rule_for_valid_bimrule_file()
    {
        var filePath = CreateTempRuleFile(BimRuleTestData.CreateDemoRule());

        try
        {
            var result = _loader.Load(filePath);

            Assert.True(result.Success);
            Assert.NotNull(result.Rule);
            Assert.Empty(result.Diagnostics);
            Assert.Equal("STD-ARC-OPENINGS-V01", result.Rule.Metadata.RuleId);
            Assert.Equal(4, result.Rule.Engines.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_returns_file_not_found_diagnostic_for_missing_file()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.bimrule");

        var result = _loader.Load(filePath);

        Assert.False(result.Success);
        Assert.Null(result.Rule);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleLoadDiagnosticCodes.FileNotFound, diagnostic.Code);
        Assert.Equal(filePath, diagnostic.FilePath);
    }

    [Fact]
    public void Load_returns_file_empty_diagnostic_for_empty_file()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.bimrule");
        File.WriteAllText(filePath, string.Empty);

        try
        {
            var result = _loader.Load(filePath);

            Assert.False(result.Success);
            Assert.Null(result.Rule);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(BimRuleLoadDiagnosticCodes.FileEmpty, diagnostic.Code);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_returns_invalid_format_diagnostic_for_malformed_file()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"malformed-{Guid.NewGuid():N}.bimrule");
        File.WriteAllText(filePath, "{ this is not valid json");

        try
        {
            var result = _loader.Load(filePath);

            Assert.False(result.Success);
            Assert.Null(result.Rule);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(BimRuleLoadDiagnosticCodes.InvalidFormat, diagnostic.Code);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_returns_deserialization_failure_for_invalid_rule_shape()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"invalid-shape-{Guid.NewGuid():N}.bimrule");
        File.WriteAllText(filePath, """{"metadata":{},"engines":[],"execution":{},"report":{}}""");

        try
        {
            var result = _loader.Load(filePath);

            Assert.False(result.Success);
            Assert.Null(result.Rule);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(BimRuleLoadDiagnosticCodes.DeserializationFailure, diagnostic.Code);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_diagnostics_are_data_only_records()
    {
        var result = _loader.Load(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.bimrule"));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.IsType<BimRuleLoadDiagnostic>(diagnostic);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Code));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }

    private static string CreateTempRuleFile(BimRule rule)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid():N}.bimrule");
        var json = System.Text.Json.JsonSerializer.Serialize(rule, BimRuleTestData.JsonOptions);
        File.WriteAllText(filePath, json);
        return filePath;
    }
}

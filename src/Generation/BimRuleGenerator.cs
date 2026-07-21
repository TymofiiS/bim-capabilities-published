using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Generation.Parsing;

namespace BIMCapabilities.Generation;

public sealed class BimRuleGenerator : IBimRuleGenerator
{
    public BimRuleGenerationResult Generate(BimRuleGenerationRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var diagnostics = new List<BimRuleGenerationDiagnostic>();

        if (string.IsNullOrWhiteSpace(request.NaturalLanguagePrompt))
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = "generation.prompt.empty",
                Message = "A natural language prompt is required.",
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = nameof(BimRuleGenerator)
            });

            return new BimRuleGenerationResult
            {
                Success = false,
                Diagnostics = diagnostics
            };
        }

        var specification = NaturalLanguageRuleParser.Parse(request.NaturalLanguagePrompt);
        var rule = BimRuleDocumentBuilder.Build(specification, request);
        var report = BimRuleGenerationReportBuilder.Build(specification);
        var serializedRule = BimRuleDocumentWriter.Serialize(rule);
        var outputFileName = $"{specification.RuleId}.bimrule";

        string? outputFilePath = null;
        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            outputFilePath = Path.Combine(request.OutputDirectory, outputFileName);
            BimRuleDocumentWriter.Write(rule, outputFilePath);

            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = "generation.file.written",
                Message = $"Generated rule written to {outputFilePath}.",
                Severity = BimRuleGenerationDiagnosticSeverity.Information,
                Source = nameof(BimRuleGenerator)
            });
        }

        var (validationSucceeded, validationDiagnostics) = BimRuleGenerationValidation.Validate(rule);
        diagnostics.AddRange(validationDiagnostics);

        if (!validationSucceeded)
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = "generation.validation.failed",
                Message = "Generated BIMRule failed one or more validators.",
                Severity = BimRuleGenerationDiagnosticSeverity.Error,
                Source = nameof(BimRuleGenerator)
            });
        }
        else
        {
            diagnostics.Add(new BimRuleGenerationDiagnostic
            {
                Code = "generation.validation.passed",
                Message = "Generated BIMRule passed loader-compatible validation.",
                Severity = BimRuleGenerationDiagnosticSeverity.Information,
                Source = nameof(BimRuleGenerator)
            });
        }

        return new BimRuleGenerationResult
        {
            Success = validationSucceeded,
            Rule = rule,
            OutputFileName = outputFileName,
            OutputFilePath = outputFilePath,
            SerializedRule = serializedRule,
            Report = report,
            ValidationSucceeded = validationSucceeded,
            Diagnostics = diagnostics
        };
    }
}

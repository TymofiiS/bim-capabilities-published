using System.Text.RegularExpressions;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Naming;
using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Engines.Naming.Atoms.Pattern;

internal static class NamingPatternValidationAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T11:30:00+00:00";

    internal sealed record ValidationObject(
        string Id,
        string Kind,
        string? Name);

    internal static PatternContracts.NamingPatternValidationResult CreateResult(
        string atomId,
        PatternContracts.NamingPatternValidationRequest request,
        IReadOnlyList<PatternContracts.NamingPatternValidationFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.ObjectKind, StringComparer.Ordinal)
            .ThenBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ToArray();

        return new PatternContracts.NamingPatternValidationResult
        {
            AtomId = atomId,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<PatternContracts.NamingPatternValidationFinding> AnalyzePatterns(
        PatternContracts.NamingPatternValidationRequest request)
    {
        var objects = BuildValidationObjects(request);
        var findings = new List<PatternContracts.NamingPatternValidationFinding>();

        foreach (var validationObject in objects)
        {
            var evaluation = EvaluatePattern(validationObject.Name, request.Rule);

            findings.Add(new PatternContracts.NamingPatternValidationFinding
            {
                ObjectId = validationObject.Id,
                ObjectKind = validationObject.Kind,
                ObjectName = validationObject.Name,
                Status = evaluation.Status,
                Passed = evaluation.Status == PatternContracts.NamingPatternValidationStatus.Valid,
                ViolationReason = evaluation.ViolationReason,
                Rule = request.Rule
            });
        }

        return findings;
    }

    internal static (PatternContracts.NamingPatternValidationStatus Status, string? ViolationReason) EvaluatePattern(
        string? name,
        PatternContracts.NamingPatternRule rule)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (PatternContracts.NamingPatternValidationStatus.EmptyName, "Object name is missing or empty.");
        }

        var trimmedName = name.Trim();

        if (rule.MinimumLength is int minimumLength && trimmedName.Length < minimumLength)
        {
            return (PatternContracts.NamingPatternValidationStatus.LengthViolation,
                $"Name length is less than the minimum length of {minimumLength}.");
        }

        if (rule.MaximumLength is int maximumLength && trimmedName.Length > maximumLength)
        {
            return (PatternContracts.NamingPatternValidationStatus.LengthViolation,
                $"Name length exceeds the maximum length of {maximumLength}.");
        }

        if (rule.ForbiddenCharacters is { Count: > 0 })
        {
            foreach (var forbiddenCharacter in rule.ForbiddenCharacters)
            {
                if (string.IsNullOrEmpty(forbiddenCharacter))
                {
                    continue;
                }

                if (trimmedName.Contains(forbiddenCharacter, StringComparison.Ordinal))
                {
                    return (PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter,
                        $"Name contains forbidden character '{forbiddenCharacter}'.");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.AllowedCharacters)
            && !MatchesAllowedCharacters(trimmedName, rule.AllowedCharacters))
        {
            return (PatternContracts.NamingPatternValidationStatus.InvalidCharacter,
                "Name contains characters outside the allowed character set.");
        }

        if (rule.CaseRule is NamingCaseRule caseRule
            && caseRule != NamingCaseRule.Unspecified
            && !MatchesCaseRule(trimmedName, caseRule))
        {
            return (PatternContracts.NamingPatternValidationStatus.PatternViolation,
                $"Name does not satisfy the required case rule '{caseRule}'.");
        }

        if (!string.IsNullOrWhiteSpace(rule.RegularExpression)
            && !MatchesRegularExpression(trimmedName, rule.RegularExpression))
        {
            return (PatternContracts.NamingPatternValidationStatus.PatternViolation,
                "Name does not match the required regular expression.");
        }

        if (!string.IsNullOrWhiteSpace(rule.TokenizedPattern)
            && !MatchesTokenizedPattern(trimmedName, rule.TokenizedPattern, rule.AllowNumericTokenStart))
        {
            return (PatternContracts.NamingPatternValidationStatus.PatternViolation,
                "Name does not match the required tokenized naming pattern.");
        }

        return (PatternContracts.NamingPatternValidationStatus.Valid, null);
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        PatternContracts.NamingPatternValidationRequest request,
        string atomId,
        IReadOnlyList<PatternContracts.NamingPatternValidationFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();

        foreach (var finding in findings.Where(candidate => !candidate.Passed))
        {
            evidence.Add(new EvidenceRecord
            {
                EvidenceId = BuildEvidenceId(finding.ObjectId, finding.Status),
                Timestamp = executedAt,
                Source = new EvidenceSource
                {
                    EngineId = "naming-engine",
                    AtomId = atomId,
                    RuleId = request.RuleId,
                    CapabilityId = atomId
                },
                Target = new EvidenceTarget
                {
                    TargetType = finding.ObjectKind ?? "object",
                    TargetId = finding.ObjectId,
                    TargetName = finding.ObjectName,
                    TargetSetDescription = request.TargetSet.TargetSetId
                },
                Category = EvidenceCategory.Validation,
                Severity = EvidenceSeverity.Error,
                Message = BuildEvidenceMessage(finding),
                StructuredData = BuildStructuredData(finding)
            });
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildValidationObjects(
        PatternContracts.NamingPatternValidationRequest request)
    {
        var targetSet = request.TargetSet;
        var objects = new List<ValidationObject>();

        if (targetSet.TargetFamilies is { Count: > 0 })
        {
            objects.AddRange(targetSet.TargetFamilies
                .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
                .Select(family => new ValidationObject(
                    family.Identity.Id,
                    "family",
                    family.Name)));
        }

        if (targetSet.TargetTypes is { Count: > 0 })
        {
            objects.AddRange(targetSet.TargetTypes
                .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
                .Select(familyType => new ValidationObject(
                    familyType.Identity.Id,
                    "familyType",
                    familyType.Name)));
        }

        if (objects.Count == 0)
        {
            objects.Add(new ValidationObject(
                targetSet.TargetSetId,
                "targetSet",
                targetSet.TargetSetId));
        }

        return objects;
    }

    internal static bool MatchesTokenizedPattern(
        string name,
        string tokenizedPattern,
        bool allowNumericTokenStart)
    {
        var regexPattern = ConvertTokenizedPatternToRegex(tokenizedPattern, allowNumericTokenStart);
        return MatchesRegularExpression(name, regexPattern);
    }

    internal static string ConvertTokenizedPatternToRegex(
        string tokenizedPattern,
        bool allowNumericTokenStart)
    {
        var tokenPattern = allowNumericTokenStart ? "[A-Za-z0-9]+" : "[A-Za-z][A-Za-z0-9]*";
        var builder = new System.Text.StringBuilder("^");

        for (var index = 0; index < tokenizedPattern.Length; index++)
        {
            if (tokenizedPattern[index] == '{')
            {
                var endIndex = tokenizedPattern.IndexOf('}', index + 1);
                if (endIndex < 0)
                {
                    throw new ArgumentException("Tokenized pattern contains an unclosed token.", nameof(tokenizedPattern));
                }

                builder.Append(tokenPattern);
                index = endIndex;
                continue;
            }

            builder.Append(Regex.Escape(tokenizedPattern[index].ToString()));
        }

        builder.Append('$');
        return builder.ToString();
    }

    private static bool MatchesRegularExpression(string name, string pattern)
    {
        return Regex.IsMatch(
            name,
            pattern,
            RegexValidationOptions.Default,
            TimeSpan.FromSeconds(1));
    }

    private static bool MatchesAllowedCharacters(string name, string allowedCharacters)
    {
        return Regex.IsMatch(
            name,
            $"^[{allowedCharacters}]+$",
            RegexValidationOptions.Default,
            TimeSpan.FromSeconds(1));
    }

    private static bool MatchesCaseRule(string name, NamingCaseRule caseRule)
    {
        return caseRule switch
        {
            NamingCaseRule.UpperCase => string.Equals(name, name.ToUpperInvariant(), StringComparison.Ordinal),
            NamingCaseRule.LowerCase => string.Equals(name, name.ToLowerInvariant(), StringComparison.Ordinal),
            NamingCaseRule.PascalCase => name.Split('_').All(segment =>
                segment.Length > 0 && char.IsUpper(segment[0]) && segment[1..].All(character => !char.IsUpper(character) || char.IsDigit(character))),
            NamingCaseRule.CamelCase => MatchesCamelCase(name),
            NamingCaseRule.TitleCase => name.Split('_').All(segment =>
                segment.Length > 0 && char.IsUpper(segment[0])),
            _ => true
        };
    }

    private static bool MatchesCamelCase(string name)
    {
        var segments = name.Split('_');
        if (segments.Length == 0 || segments[0].Length == 0)
        {
            return false;
        }

        if (!char.IsLower(segments[0][0]))
        {
            return false;
        }

        return segments.Skip(1).All(segment =>
            segment.Length > 0 && char.IsUpper(segment[0]));
    }

    private static PatternContracts.NamingPatternValidationStatistics BuildStatistics(
        IReadOnlyList<PatternContracts.NamingPatternValidationFinding> findings)
    {
        var objectsChecked = findings.Count;
        var objectsFailed = findings.Count(finding => !finding.Passed);

        return new PatternContracts.NamingPatternValidationStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsChecked - objectsFailed,
            ObjectsFailed = objectsFailed,
            PatternViolations = findings.Count(finding =>
                finding.Status is PatternContracts.NamingPatternValidationStatus.PatternViolation
                    or PatternContracts.NamingPatternValidationStatus.EmptyName),
            InvalidCharacterViolations = findings.Count(finding =>
                finding.Status is PatternContracts.NamingPatternValidationStatus.InvalidCharacter
                    or PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter),
            LengthViolations = findings.Count(finding =>
                finding.Status == PatternContracts.NamingPatternValidationStatus.LengthViolation)
        };
    }

    private static IReadOnlyList<NamingEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<PatternContracts.NamingPatternValidationFinding> findings)
    {
        var patternViolations = findings.Count(finding =>
            finding.Status is PatternContracts.NamingPatternValidationStatus.PatternViolation
                or PatternContracts.NamingPatternValidationStatus.EmptyName);
        var invalidCharacterViolations = findings.Count(finding =>
            finding.Status is PatternContracts.NamingPatternValidationStatus.InvalidCharacter
                or PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter);
        var lengthViolations = findings.Count(finding =>
            finding.Status == PatternContracts.NamingPatternValidationStatus.LengthViolation);

        return
        [
            new NamingEngineDiagnostic
            {
                Code = "NamingPatternValidation.Completed",
                Message = $"Naming pattern atom '{atomId}' checked {findings.Count} objects, found {patternViolations} pattern violations, {invalidCharacterViolations} character violations, and {lengthViolations} length violations.",
                Severity = NamingEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        PatternContracts.NamingPatternValidationRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationOperation"] = "naming-pattern-validation",
            ["targetSetId"] = request.TargetSet.TargetSetId
        };

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            metadata["ruleId"] = request.RuleId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["correlationId"] = request.CorrelationId;
        }

        if (request.Metadata is not null)
        {
            foreach (var pair in request.Metadata.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static Dictionary<string, string> BuildStructuredData(
        PatternContracts.NamingPatternValidationFinding finding)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["objectId"] = finding.ObjectId,
            ["objectKind"] = finding.ObjectKind ?? "object",
            ["objectName"] = finding.ObjectName ?? string.Empty,
            ["validationStatus"] = finding.Status.ToString(),
            ["violationReason"] = finding.ViolationReason ?? string.Empty
        };
    }

    private static string BuildEvidenceMessage(PatternContracts.NamingPatternValidationFinding finding)
    {
        return finding.Status switch
        {
            PatternContracts.NamingPatternValidationStatus.EmptyName =>
                $"Object '{finding.ObjectId}' has an empty name.",
            PatternContracts.NamingPatternValidationStatus.LengthViolation =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' violates length constraints.",
            PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' contains forbidden characters.",
            PatternContracts.NamingPatternValidationStatus.InvalidCharacter =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' contains invalid characters.",
            PatternContracts.NamingPatternValidationStatus.PatternViolation =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' does not match the required naming pattern.",
            _ => $"Naming pattern validation failed for '{finding.ObjectName ?? finding.ObjectId}'."
        };
    }

    private static string BuildEvidenceId(
        string objectId,
        PatternContracts.NamingPatternValidationStatus status)
    {
        return $"naming-pattern-{status}-{objectId}".ToLowerInvariant();
    }
}

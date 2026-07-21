using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

internal static class RevitAdapterReadSupport
{
    internal const string AdapterId = "revit-adapter-read-composition";

    internal static RevitAdapterReadResult ExecuteRead(
        IRevitReadAdapter readLayer,
        RevitAdapterReadContext context,
        DateTimeOffset executedAt)
    {
        ArgumentGuard.ThrowIfNull(readLayer);
        ArgumentGuard.ThrowIfNull(context);

        var diagnostics = new List<RevitAdapterReadDiagnostic>();
        FamilyQueryResult? families = null;
        ParameterQueryResult? parameters = null;
        RelationshipQueryResult? relationships = null;
        List<ObjectTranslationResult>? translations = null;

        if (context.FamilyQuery is not null)
        {
            families = readLayer.Families.Retrieve(ApplyCorrelationId(context.FamilyQuery, context.CorrelationId));
            CollectFamilyDiagnostics(diagnostics, families);
        }

        if (context.ParameterQuery is not null)
        {
            parameters = readLayer.Parameters.Retrieve(ApplyCorrelationId(context.ParameterQuery, context.CorrelationId));
            CollectParameterDiagnostics(diagnostics, parameters);
        }

        if (context.RelationshipQuery is not null)
        {
            relationships = readLayer.Relationships.Retrieve(
                ApplyCorrelationId(context.RelationshipQuery, context.CorrelationId));
            CollectRelationshipDiagnostics(diagnostics, relationships);
        }

        if (context.TranslationQueries is { Count: > 0 })
        {
            translations = context.TranslationQueries
                .Select(query => readLayer.Translator.Translate(ApplyCorrelationId(query, context.CorrelationId)))
                .ToList();
        }

        return new RevitAdapterReadResult
        {
            Families = families,
            Parameters = parameters,
            Relationships = relationships,
            Translations = translations,
            Statistics = CreateStatistics(families, parameters, relationships, translations),
            Metadata = CreateMetadata(context, executedAt),
            Diagnostics = diagnostics.Count > 0
                ? diagnostics
                    .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .ToList()
                : null
        };
    }

    internal static RevitAdapter CreateOperationalReadLayer(
        IRevitFamilyCatalog familyCatalog,
        IRevitParameterCatalog parameterCatalog,
        IRevitRelationshipCatalog relationshipCatalog,
        IRevitObjectResolver objectResolver,
        IFamilyQueryClock clock)
    {
        return new RevitAdapter(
            new RevitFamilyProvider(familyCatalog, clock),
            new RevitParameterProvider(parameterCatalog, clock),
            new RevitRelationshipProvider(relationshipCatalog, clock),
            new RevitObjectTranslator(objectResolver),
            clock);
    }

    private static FamilyQuery ApplyCorrelationId(FamilyQuery query, string? correlationId)
    {
        return correlationId is null || query.CorrelationId is not null
            ? query
            : query with { CorrelationId = correlationId };
    }

    private static ParameterQuery ApplyCorrelationId(ParameterQuery query, string? correlationId)
    {
        return correlationId is null || query.CorrelationId is not null
            ? query
            : query with { CorrelationId = correlationId };
    }

    private static RelationshipQuery ApplyCorrelationId(RelationshipQuery query, string? correlationId)
    {
        return correlationId is null || query.CorrelationId is not null
            ? query
            : query with { CorrelationId = correlationId };
    }

    private static ObjectTranslationQuery ApplyCorrelationId(ObjectTranslationQuery query, string? correlationId)
    {
        return correlationId is null || query.CorrelationId is not null
            ? query
            : query with { CorrelationId = correlationId };
    }

    private static void CollectFamilyDiagnostics(
        ICollection<RevitAdapterReadDiagnostic> diagnostics,
        FamilyQueryResult result)
    {
        if (result.Diagnostics is null)
        {
            return;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            diagnostics.Add(new RevitAdapterReadDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Source = "provider:family",
                Data = diagnostic.Data
            });
        }
    }

    private static void CollectParameterDiagnostics(
        ICollection<RevitAdapterReadDiagnostic> diagnostics,
        ParameterQueryResult result)
    {
        if (result.Diagnostics is null)
        {
            return;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            diagnostics.Add(new RevitAdapterReadDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Source = "provider:parameter",
                Data = diagnostic.Data
            });
        }
    }

    private static void CollectRelationshipDiagnostics(
        ICollection<RevitAdapterReadDiagnostic> diagnostics,
        RelationshipQueryResult result)
    {
        if (result.Diagnostics is null)
        {
            return;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            diagnostics.Add(new RevitAdapterReadDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Source = "provider:relationship",
                Data = diagnostic.Data
            });
        }
    }

    private static RevitAdapterReadDiagnosticSeverity MapSeverity(FamilyQueryDiagnosticSeverity severity) =>
        severity switch
        {
            FamilyQueryDiagnosticSeverity.Warning => RevitAdapterReadDiagnosticSeverity.Warning,
            FamilyQueryDiagnosticSeverity.Error => RevitAdapterReadDiagnosticSeverity.Error,
            _ => RevitAdapterReadDiagnosticSeverity.Information
        };

    private static RevitAdapterReadDiagnosticSeverity MapSeverity(ParameterQueryDiagnosticSeverity severity) =>
        severity switch
        {
            ParameterQueryDiagnosticSeverity.Warning => RevitAdapterReadDiagnosticSeverity.Warning,
            ParameterQueryDiagnosticSeverity.Error => RevitAdapterReadDiagnosticSeverity.Error,
            _ => RevitAdapterReadDiagnosticSeverity.Information
        };

    private static RevitAdapterReadDiagnosticSeverity MapSeverity(RelationshipQueryDiagnosticSeverity severity) =>
        severity switch
        {
            RelationshipQueryDiagnosticSeverity.Warning => RevitAdapterReadDiagnosticSeverity.Warning,
            RelationshipQueryDiagnosticSeverity.Error => RevitAdapterReadDiagnosticSeverity.Error,
            _ => RevitAdapterReadDiagnosticSeverity.Information
        };

    private static RevitAdapterStatistics CreateStatistics(
        FamilyQueryResult? families,
        ParameterQueryResult? parameters,
        RelationshipQueryResult? relationships,
        IReadOnlyList<ObjectTranslationResult>? translations)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (families is not null)
        {
            properties["includesFamilyQuery"] = "true";
        }

        if (parameters is not null)
        {
            properties["includesParameterQuery"] = "true";
        }

        if (relationships is not null)
        {
            properties["includesRelationshipQuery"] = "true";
        }

        if (translations is not null)
        {
            properties["translationQueryCount"] = translations.Count.ToString();
        }

        if (families?.Statistics is not null)
        {
            properties["totalFamilies"] = families.Statistics.TotalFamilies.ToString();
            properties["filteredFamilies"] = families.Statistics.FilteredFamilies.ToString();
        }

        if (parameters?.Statistics is not null)
        {
            properties["missingParameters"] = parameters.Statistics.MissingParameters.ToString();
        }

        if (relationships?.QueryMetadata?.Properties is not null)
        {
            foreach (var entry in relationships.QueryMetadata.Properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                properties[$"relationship.{entry.Key}"] = entry.Value;
            }
        }

        return new RevitAdapterStatistics
        {
            FamiliesRetrieved = families?.Families.Count ?? 0,
            ParametersRetrieved = parameters?.Parameters.Count ?? 0,
            RelationshipsRetrieved = relationships?.Relationships.Count ?? 0,
            TranslationsRetrieved = translations?.Count ?? 0,
            Properties = properties.Count > 0 ? properties : null
        };
    }

    private static RevitAdapterReadMetadata CreateMetadata(
        RevitAdapterReadContext context,
        DateTimeOffset executedAt)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (context.Metadata is not null)
        {
            foreach (var entry in context.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                properties[entry.Key] = entry.Value;
            }
        }

        if (context.FamilyQuery is not null)
        {
            properties["includesFamilyQuery"] = "true";
        }

        if (context.ParameterQuery is not null)
        {
            properties["includesParameterQuery"] = "true";
        }

        if (context.RelationshipQuery is not null)
        {
            properties["includesRelationshipQuery"] = "true";
        }

        if (context.TranslationQueries is { Count: > 0 })
        {
            properties["translationQueryCount"] = context.TranslationQueries.Count.ToString();
        }

        return new RevitAdapterReadMetadata
        {
            CorrelationId = context.CorrelationId,
            ExecutedAt = executedAt,
            AdapterId = AdapterId,
            Properties = properties.Count > 0 ? properties : null
        };
    }
}

using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Retrieves Revit parameters and translates them into normalized BIMCapabilities contracts.
/// </summary>
public sealed class RevitParameterProvider : IParameterProvider
{
    private readonly IRevitParameterCatalog _catalog;
    private readonly IFamilyQueryClock _clock;

    public RevitParameterProvider(IRevitParameterCatalog catalog)
        : this(catalog, new SystemFamilyQueryClock())
    {
    }

    internal RevitParameterProvider(IRevitParameterCatalog catalog, IFamilyQueryClock clock)
    {
        ArgumentGuard.ThrowIfNull(catalog);
        ArgumentGuard.ThrowIfNull(clock);

        _catalog = catalog;
        _clock = clock;
    }

    public ParameterQueryResult Retrieve(ParameterQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        var diagnostics = new List<ParameterQueryDiagnostic>();

        if (ParameterRetrievalSupport.IsInvalidQuery(query))
        {
            diagnostics.Add(new ParameterQueryDiagnostic
            {
                Code = ParameterRetrievalDiagnostics.InvalidQuery,
                Message = "The parameter query scope requires one or more scope identifiers.",
                Severity = ParameterQueryDiagnosticSeverity.Error,
                Location = "query:scope",
                Data = query.Scope is null
                    ? null
                    : new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["scopeKind"] = query.Scope.Kind.ToString()
                    }
            });

            return CreateResult([], diagnostics, query, totalCandidates: 0, missingParameters: 0);
        }

        if (query.Filter is not null)
        {
            diagnostics.Add(new ParameterQueryDiagnostic
            {
                Code = ParameterRetrievalDiagnostics.UnsupportedFilter,
                Message = "Parameter query filter criteria are not evaluated during retrieval.",
                Severity = ParameterQueryDiagnosticSeverity.Information,
                Location = "query:filter"
            });
        }

        if (ParameterRetrievalSupport.TryGetInvalidCategories(query, out var invalidCategories))
        {
            diagnostics.Add(new ParameterQueryDiagnostic
            {
                Code = ParameterRetrievalDiagnostics.InvalidQuery,
                Message = "One or more requested categories are not supported by the Revit Adapter.",
                Severity = ParameterQueryDiagnosticSeverity.Error,
                Location = "query:categories",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["invalidCategories"] = string.Join(",", invalidCategories)
                }
            });

            return CreateResult([], diagnostics, query, totalCandidates: 0, missingParameters: 0);
        }

        var catalogParameters = _catalog.GetParameters();
        var selectedEntries = ParameterRetrievalSupport.SelectParameters(catalogParameters, query).ToList();
        var translation = ParameterRetrievalSupport.TranslateParameters(selectedEntries);

        diagnostics.AddRange(translation.Diagnostics);

        if (translation.Parameters.Count == 0)
        {
            ParameterRetrievalSupport.AddEmptyResultDiagnostic(diagnostics, catalogParameters.Count);
        }

        ParameterRetrievalSupport.AddMissingParameterDiagnostics(diagnostics, query, translation.Parameters);

        var missingParameters = ParameterRetrievalSupport.CountMissingParameters(query, translation.Parameters);

        return CreateResult(
            translation.Parameters,
            diagnostics,
            query,
            selectedEntries.Count,
            missingParameters);
    }

    private ParameterQueryResult CreateResult(
        IReadOnlyList<NormalizedParameter> parameters,
        IReadOnlyList<ParameterQueryDiagnostic> diagnostics,
        ParameterQuery query,
        int totalCandidates,
        int missingParameters)
    {
        var sharedParametersRetrieved = parameters.Count(parameter => parameter.IsSharedParameter);

        return new ParameterQueryResult
        {
            Parameters = parameters,
            Diagnostics = diagnostics.Count > 0
                ? diagnostics
                    .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .ToList()
                : null,
            Statistics = ParameterRetrievalSupport.CreateStatistics(
                totalCandidates,
                parameters,
                missingParameters),
            QueryMetadata = ParameterRetrievalSupport.CreateMetadata(
                query,
                _clock.UtcNow,
                _catalog.ObjectsInspected,
                sharedParametersRetrieved)
        };
    }
}

using BIMCapabilities.Composition.Interpretation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Composition.Capabilities;

internal sealed class BimRuleExecutionPlanBuilder : IBimRuleExecutionPlanBuilder
{
    private readonly string _ruleId;
    private readonly Dictionary<string, CategoryExecutionSpecificationBuilder> _categories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _interpretationMarkers = [];

    internal BimRuleExecutionPlanBuilder(string ruleId)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(ruleId);
        _ruleId = ruleId;
    }

    public bool RunParameterCompliance { get; private set; }

    public bool RunNamingCompliance { get; private set; }

    public bool RunImportedCadExclusion { get; private set; }

    public bool GenerateReport { get; private set; }

    public IReadOnlyList<string> InterpretationMarkers => _interpretationMarkers;

    public void EnableParameterCompliance() => RunParameterCompliance = true;

    public void EnableNamingCompliance() => RunNamingCompliance = true;

    public void EnableImportedCadExclusion() => RunImportedCadExclusion = true;

    public void EnableReportGeneration() => GenerateReport = true;

    public void AddCategory(string categoryName)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(categoryName);
        _ = GetOrCreateCategory(categoryName);
    }

    public void SetRequiredParameters(string categoryName, IReadOnlyList<string> parameters)
    {
        var category = GetOrCreateCategory(categoryName);
        category.RequiredParameters = parameters;
    }

    public void SetParameterDefaults(string categoryName, IReadOnlyDictionary<string, string> parameterDefaults)
    {
        var category = GetOrCreateCategory(categoryName);
        category.ParameterDefaults = parameterDefaults;
    }

    public void SetParameterBindings(string categoryName, IReadOnlyDictionary<string, bool> parameterBindings)
    {
        var category = GetOrCreateCategory(categoryName);
        category.ParameterBindings = parameterBindings;
    }

    public void SetParameterFillRules(string categoryName, IReadOnlyDictionary<string, string> parameterFillRules)
    {
        var category = GetOrCreateCategory(categoryName);
        category.ParameterFillRules = parameterFillRules;
    }

    public void SetPrefixFixScope(string categoryName, PrefixFixScope prefixFixScope)
    {
        var category = GetOrCreateCategory(categoryName);
        category.PrefixFixScope = prefixFixScope;
    }

    public void SetRequiredPrefix(string categoryName, string prefix)
    {
        var category = GetOrCreateCategory(categoryName);
        category.RequiredPrefix = prefix;
    }

    public void SetExcludeImportedCad(string categoryName, bool exclude)
    {
        var category = GetOrCreateCategory(categoryName);
        category.ExcludeImportedCad = exclude;
    }

    public void AddInterpretationMarker(string marker)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(marker);
        _interpretationMarkers.Add(marker);
    }

    internal BimRuleExecutionPlan Build()
    {
        if (_categories.Count == 0)
        {
            throw new InvalidOperationException(
                "BIMRule does not declare any categories in capability configuration.");
        }

        var categories = _categories.Values
            .OrderBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .Select(category => new CategoryExecutionSpecification
            {
                CategoryName = category.CategoryName,
                RequiredParameters = category.RequiredParameters,
                ParameterDefaults = category.ParameterDefaults,
                ParameterFillRules = category.ParameterFillRules,
                ParameterBindings = category.ParameterBindings,
                RequiredPrefix = category.RequiredPrefix,
                PrefixFixScope = category.PrefixFixScope,
                ExcludeImportedCad = category.ExcludeImportedCad
            })
            .ToArray();

        return new BimRuleExecutionPlan
        {
            RuleId = _ruleId,
            Categories = categories,
            RunParameterCompliance = RunParameterCompliance,
            RunNamingCompliance = RunNamingCompliance,
            RunImportedCadExclusion = RunImportedCadExclusion,
            GenerateReport = GenerateReport,
            InterpretationMarkers = _interpretationMarkers
        };
    }

    private CategoryExecutionSpecificationBuilder GetOrCreateCategory(string categoryName)
    {
        if (!_categories.TryGetValue(categoryName, out var category))
        {
            category = new CategoryExecutionSpecificationBuilder { CategoryName = categoryName };
            _categories[categoryName] = category;
        }

        return category;
    }

    private sealed class CategoryExecutionSpecificationBuilder
    {
        public required string CategoryName { get; init; }

        public IReadOnlyList<string> RequiredParameters { get; set; } = [];

        public IReadOnlyDictionary<string, string> ParameterDefaults { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> ParameterFillRules { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, bool> ParameterBindings { get; set; }
            = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public string? RequiredPrefix { get; set; }

        public PrefixFixScope PrefixFixScope { get; set; }

        public bool ExcludeImportedCad { get; set; }
    }
}

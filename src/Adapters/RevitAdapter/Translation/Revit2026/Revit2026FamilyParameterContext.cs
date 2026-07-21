namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal sealed record Revit2026FamilyParameterContext
{
    public static Revit2026FamilyParameterContext Empty { get; } = new([], new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase));

    public Revit2026FamilyParameterContext(
        IReadOnlyList<string> definitionNames,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> valuesByTypeName)
    {
        DefinitionNames = definitionNames;
        ValuesByTypeName = valuesByTypeName;
    }

    public IReadOnlyList<string> DefinitionNames { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ValuesByTypeName { get; }

    public IReadOnlyDictionary<string, string> GetValuesForType(string typeName)
    {
        return ValuesByTypeName.TryGetValue(typeName, out var values)
            ? values
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}

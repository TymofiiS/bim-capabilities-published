using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests.Mocks;

internal sealed class MockRevitCategoryHandle : IRevitCategoryHandle
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

internal sealed class MockRevitParameterHandle : IRevitParameterHandle
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Value { get; init; }

    public required string StorageType { get; init; }

    public bool IsSharedParameter { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

internal sealed class MockRevitFamilyTypeHandle : IRevitFamilyTypeHandle
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyList<IRevitParameterHandle> Parameters { get; init; } = [];
}

internal sealed class MockRevitFamilyHandle : IRevitFamilyHandle
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IRevitCategoryHandle? Category { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyList<IRevitFamilyTypeHandle> FamilyTypes { get; init; } = [];

    public IReadOnlyList<IRevitParameterHandle> Parameters { get; init; } = [];

    public IReadOnlyList<IRevitRelationshipHandle> Relationships { get; init; } = [];
}

internal sealed class MockRevitRelationshipHandle : IRevitRelationshipHandle
{
    public required string SourceId { get; init; }

    public required string SourceKind { get; init; }

    public required string TargetId { get; init; }

    public required string TargetKind { get; init; }

    public required NormalizedRelationshipType RelationshipType { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

internal sealed class MockRevitObjectResolver : IRevitObjectResolver
{
    private readonly Dictionary<string, IRevitFamilyHandle> _families = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IRevitFamilyTypeHandle> _familyTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IRevitCategoryHandle> _categories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IRevitParameterHandle> _parameters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IRevitRelationshipHandle> _relationships = new(StringComparer.Ordinal);

    internal void RegisterFamily(string id, IRevitFamilyHandle handle) => _families[id] = handle;

    internal void RegisterFamilyType(string id, IRevitFamilyTypeHandle handle) => _familyTypes[id] = handle;

    internal void RegisterCategory(string id, IRevitCategoryHandle handle) => _categories[id] = handle;

    internal void RegisterParameter(string id, IRevitParameterHandle handle) => _parameters[id] = handle;

    internal void RegisterRelationship(string id, IRevitRelationshipHandle handle) => _relationships[id] = handle;

    public IRevitFamilyHandle? ResolveFamily(string sourceObjectId) =>
        _families.TryGetValue(sourceObjectId, out var handle) ? handle : null;

    public IRevitFamilyTypeHandle? ResolveFamilyType(string sourceObjectId) =>
        _familyTypes.TryGetValue(sourceObjectId, out var handle) ? handle : null;

    public IRevitCategoryHandle? ResolveCategory(string sourceObjectId) =>
        _categories.TryGetValue(sourceObjectId, out var handle) ? handle : null;

    public IRevitParameterHandle? ResolveParameter(string sourceObjectId) =>
        _parameters.TryGetValue(sourceObjectId, out var handle) ? handle : null;

    public IRevitElementHandle? ResolveElement(string sourceObjectId) => null;

    public IRevitRelationshipHandle? ResolveRelationship(string sourceObjectId) =>
        _relationships.TryGetValue(sourceObjectId, out var handle) ? handle : null;
}

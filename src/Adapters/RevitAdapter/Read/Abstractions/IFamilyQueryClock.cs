namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// Supplies timestamps for family retrieval metadata.
/// </summary>
internal interface IFamilyQueryClock
{
    DateTimeOffset UtcNow { get; }
}

internal sealed class SystemFamilyQueryClock : IFamilyQueryClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

internal sealed class FixedFamilyQueryClock(DateTimeOffset utcNow) : IFamilyQueryClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

using BIMCapabilities.Contracts.Diagnostics;

namespace BIMCapabilities.Runtime.Diagnostics;

/// <summary>
/// In-memory diagnostic collection for runtime composition.
/// </summary>
public sealed class RuntimeDiagnosticsSkeleton : IRuntimeDiagnostics
{
    private readonly List<DiagnosticRecord> _records = [];
    private int _sequence;

    public DiagnosticCollection Collection =>
        new()
        {
            CollectionId = "runtime-diagnostics",
            Records = _records
        };

    public void Add(DiagnosticRecord record)
    {
        ArgumentGuard.ThrowIfNull(record);
        _records.Add(record);
    }

    internal void AddPlaceholder(string correlationId, string message)
    {
        _sequence++;
        Add(new DiagnosticRecord
        {
            DiagnosticId = $"runtime-diagnostic-{_sequence:D3}",
            Timestamp = DateTimeOffset.UtcNow,
            Source = new DiagnosticSource
            {
                ComponentType = "Runtime",
                ComponentId = "runtime-skeleton",
                Operation = "ComposeResult",
                Code = "RuntimeExecutionNotImplemented"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = message,
            CorrelationId = correlationId
        });
    }
}

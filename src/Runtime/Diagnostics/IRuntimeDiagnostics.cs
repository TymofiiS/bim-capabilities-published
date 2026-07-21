using BIMCapabilities.Contracts.Diagnostics;

namespace BIMCapabilities.Runtime.Diagnostics;

/// <summary>
/// Collects runtime diagnostic records during composition.
/// </summary>
public interface IRuntimeDiagnostics
{
    DiagnosticCollection Collection { get; }

    void Add(DiagnosticRecord record);
}

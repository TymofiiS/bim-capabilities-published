namespace BIMCapabilities.Contracts.Diagnostics;

/// <summary>
/// Classifies the origin area of a diagnostic record.
/// </summary>
public enum DiagnosticCategory
{
    Runtime,

    Configuration,

    Validation,

    Execution,

    Adapter,

    Launcher,

    Report,

    System
}

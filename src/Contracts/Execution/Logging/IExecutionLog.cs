namespace BIMCapabilities.Contracts.Execution.Logging;

/// <summary>
/// Optional per-run execution trace written when enabled by the rule.
/// </summary>
public interface IExecutionLog
{
    string LogFilePath { get; }

    void WriteInformation(string category, string message);

    void WriteWarning(string category, string message);

    void WriteError(string category, string message);
}

using System.Text;
using BIMCapabilities.Contracts.Execution.Logging;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Composition.Logging;

/// <summary>
/// Writes a unique execution log file into the run report directory when enabled by the rule.
/// </summary>
public sealed class ExecutionLogWriter : IExecutionLog, IDisposable
{
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    private ExecutionLogWriter(string logFilePath, bool append, string? ruleId, string? correlationId, DateTimeOffset? startedAt)
    {
        LogFilePath = logFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        _writer = new StreamWriter(logFilePath, append, Encoding.UTF8) { AutoFlush = true };

        if (!append)
        {
            WriteLine("INFO", "execution-log", $"Execution log started for rule '{ruleId}'.");
            WriteLine("INFO", "execution-log", $"CorrelationId={correlationId}");
            WriteLine("INFO", "execution-log", $"StartedAt={startedAt:O}");
        }
        else
        {
            WriteLine("INFO", "execution-log", "Execution log resumed.");
        }
    }

    public string LogFilePath { get; }

    public static ExecutionLogWriter? CreateIfEnabled(
        BimRuleReport? report,
        string reportDirectory,
        string ruleId,
        string correlationId,
        DateTimeOffset startedAt)
    {
        if (report?.EnableExecutionLog != true)
        {
            return null;
        }

        ArgumentGuard.ThrowIfNullOrWhiteSpace(reportDirectory);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(correlationId);

        var fileName = $"{SanitizeFileToken(ruleId)}-execution-{SanitizeFileToken(correlationId)}.log";
        var logFilePath = Path.Combine(reportDirectory, fileName);
        return new ExecutionLogWriter(logFilePath, append: false, ruleId, correlationId, startedAt);
    }

    public static ExecutionLogWriter? OpenForAppend(string? logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            return null;
        }

        return new ExecutionLogWriter(logFilePath, append: true, ruleId: null, correlationId: null, startedAt: null);
    }

    public void WriteInformation(string category, string message)
    {
        WriteLine("INFO", category, message);
    }

    public void WriteWarning(string category, string message)
    {
        WriteLine("WARN", category, message);
    }

    public void WriteError(string category, string message)
    {
        WriteLine("ERROR", category, message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            WriteLine("INFO", "execution-log", "Execution log closed.");
            _writer.Dispose();
            _disposed = true;
        }
    }

    private void WriteLine(string level, string category, string message)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _writer.WriteLine($"{DateTimeOffset.UtcNow:O} [{level}] [{category}] {message}");
        }
    }

    private static string SanitizeFileToken(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        return builder.ToString();
    }
}

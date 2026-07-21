using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Integration.Tests;

public class ExecutionLogWriterTests
{
    [Fact]
    public void CreateIfEnabled_returns_null_when_execution_log_disabled()
    {
        var reportDirectory = CreateTempReportDirectory();

        var log = ExecutionLogWriter.CreateIfEnabled(
            new BimRuleReport { EnableExecutionLog = false },
            reportDirectory,
            "RULE-001",
            "corr-test-disabled",
            DateTimeOffset.UtcNow);

        Assert.Null(log);
    }

    [Fact]
    public void CreateIfEnabled_returns_null_when_report_section_missing()
    {
        var reportDirectory = CreateTempReportDirectory();

        var log = ExecutionLogWriter.CreateIfEnabled(
            report: null,
            reportDirectory,
            "RULE-001",
            "corr-test-missing",
            DateTimeOffset.UtcNow);

        Assert.Null(log);
    }

    [Fact]
    public void CreateIfEnabled_writes_unique_log_file_per_correlation_id()
    {
        var reportDirectory = CreateTempReportDirectory();
        var startedAt = new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero);
        var report = new BimRuleReport { EnableExecutionLog = true };

        using (var firstLog = ExecutionLogWriter.CreateIfEnabled(
            report,
            reportDirectory,
            "RULE-001",
            "corr-run-a",
            startedAt))
        {
            Assert.NotNull(firstLog);
            firstLog!.WriteInformation("test", "first run");
        }

        using (var secondLog = ExecutionLogWriter.CreateIfEnabled(
            report,
            reportDirectory,
            "RULE-001",
            "corr-run-b",
            startedAt))
        {
            Assert.NotNull(secondLog);
            secondLog!.WriteInformation("test", "second run");
        }

        var logFiles = Directory.GetFiles(reportDirectory, "RULE-001-execution-*.log");
        Assert.Equal(2, logFiles.Length);
        Assert.Contains(logFiles, path => path.EndsWith("RULE-001-execution-corr-run-a.log", StringComparison.Ordinal));
        Assert.Contains(logFiles, path => path.EndsWith("RULE-001-execution-corr-run-b.log", StringComparison.Ordinal));
    }

    [Fact]
    public void OpenForAppend_appends_to_existing_log_without_overwriting()
    {
        var reportDirectory = CreateTempReportDirectory();
        var startedAt = DateTimeOffset.UtcNow;
        var report = new BimRuleReport { EnableExecutionLog = true };

        string logFilePath;
        using (var initialLog = ExecutionLogWriter.CreateIfEnabled(
            report,
            reportDirectory,
            "RULE-002",
            "corr-append",
            startedAt))
        {
            Assert.NotNull(initialLog);
            logFilePath = initialLog!.LogFilePath;
            initialLog.WriteInformation("validation", "validation complete");
        }

        using (var resumedLog = ExecutionLogWriter.OpenForAppend(logFilePath))
        {
            Assert.NotNull(resumedLog);
            resumedLog!.WriteInformation("fix-pipeline", "fix complete");
        }

        var content = File.ReadAllText(logFilePath);
        Assert.Contains("validation complete", content, StringComparison.Ordinal);
        Assert.Contains("Execution log resumed.", content, StringComparison.Ordinal);
        Assert.Contains("fix complete", content, StringComparison.Ordinal);
    }

    private static string CreateTempReportDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "BIMCapabilities",
            $"execution-log-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}

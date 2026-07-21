using BIMCapabilities.Runtime.Context;
using BIMCapabilities.Runtime.Diagnostics;
using BIMCapabilities.Runtime.Evidence;
using BIMCapabilities.Runtime.Execution;
using BIMCapabilities.Runtime.Registration;

namespace BIMCapabilities.Runtime;

/// <summary>
/// Skeleton runtime that composes contracts without performing execution.
/// </summary>
public sealed class RuntimeSkeleton : IRuntime
{
    public RuntimeSkeleton()
    {
        Context = new RuntimeContextSkeleton();
        Registry = new RuntimeRegistrySkeleton();
        Diagnostics = new RuntimeDiagnosticsSkeleton();
        Evidence = new RuntimeEvidenceSkeleton();
        Execution = new RuntimeExecutionSkeleton(Registry, (RuntimeDiagnosticsSkeleton)Diagnostics, Evidence);
    }

    public IRuntimeContext Context { get; }

    public IRuntimeExecution Execution { get; }

    public IRuntimeRegistry Registry { get; }

    public IRuntimeDiagnostics Diagnostics { get; }

    public IRuntimeEvidence Evidence { get; }
}

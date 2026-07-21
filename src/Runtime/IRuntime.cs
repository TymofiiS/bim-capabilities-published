using BIMCapabilities.Runtime.Context;
using BIMCapabilities.Runtime.Diagnostics;
using BIMCapabilities.Runtime.Evidence;
using BIMCapabilities.Runtime.Execution;
using BIMCapabilities.Runtime.Registration;

namespace BIMCapabilities.Runtime;

/// <summary>
/// Root runtime composition contract for coordinating execution-related services.
/// </summary>
public interface IRuntime
{
    IRuntimeContext Context { get; }

    IRuntimeExecution Execution { get; }

    IRuntimeRegistry Registry { get; }

    IRuntimeDiagnostics Diagnostics { get; }

    IRuntimeEvidence Evidence { get; }
}

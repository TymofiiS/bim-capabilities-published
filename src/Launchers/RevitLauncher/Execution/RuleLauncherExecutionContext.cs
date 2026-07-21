using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Runtime context for executing the Revit rule launcher workflow.
/// </summary>
public sealed record RuleLauncherExecutionContext(UIApplication Application, Document Document);

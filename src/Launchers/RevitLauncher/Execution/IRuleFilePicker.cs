namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Selects a .bimrule file for launcher execution.
/// </summary>
public interface IRuleFilePicker
{
    string? PickRuleFile(string? initialDirectory = null, nint ownerHandle = 0);
}

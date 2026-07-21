using System.Windows.Forms;
using BIMCapabilities.Launchers.Revit.Commands;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Uses the standard Windows file picker to select a .bimrule file.
/// </summary>
public sealed class WinFormsRuleFilePicker : IRuleFilePicker
{
    public string? PickRuleFile(string? initialDirectory = null, nint ownerHandle = 0)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select BIMRule File",
            Filter = "BIMRule files (*.bimrule)|*.bimrule|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = RuleFilePickerSettings.ResolveInitialDirectory(initialDirectory)
        };

        var owner = ownerHandle == 0 ? null : new RevitWindowOwner(ownerHandle);
        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        if (result != DialogResult.OK)
        {
            return null;
        }

        RuleFilePickerSettings.SaveLastFolder(dialog.FileName);
        return dialog.FileName;
    }
}

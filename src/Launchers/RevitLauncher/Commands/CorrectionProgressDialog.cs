using System.Windows.Forms;

namespace BIMCapabilities.Launchers.Revit.Commands;

internal sealed class CorrectionProgressDialog : Form
{
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly IWin32Window? _owner;

    private CorrectionProgressDialog(int totalSteps, string title, string initialMessage, IWin32Window? owner)
    {
        _owner = owner;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        StartPosition = owner is null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
        Width = 480;
        Height = 130;
        TopMost = owner is null;
        ShowInTaskbar = owner is null;

        _statusLabel = new Label
        {
            AutoSize = false,
            Left = 16,
            Top = 16,
            Width = 440,
            Height = 32,
            Text = initialMessage
        };

        _progressBar = new ProgressBar
        {
            Left = 16,
            Top = 56,
            Width = 440,
            Height = 24,
            Minimum = 0,
            Maximum = Math.Max(totalSteps, 1),
            Step = 1
        };

        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
    }

    internal static CorrectionProgressDialog Show(int totalSteps, nint ownerHandle = 0)
    {
        return Show(totalSteps, "Applying Automatic Correction", "Preparing automatic correction...", ownerHandle);
    }

    internal static CorrectionProgressDialog Show(
        int totalSteps,
        string title,
        string initialMessage,
        nint ownerHandle = 0)
    {
        var owner = ownerHandle == 0 ? null : new RevitWindowOwner(ownerHandle);
        var dialog = new CorrectionProgressDialog(totalSteps, title, initialMessage, owner);
        if (owner is null)
        {
            dialog.Show();
        }
        else
        {
            dialog.Show(owner);
        }

        Application.DoEvents();
        return dialog;
    }

    internal void Report(int currentStep, int totalSteps, string message)
    {
        var maximum = Math.Max(Math.Max(totalSteps, currentStep), _progressBar.Maximum);
        _progressBar.Maximum = maximum;
        _progressBar.Value = Math.Min(Math.Max(currentStep, 0), _progressBar.Maximum);
        _statusLabel.Text = message;
        Refresh();
        Application.DoEvents();
    }

    internal void BeforeDocumentModification()
    {
        Hide();
    }

    internal void AfterDocumentModification(int currentStep, int totalSteps, string message)
    {
        Report(currentStep, totalSteps, message);
        if (_owner is null)
        {
            Show();
        }
        else
        {
            Show(_owner);
        }

        Refresh();
        Application.DoEvents();
    }

    internal void CloseDialog()
    {
        Close();
        Dispose();
    }
}

public sealed class CorrectionProgressScope
{
    private readonly CorrectionProgressDialog _dialog;

    internal CorrectionProgressScope(CorrectionProgressDialog dialog)
    {
        _dialog = dialog;
    }

    internal void Report(int currentStep, int totalSteps, string message)
    {
        _dialog.Report(currentStep, totalSteps, message);
    }

    internal void BeforeDocumentModification()
    {
        _dialog.BeforeDocumentModification();
    }

    internal void AfterDocumentModification(int currentStep, int totalSteps, string message)
    {
        _dialog.AfterDocumentModification(currentStep, totalSteps, message);
    }
}

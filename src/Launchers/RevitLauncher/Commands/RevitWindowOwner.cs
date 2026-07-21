using System.Windows.Forms;

namespace BIMCapabilities.Launchers.Revit.Commands;

internal sealed class RevitWindowOwner : IWin32Window
{
    internal RevitWindowOwner(nint handle)
    {
        Handle = handle;
    }

    public nint Handle { get; }
}

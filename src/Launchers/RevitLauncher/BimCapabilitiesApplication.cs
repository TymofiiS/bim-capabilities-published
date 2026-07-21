using System.Reflection;
using Autodesk.Revit.UI;
using BIMCapabilities.Launchers.Revit.Commands;

namespace BIMCapabilities.Launchers.Revit;

/// <summary>
/// Registers the BIMCapabilities ribbon and launcher commands in Revit 2026.
/// </summary>
public sealed class BimCapabilitiesApplication : IExternalApplication
{
    internal const string TabName = "BIMCapabilities";

    internal const string PanelName = "Company Standard";

    internal const string RunBimRuleButtonName = "RunBimRule";

    public Result OnStartup(UIControlledApplication application)
    {
        ArgumentGuard.ThrowIfNull(application);

        application.CreateRibbonTab(TabName);
        var panel = application.CreateRibbonPanel(TabName, PanelName);
        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        var runRuleData = new PushButtonData(
            RunBimRuleButtonName,
            "Run BIM\nRule",
            assemblyPath,
            typeof(RuleExecutionCommand).FullName)
        {
            ToolTip = "Run a BIM rule against the active model.",
            LongDescription =
                "Select a company .bimrule file, validate the model, review the report, " +
                "and apply automatic correction when the rule supports it.",
            LargeImage = RibbonIconFactory.CreateLargeImage(),
            Image = RibbonIconFactory.CreateSmallImage()
        };

        panel.AddItem(runRuleData);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}

using System.Reflection;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Tests;

public class LauncherProjectTests
{
    [Fact]
    public void Revit_launcher_project_is_referenced()
    {
        var launcherAssembly = Assembly.Load("BIMCapabilities.Launchers.Revit");

        Assert.Equal("BIMCapabilities.Launchers.Revit", launcherAssembly.GetName().Name);
    }

    [Fact]
    public void Launcher_execution_service_is_available()
    {
        Assert.NotNull(typeof(LauncherRuleExecutionService));
    }
}

using System.Reflection;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Tests;

public class RuleFilePickerSettingsTests
{
    [Fact]
    public void SaveLastFolder_remembers_directory_for_current_session()
    {
        ClearSessionCache();

        var directory = Path.GetTempPath();
        var selectedFilePath = Path.Combine(directory, "Company-Doors-Windows-Room.bimrule");

        RuleFilePickerSettings.SaveLastFolder(selectedFilePath);

        var resolved = RuleFilePickerSettings.ResolveInitialDirectory(null);

        Assert.Equal(
            NormalizeDirectory(directory),
            NormalizeDirectory(resolved));
    }

    [Fact]
    public void ResolveInitialDirectory_uses_fallback_when_nothing_saved()
    {
        ClearSessionCache();

        var fallbackDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var resolved = RuleFilePickerSettings.ResolveInitialDirectory(fallbackDirectory);

        Assert.Equal(fallbackDirectory, resolved);
    }

    [Fact]
    public void SaveLastFolder_ignores_invalid_paths_without_throwing()
    {
        ClearSessionCache();

        RuleFilePickerSettings.SaveLastFolder(null);
        RuleFilePickerSettings.SaveLastFolder(string.Empty);
        RuleFilePickerSettings.SaveLastFolder(@"C:\nonexistent\missing.bimrule");
    }

    private static void ClearSessionCache()
    {
        typeof(RuleFilePickerSettings)
            .GetField("_sessionLastFolder", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, null);

        foreach (var storageFilePath in GetStorageFilePaths())
        {
            try
            {
                if (File.Exists(storageFilePath))
                {
                    File.Delete(storageFilePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static IEnumerable<string> GetStorageFilePaths()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BIMCapabilities",
            "last-rule-folder.txt");

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BIMCapabilities",
            "last-rule-folder.txt");

        yield return Path.Combine(
            Path.GetTempPath(),
            "BIMCapabilities",
            "last-rule-folder.txt");
    }

    private static string NormalizeDirectory(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

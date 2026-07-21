namespace BIMCapabilities.Launchers.Revit.Execution;

internal static class RuleFilePickerSettings
{
    private static string? _sessionLastFolder;

    private static IEnumerable<string> StorageFilePaths
    {
        get
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
    }

    internal static string ResolveInitialDirectory(string? fallbackDirectory)
    {
        var savedDirectory = ReadLastFolder();
        if (!string.IsNullOrWhiteSpace(savedDirectory) && Directory.Exists(savedDirectory))
        {
            return savedDirectory;
        }

        if (!string.IsNullOrWhiteSpace(fallbackDirectory) && Directory.Exists(fallbackDirectory))
        {
            return fallbackDirectory;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    internal static void SaveLastFolder(string? selectedFilePath)
    {
        if (string.IsNullOrWhiteSpace(selectedFilePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(selectedFilePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        _sessionLastFolder = directory;

        foreach (var storageFilePath in StorageFilePaths)
        {
            if (TryWriteLastFolder(storageFilePath, directory))
            {
                return;
            }
        }
    }

    private static string? ReadLastFolder()
    {
        if (!string.IsNullOrWhiteSpace(_sessionLastFolder) && Directory.Exists(_sessionLastFolder))
        {
            return _sessionLastFolder;
        }

        foreach (var storageFilePath in StorageFilePaths)
        {
            var directory = TryReadLastFolder(storageFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return directory;
            }
        }

        return null;
    }

    private static bool TryWriteLastFolder(string storageFilePath, string directory)
    {
        try
        {
            var settingsDirectory = Path.GetDirectoryName(storageFilePath);
            if (string.IsNullOrWhiteSpace(settingsDirectory))
            {
                return false;
            }

            Directory.CreateDirectory(settingsDirectory);
            File.WriteAllText(storageFilePath, directory);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string? TryReadLastFolder(string storageFilePath)
    {
        try
        {
            if (!File.Exists(storageFilePath))
            {
                return null;
            }

            var directory = File.ReadAllText(storageFilePath).Trim();
            return string.IsNullOrWhiteSpace(directory) ? null : directory;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

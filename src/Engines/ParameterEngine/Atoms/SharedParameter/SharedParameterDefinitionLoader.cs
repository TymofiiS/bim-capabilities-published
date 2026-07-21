using BIMCapabilities.Contracts.Diagnostics;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;

internal static class SharedParameterDefinitionLoader
{
    internal static IReadOnlyList<SharedParameterContracts.SharedParameterDefinition> Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Shared parameter file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Shared parameter file was not found at '{filePath}'.", filePath);
        }

        var definitions = new Dictionary<string, SharedParameterContracts.SharedParameterDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('*'))
            {
                continue;
            }

            if (line.StartsWith("PARAM", StringComparison.OrdinalIgnoreCase))
            {
                var parts = StringParsing.SplitDelimited(line, '\t');
                if (parts.Length >= 3 && Guid.TryParse(parts[1], out _))
                {
                    var revitDefinition = new SharedParameterContracts.SharedParameterDefinition
                    {
                        Name = parts[2],
                        Guid = parts[1],
                        DataType = parts.Length > 3 ? parts[3] : null,
                        Group = "BIM Data"
                    };
                    definitions[revitDefinition.Name] = revitDefinition;
                }

                continue;
            }

            var pipeParts = StringParsing.SplitDelimited(line, '|');
            if (pipeParts.Length == 0 || string.IsNullOrWhiteSpace(pipeParts[0]))
            {
                continue;
            }

            var pipeDefinition = new SharedParameterContracts.SharedParameterDefinition
            {
                Name = pipeParts[0],
                Guid = pipeParts.Length > 1 && pipeParts[1].Length > 0 ? pipeParts[1] : null,
                DataType = pipeParts.Length > 2 && pipeParts[2].Length > 0 ? pipeParts[2] : null,
                Group = pipeParts.Length > 3 && pipeParts[3].Length > 0 ? pipeParts[3] : null
            };

            definitions[pipeDefinition.Name] = pipeDefinition;
        }

        return definitions.Values
            .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

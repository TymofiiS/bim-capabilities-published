using Autodesk.Revit.DB;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal static class Revit2026FamilyParameterContextCollector
{
    internal static Revit2026FamilyParameterContext Collect(Family family, Document document)
    {
        ArgumentGuard.ThrowIfNull(family);
        ArgumentGuard.ThrowIfNull(document);

        var familyDocument = document.EditFamily(family);

        try
        {
            using var familyTransaction = new Transaction(familyDocument, "Read family parameters");
            familyTransaction.Start();

            try
            {
                return ReadContext(familyDocument);
            }
            finally
            {
                if (familyTransaction.GetStatus() == TransactionStatus.Started)
                {
                    familyTransaction.RollBack();
                }
            }
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static Revit2026FamilyParameterContext ReadContext(Document familyDocument)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var valuesByType = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var familyManager = familyDocument.FamilyManager;

        foreach (FamilyParameter familyParameter in familyManager.Parameters)
        {
            names.Add(familyParameter.Definition.Name);
        }

        foreach (FamilyType familyType in familyManager.Types)
        {
            familyManager.CurrentType = familyType;
            var typeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (FamilyParameter familyParameter in familyManager.Parameters)
            {
                var value = ReadFamilyTypeParameterValue(familyType, familyParameter);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    typeValues[familyParameter.Definition.Name] = value;
                }
            }

            valuesByType[familyType.Name] = typeValues;
        }

        return new Revit2026FamilyParameterContext(
            names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            valuesByType.ToDictionary(
                entry => entry.Key,
                entry => (IReadOnlyDictionary<string, string>)entry.Value,
                StringComparer.OrdinalIgnoreCase));
    }

    private static string? ReadFamilyTypeParameterValue(FamilyType familyType, FamilyParameter familyParameter)
    {
        if (familyParameter.StorageType != StorageType.String)
        {
            return null;
        }

        return familyType.AsString(familyParameter);
    }
}

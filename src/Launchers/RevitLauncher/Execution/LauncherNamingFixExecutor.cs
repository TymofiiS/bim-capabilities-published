using Autodesk.Revit.DB;
using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Execution.Logging;
using BIMCapabilities.Launchers.Revit.Commands;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Executes naming rename write requests against the active Revit document.
/// </summary>
public sealed class LauncherNamingFixExecutor
{
    public LauncherNamingFixExecutionResult Execute(
        Document document,
        IReadOnlyList<WriteRequest> writeRequests,
        IExecutionLog? executionLog = null,
        CorrectionProgressScope? progressScope = null)
    {
        ArgumentGuard.ThrowIfNull(document);
        ArgumentGuard.ThrowIfNull(writeRequests);

        var renameRequests = writeRequests
            .Where(request => request.RequestType is WriteRequestType.RenameFamily or WriteRequestType.RenameType)
            .OrderBy(request => request.Order)
            .ToArray();

        if (renameRequests.Length == 0)
        {
            return new LauncherNamingFixExecutionResult
            {
                Succeeded = true,
                NamesRenamed = 0
            };
        }

        var namesRenamed = 0;
        var executedRequests = new List<WriteRequestReference>();
        var processedCount = 0;
        progressScope?.Report(0, renameRequests.Length, "Applying naming corrections...");

        try
        {
            foreach (var writeRequest in renameRequests.Where(request => request.RequestType == WriteRequestType.RenameFamily))
            {
                var currentName = writeRequest.Payload?.GetValueOrDefault("currentName");
                var proposedName = writeRequest.Payload?.GetValueOrDefault("proposedName");

                if (string.IsNullOrWhiteSpace(currentName) || string.IsNullOrWhiteSpace(proposedName))
                {
                    continue;
                }

                processedCount++;
                progressScope?.BeforeDocumentModification();
                progressScope?.Report(
                    processedCount,
                    renameRequests.Length,
                    $"Renaming {processedCount} of {renameRequests.Length}: {currentName} -> {proposedName}");

                if (!TryRenameFamily(document, writeRequest.TargetObject.Id, proposedName, out var errorMessage))
                {
                    return LauncherNamingFixExecutionResult.Failed(errorMessage ?? "Family rename failed.");
                }

                namesRenamed++;
                executedRequests.Add(CreateExecutedRequest(writeRequest));
                LogRename(executionLog, proposedName, processedCount, renameRequests.Length);
                progressScope?.AfterDocumentModification(
                    processedCount,
                    renameRequests.Length,
                    $"Renamed {processedCount} of {renameRequests.Length}: {proposedName}");
            }

            var typeRenameGroups = GroupTypeRenameRequests(document, renameRequests);
            foreach (var group in typeRenameGroups)
            {
                var renames = group.Renames
                    .Where(rename => !string.IsNullOrWhiteSpace(rename.CurrentName)
                        && !string.IsNullOrWhiteSpace(rename.ProposedName))
                    .ToArray();

                if (renames.Length == 0)
                {
                    continue;
                }

                if (!TryResolveFamilyForBatch(document, group.FamilyId, group.FamilyName, out var family, out var resolveError))
                {
                    return LauncherNamingFixExecutionResult.Failed(resolveError ?? "Family resolution failed.");
                }

                var firstRename = renames[0];
                progressScope?.BeforeDocumentModification();
                progressScope?.Report(
                    processedCount + 1,
                    renameRequests.Length,
                    $"Renaming types in {group.FamilyName}: {firstRename.CurrentName} -> {firstRename.ProposedName}");

                if (!TryRenameFamilyTypes(document, family, renames, out var typeError))
                {
                    return LauncherNamingFixExecutionResult.Failed(typeError ?? "Family type rename failed.");
                }

                foreach (var rename in renames)
                {
                    processedCount++;
                    namesRenamed++;
                    executedRequests.Add(CreateExecutedRequest(rename.WriteRequest));
                    progressScope?.Report(
                        processedCount,
                        renameRequests.Length,
                        $"Renamed {processedCount} of {renameRequests.Length}: {rename.ProposedName}");
                    LogRename(executionLog, rename.ProposedName!, processedCount, renameRequests.Length);
                }

                progressScope?.AfterDocumentModification(
                    processedCount,
                    renameRequests.Length,
                    $"Renamed {renames.Length} type(s) in {group.FamilyName}");
            }
        }
        catch (Exception exception)
        {
            ExecutionLogSupport.WriteFixFailed(executionLog, exception.Message);
            return LauncherNamingFixExecutionResult.Failed(exception.Message);
        }

        return new LauncherNamingFixExecutionResult
        {
            Succeeded = true,
            NamesRenamed = namesRenamed,
            ExecutedRequests = executedRequests
        };
    }

    private static void LogRename(IExecutionLog? executionLog, string proposedName, int index, int total)
    {
        ExecutionLogSupport.WriteFixFamilyUpdated(
            executionLog,
            proposedName,
            index,
            total);
    }

    private static WriteRequestReference CreateExecutedRequest(WriteRequest writeRequest)
    {
        return new WriteRequestReference
        {
            RequestId = writeRequest.RequestId,
            RequestType = writeRequest.RequestType,
            Status = WriteRequestStatus.Succeeded,
            Order = writeRequest.Order
        };
    }

    private static IReadOnlyList<FamilyTypeRenameGroup> GroupTypeRenameRequests(
        Document document,
        IReadOnlyList<WriteRequest> renameRequests)
    {
        var groups = new Dictionary<string, FamilyTypeRenameGroup>(StringComparer.Ordinal);

        foreach (var writeRequest in renameRequests.Where(request => request.RequestType == WriteRequestType.RenameType))
        {
            var currentName = writeRequest.Payload?.GetValueOrDefault("currentName");
            var proposedName = writeRequest.Payload?.GetValueOrDefault("proposedName");
            if (string.IsNullOrWhiteSpace(currentName) || string.IsNullOrWhiteSpace(proposedName))
            {
                continue;
            }

            var family = ResolveFamilyForTypeRename(document, writeRequest.TargetObject.Id, currentName);
            if (family is null)
            {
                continue;
            }

            var familyKey = family.Name;
            if (!groups.TryGetValue(familyKey, out var group))
            {
                group = new FamilyTypeRenameGroup(family.Id.ToString(), family.Name);
                groups[familyKey] = group;
            }

            group.Renames.Add(new TypeRenameRequest(writeRequest, currentName, proposedName));
        }

        return groups.Values
            .OrderBy(group => group.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryResolveFamilyForBatch(
        Document document,
        string familyIdText,
        string familyName,
        out Family family,
        out string? errorMessage)
    {
        errorMessage = null;
        var resolved = ResolveFamily(document, familyIdText);
        if (resolved is { IsValidObject: true })
        {
            family = resolved;
            return true;
        }

        resolved = ResolveFamilyByName(document, familyName);
        if (resolved is not null)
        {
            family = resolved;
            return true;
        }

        family = null!;
        errorMessage = $"Unable to resolve family '{familyName}' for type rename.";
        return false;
    }

    private static bool TryRenameFamily(Document document, string familyIdText, string proposedName, out string? errorMessage)
    {
        errorMessage = null;
        var family = ResolveFamily(document, familyIdText);
        if (family is null)
        {
            errorMessage = $"Unable to resolve family '{familyIdText}' for rename.";
            return false;
        }

        using var transaction = new Transaction(document, "Rename family");
        transaction.Start();
        family.Name = proposedName;
        transaction.Commit();
        return true;
    }

    private static bool TryRenameFamilyTypes(
        Document document,
        Family family,
        IReadOnlyList<TypeRenameRequest> renames,
        out string? errorMessage)
    {
        errorMessage = null;
        var familyName = family.Name;
        var familyDocument = document.EditFamily(family);
        try
        {
            using var transaction = new Transaction(familyDocument, "Rename family types");
            transaction.Start();

            var familyManager = familyDocument.FamilyManager;
            foreach (var rename in renames)
            {
                var renamed = false;
                foreach (FamilyType familyType in familyManager.Types)
                {
                    if (!string.Equals(familyType.Name, rename.CurrentName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    familyManager.CurrentType = familyType;
                    familyManager.RenameCurrentType(rename.ProposedName!);
                    renamed = true;
                    break;
                }

                if (!renamed)
                {
                    errorMessage = $"Unable to find family type '{rename.CurrentName}' in family '{familyName}'.";
                    transaction.RollBack();
                    return false;
                }
            }

            transaction.Commit();
            familyDocument.LoadFamily(document, new RevitFamilyLoadOptions());
            return ReconcileFamilyTypesAfterLoad(document, familyName, renames, out errorMessage);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static bool ReconcileFamilyTypesAfterLoad(
        Document document,
        string familyName,
        IReadOnlyList<TypeRenameRequest> renames,
        out string? errorMessage)
    {
        errorMessage = null;
        var family = ResolveFamilyByName(document, familyName);
        if (family is null)
        {
            errorMessage = $"Family '{familyName}' was not found in the project after reload.";
            return false;
        }

        var symbols = CollectFamilySymbols(document, family);
        foreach (var rename in renames)
        {
            if (!symbols.Any(symbol =>
                    string.Equals(symbol.Name, rename.ProposedName, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage =
                    $"Renamed type '{rename.ProposedName}' was not found in family '{familyName}' after reload.";
                return false;
            }
        }

        var symbolsToDelete = new List<ElementId>();
        var instanceRemaps = new List<InstanceTypeRemap>();
        foreach (var rename in renames)
        {
            var newSymbol = symbols.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, rename.ProposedName, StringComparison.OrdinalIgnoreCase));
            if (newSymbol is null)
            {
                continue;
            }

            foreach (var oldSymbol in symbols.Where(candidate =>
                         string.Equals(candidate.Name, rename.CurrentName, StringComparison.OrdinalIgnoreCase)))
            {
                if (oldSymbol.Id == newSymbol.Id)
                {
                    continue;
                }

                symbolsToDelete.Add(oldSymbol.Id);
                instanceRemaps.Add(new InstanceTypeRemap(
                    oldSymbol.Id,
                    newSymbol,
                    rename.CurrentName!,
                    rename.ProposedName!));
            }
        }

        if (symbolsToDelete.Count > 0)
        {
            using var transaction = new Transaction(document, "Reconcile renamed family types");
            transaction.Start();

            foreach (var remap in instanceRemaps)
            {
                if (!TryRemapInstancesToSymbol(
                        document,
                        familyName,
                        remap.OldSymbolId,
                        remap.ProposedName,
                        out errorMessage))
                {
                    transaction.RollBack();
                    return false;
                }
            }

            document.Delete(symbolsToDelete);
            transaction.Commit();
        }

        family = ResolveFamilyByName(document, familyName);
        if (family is null)
        {
            errorMessage = $"Family '{familyName}' was not found after removing superseded types.";
            return false;
        }

        foreach (var rename in renames)
        {
            if (CollectFamilySymbols(document, family).Any(symbol =>
                    string.Equals(symbol.Name, rename.CurrentName, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage =
                    $"Type '{rename.CurrentName}' still exists in family '{familyName}' after automatic correction.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<FamilySymbol> CollectFamilySymbols(Document document, Family family)
    {
        return family.GetFamilySymbolIds()
            .Select(symbolId => document.GetElement(symbolId))
            .OfType<FamilySymbol>()
            .OrderBy(symbol => symbol.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryRemapInstancesToSymbol(
        Document document,
        string familyName,
        ElementId oldSymbolId,
        string proposedName,
        out string? errorMessage)
    {
        errorMessage = null;
        var instances = GetPlacedInstancesForSymbol(document, oldSymbolId);
        if (instances.Count == 0)
        {
            return true;
        }

        var family = ResolveFamilyByName(document, familyName);
        if (family is null)
        {
            errorMessage = $"Family '{familyName}' was not found while remapping placed instances.";
            return false;
        }

        var newSymbol = CollectFamilySymbols(document, family)
            .FirstOrDefault(symbol =>
                string.Equals(symbol.Name, proposedName, StringComparison.OrdinalIgnoreCase));
        if (newSymbol is null)
        {
            errorMessage = $"Renamed type '{proposedName}' was not found while remapping placed instances.";
            return false;
        }

        if (!newSymbol.IsActive)
        {
            newSymbol.Activate();
        }

        document.Regenerate();

        foreach (var instance in instances)
        {
            if (!instance.IsValidObject)
            {
                continue;
            }

            if (instance.Symbol.Id == newSymbol.Id)
            {
                continue;
            }

            try
            {
                instance.ChangeTypeId(newSymbol.Id);
            }
            catch (Exception exception)
            {
                errorMessage =
                    $"Unable to remap placed instance {instance.Id.Value} from '{instance.Symbol.Name}' to '{proposedName}' in family '{familyName}': {exception.Message}";
                return false;
            }

            document.Regenerate();

            if (instance.Symbol.Id != newSymbol.Id)
            {
                errorMessage =
                    $"Placed instance {instance.Id.Value} is still type '{instance.Symbol.Name}' after attempting to change it to '{proposedName}' in family '{familyName}'.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<FamilyInstance> GetPlacedInstancesForSymbol(Document document, ElementId symbolId)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Where(instance => instance.Symbol.Id == symbolId)
            .ToArray();
    }

    private sealed record InstanceTypeRemap(
        ElementId OldSymbolId,
        FamilySymbol NewSymbol,
        string CurrentName,
        string ProposedName);

    private static Family? ResolveFamilyForTypeRename(Document document, string targetIdText, string currentName)
    {
        if (long.TryParse(targetIdText, out var targetIdValue))
        {
            if (document.GetElement(new ElementId(targetIdValue)) is Family family)
            {
                return family;
            }
        }

        return new FilteredElementCollector(document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(candidate =>
                candidate.GetFamilySymbolIds()
                    .Select(id => document.GetElement(id))
                    .OfType<FamilySymbol>()
                    .Any(symbol => symbol.Id.Value.ToString() == targetIdText
                        || string.Equals(symbol.Name, currentName, StringComparison.OrdinalIgnoreCase)));
    }

    private static Family? ResolveFamilyByName(Document document, string familyName)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, familyName, StringComparison.OrdinalIgnoreCase));
    }

    private static Family? ResolveFamily(Document document, string familyIdText)
    {
        if (!long.TryParse(familyIdText, out var familyIdValue))
        {
            return null;
        }

        var family = document.GetElement(new ElementId(familyIdValue)) as Family;
        if (family is not null)
        {
            return family;
        }

        return new FilteredElementCollector(document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(candidate => candidate.Id.Value.ToString() == familyIdText);
    }

    private sealed class FamilyTypeRenameGroup(string familyId, string familyName)
    {
        public string FamilyId { get; } = familyId;

        public string FamilyName { get; } = familyName;

        public List<TypeRenameRequest> Renames { get; } = [];
    }

    private sealed record TypeRenameRequest(WriteRequest WriteRequest, string CurrentName, string? ProposedName);
}

public sealed record LauncherNamingFixExecutionResult
{
    public required bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public int NamesRenamed { get; init; }

    public IReadOnlyList<WriteRequestReference> ExecutedRequests { get; init; } = [];

    public static LauncherNamingFixExecutionResult Failed(string message)
    {
        return new LauncherNamingFixExecutionResult
        {
            Succeeded = false,
            ErrorMessage = message
        };
    }
}

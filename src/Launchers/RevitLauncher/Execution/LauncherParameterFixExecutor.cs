using Autodesk.Revit.DB;
using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Execution.Logging;

using BIMCapabilities.Launchers.Revit.Commands;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Outcome of executing parameter fix write requests in Revit.
/// </summary>
public sealed record LauncherFixExecutionResult
{
    public required bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public int ParametersAdded { get; init; }

    public int ValuesAssigned { get; init; }

    public int AffectedTypes { get; init; }

    public int AffectedFamilies { get; init; }

    public int AffectedInstances { get; init; }

    public int NamesRenamed { get; init; }

    public IReadOnlyList<string> DefaultValuesApplied { get; init; } = [];

    public IReadOnlyList<WriteRequestReference> ExecutedRequests { get; init; } = [];
}

/// <summary>
/// Executes parameter create/update write requests against the active Revit document.
/// </summary>
public sealed class LauncherParameterFixExecutor
{
    public LauncherFixExecutionResult Execute(
        Document document,
        string? sharedParameterFilePath,
        IReadOnlyList<WriteRequest> writeRequests,
        IExecutionLog? executionLog = null,
        CorrectionProgressScope? progressScope = null)
    {
        ArgumentGuard.ThrowIfNull(document);
        ArgumentGuard.ThrowIfNull(writeRequests);

        var parametersAdded = 0;
        var valuesAssigned = 0;
        var affectedTypes = 0;
        var affectedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedInstances = 0;
        var defaultValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var executedRequests = new List<WriteRequestReference>();

        var familyWorkItems = BuildFamilyWorkItems(document, writeRequests, out var preparationError);
        if (preparationError is not null)
        {
            return new LauncherFixExecutionResult
            {
                Succeeded = false,
                ErrorMessage = preparationError
            };
        }

        if (familyWorkItems.Count == 0)
        {
            return new LauncherFixExecutionResult
            {
                Succeeded = false,
                ErrorMessage = "No supported parameter fixes were found for the active model."
            };
        }

        progressScope?.Report(0, familyWorkItems.Count, "Applying automatic correction...");

        try
        {
            var familyIndex = 0;
            foreach (var workItem in familyWorkItems)
            {
                familyIndex++;
                progressScope?.BeforeDocumentModification();
                progressScope?.Report(
                    familyIndex,
                    familyWorkItems.Count,
                    $"Updating family {familyIndex} of {familyWorkItems.Count}: {workItem.FamilyName}");

                var family = ResolveFamily(document, workItem.FamilyId);
                if (family is null)
                {
                    return new LauncherFixExecutionResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"Unable to resolve family '{workItem.FamilyName}' in the active model."
                    };
                }

                var placedInstanceCount = CountPlacedInstancesForFamily(document, workItem.FamilyId);

                var familyDocument = document.EditFamily(family);
                var instanceCorrections = new List<FamilyParameterCorrectionAggregator.ParameterCorrectionState>();
                var familyDocumentClosed = false;
                try
                {
                    using (var familyTransaction = new Transaction(familyDocument, "Update family parameters"))
                    {
                        familyTransaction.Start();

                        var familyManager = familyDocument.FamilyManager;
                        var typesUpdated = 0;

                        foreach (var parameterCorrection in workItem.Parameters.Values)
                        {
                            if (!TryResolveOrCreateFamilyParameter(
                                    document,
                                    sharedParameterFilePath,
                                    familyManager,
                                    parameterCorrection,
                                    out var familyParameter,
                                    out var wasParameterAdded,
                                    out var parameterError))
                            {
                                return new LauncherFixExecutionResult
                                {
                                    Succeeded = false,
                                    ErrorMessage = parameterError
                                };
                            }

                            if (wasParameterAdded)
                            {
                                parametersAdded++;
                            }

                            if (parameterCorrection.IsInstance)
                            {
                                instanceCorrections.Add(parameterCorrection);

                            foreach (FamilyType familyType in familyManager.Types)
                            {
                                familyManager.CurrentType = familyType;
                                var requestedValue = FamilyParameterCorrectionAggregator.ResolveValueForType(
                                    parameterCorrection,
                                    familyType.Name);
                                if (TrySetFamilyParameterValue(
                                        familyManager,
                                        familyParameter,
                                        requestedValue))
                                {
                                    typesUpdated++;
                                    valuesAssigned++;
                                }
                            }

                            TrackAppliedDefaultValue(defaultValues, parameterCorrection);

                            continue;
                        }

                        foreach (FamilyType familyType in familyManager.Types)
                        {
                            familyManager.CurrentType = familyType;
                            var requestedValue = FamilyParameterCorrectionAggregator.ResolveValueForType(
                                parameterCorrection,
                                familyType.Name);
                            if (TrySetFamilyParameterValue(familyManager, familyParameter, requestedValue))
                            {
                                typesUpdated++;
                                valuesAssigned++;
                            }
                        }

                        TrackAppliedDefaultValue(defaultValues, parameterCorrection);
                        }

                        familyTransaction.Commit();
                        affectedTypes += typesUpdated;
                    }

                    familyDocument.LoadFamily(document, new RevitFamilyLoadOptions());
                    familyDocument.Close(false);
                    familyDocumentClosed = true;

                    if (instanceCorrections.Count > 0)
                    {
                        var loadedFamilyId = ResolveLoadedFamilyId(
                            document,
                            workItem.FamilyName,
                            loadedProjectFamily: null,
                            workItem.FamilyId);
                        if (loadedFamilyId == ElementId.InvalidElementId)
                        {
                            return new LauncherFixExecutionResult
                            {
                                Succeeded = false,
                                ErrorMessage =
                                    $"Unable to resolve loaded family '{workItem.FamilyName}' after family reload."
                            };
                        }

                        valuesAssigned += ApplyInstanceParameterValuesToPlacedInstances(
                            document,
                            workItem.FamilyName,
                            loadedFamilyId,
                            instanceCorrections);
                    }

                    affectedFamilies.Add(workItem.FamilyName);
                    affectedInstances += placedInstanceCount;
                }
                finally
                {
                    if (!familyDocumentClosed)
                    {
                        familyDocument.Close(false);
                    }
                }

                executedRequests.AddRange(workItem.SourceRequests);
                ExecutionLogSupport.WriteFixFamilyUpdated(
                    executionLog,
                    workItem.FamilyName,
                    familyIndex,
                    familyWorkItems.Count);
                progressScope?.AfterDocumentModification(
                    familyIndex,
                    familyWorkItems.Count,
                    $"Updated family {familyIndex} of {familyWorkItems.Count}: {workItem.FamilyName}");
            }
        }
        catch (Exception exception)
        {
            ExecutionLogSupport.WriteFixFailed(executionLog, exception.Message);
            return new LauncherFixExecutionResult
            {
                Succeeded = false,
                ErrorMessage = exception.Message
            };
        }

        return new LauncherFixExecutionResult
        {
            Succeeded = true,
            ParametersAdded = parametersAdded,
            ValuesAssigned = valuesAssigned,
            AffectedTypes = affectedTypes,
            AffectedFamilies = affectedFamilies.Count,
            AffectedInstances = affectedInstances,
            DefaultValuesApplied = defaultValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            ExecutedRequests = executedRequests
        };
    }

    private static List<FamilyCorrectionWorkItem> BuildFamilyWorkItems(
        Document document,
        IReadOnlyList<WriteRequest> writeRequests,
        out string? errorMessage)
    {
        errorMessage = null;
        var workItems = new Dictionary<string, FamilyCorrectionWorkItem>(StringComparer.Ordinal);

        foreach (var writeRequest in writeRequests.OrderBy(request => request.Order))
        {
            if (writeRequest.RequestType is not (WriteRequestType.ParameterCreate or WriteRequestType.ParameterUpdate))
            {
                continue;
            }

            var parameterName = writeRequest.Payload?.GetValueOrDefault("parameterName");
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            if (!TryResolveFamily(document, writeRequest.TargetObject.Id, out var family))
            {
                errorMessage = $"Unable to resolve family for target '{writeRequest.TargetObject.Id}'.";
                return [];
            }

            var familyKey = family.Id.ToString();
            if (!workItems.TryGetValue(familyKey, out var workItem))
            {
                workItem = new FamilyCorrectionWorkItem(family.Id, family.Name);
                workItems[familyKey] = workItem;
            }

            var typeName = TryResolveTargetTypeName(document, writeRequest);
            workItem.AddOrUpdateParameter(parameterName, typeName, writeRequest);
        }

        return workItems.Values
            .OrderBy(item => item.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Family? ResolveFamily(Document document, ElementId familyId)
    {
        return document.GetElement(familyId) as Family;
    }

    private static Family? ResolveFamilyByName(Document document, string familyName)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, familyName, StringComparison.OrdinalIgnoreCase));
    }

    private static ElementId ResolveLoadedFamilyId(
        Document document,
        string familyName,
        Family? loadedProjectFamily,
        ElementId fallbackFamilyId)
    {
        var familyByName = ResolveFamilyByName(document, familyName);
        if (familyByName is not null)
        {
            return familyByName.Id;
        }

        if (loadedProjectFamily is not null && loadedProjectFamily.IsValidObject)
        {
            return loadedProjectFamily.Id;
        }

        var familyById = ResolveFamily(document, fallbackFamilyId);
        return familyById?.Id ?? ElementId.InvalidElementId;
    }

    private sealed class FamilyCorrectionWorkItem
    {
        public FamilyCorrectionWorkItem(ElementId familyId, string familyName)
        {
            FamilyId = familyId;
            FamilyName = familyName;
        }

        public ElementId FamilyId { get; }

        public string FamilyName { get; }

        public Dictionary<string, FamilyParameterCorrectionAggregator.ParameterCorrectionState> Parameters { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public List<WriteRequestReference> SourceRequests { get; } = [];

        public void AddOrUpdateParameter(string parameterName, string? typeName, WriteRequest writeRequest)
        {
            var requestedValue = writeRequest.Payload?.GetValueOrDefault("requestedValue") ?? string.Empty;
            var isInstance = string.Equals(
                writeRequest.Payload?.GetValueOrDefault("parameterIsInstance"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            FamilyParameterCorrectionAggregator.AddOrUpdateParameter(
                Parameters,
                parameterName,
                typeName,
                requestedValue,
                isInstance,
                writeRequest,
                SourceRequests);
        }
    }

    private static string? TryResolveTargetTypeName(Document document, WriteRequest writeRequest)
    {
        if (!long.TryParse(writeRequest.TargetObject.Id, out var elementValue))
        {
            return null;
        }

        return document.GetElement(new ElementId(elementValue)) is FamilySymbol familySymbol
            ? familySymbol.Name
            : null;
    }

    private static void TrackAppliedDefaultValue(
        ISet<string> defaultValues,
        FamilyParameterCorrectionAggregator.ParameterCorrectionState parameterCorrection)
    {
        if (parameterCorrection.ValuesByTypeName.Count > 0)
        {
            foreach (var entry in parameterCorrection.ValuesByTypeName.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                defaultValues.Add($"{parameterCorrection.ParameterName}={entry.Value}");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(parameterCorrection.RequestedValue))
        {
            defaultValues.Add($"{parameterCorrection.ParameterName}={parameterCorrection.RequestedValue}");
        }
    }

    private static int CountPlacedInstancesForFamily(Document document, ElementId familyId)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Count(instance => instance.Symbol.Family.Id == familyId);
    }

    private static bool TrySetFamilyParameterValue(
        FamilyManager familyManager,
        FamilyParameter familyParameter,
        string? requestedValue)
    {
        if (string.IsNullOrWhiteSpace(requestedValue))
        {
            return false;
        }

        if (familyParameter.StorageType != StorageType.String)
        {
            return false;
        }

        familyManager.Set(familyParameter, requestedValue);
        return true;
    }

    private static int ApplyInstanceParameterValuesToPlacedInstances(
        Document document,
        string familyName,
        ElementId familyId,
        IReadOnlyList<FamilyParameterCorrectionAggregator.ParameterCorrectionState> instanceCorrections)
    {
        var valuesAssigned = 0;

        using (var transaction = new Transaction(document, "Set instance parameter defaults"))
        {
            transaction.Start();

            foreach (var instance in new FilteredElementCollector(document)
                         .OfClass(typeof(FamilyInstance))
                         .Cast<FamilyInstance>()
                         .Where(candidate => BelongsToFamily(candidate, familyName, familyId)))
            {
                foreach (var correction in instanceCorrections)
                {
                    var parameter = instance.LookupParameter(correction.ParameterName);
                    if (parameter is null || parameter.IsReadOnly)
                    {
                        continue;
                    }

                    var requestedValue = FamilyParameterCorrectionAggregator.ResolveValueForType(
                        correction,
                        instance.Symbol.Name);
                    if (TrySetInstanceParameterValue(parameter, requestedValue))
                    {
                        valuesAssigned++;
                        continue;
                    }

                    if (parameter.StorageType == StorageType.String
                        && !string.IsNullOrWhiteSpace(requestedValue))
                    {
                        parameter.SetValueString(requestedValue);
                        valuesAssigned++;
                    }
                }
            }

            transaction.Commit();
        }

        return valuesAssigned;
    }

    private static bool BelongsToFamily(FamilyInstance instance, string familyName, ElementId familyId)
    {
        if (!instance.IsValidObject)
        {
            return false;
        }

        try
        {
            var symbol = instance.Symbol;
            if (symbol is null || !symbol.IsValidObject)
            {
                return false;
            }

            var family = symbol.Family;
            if (family is null || !family.IsValidObject)
            {
                return false;
            }

            return family.Id == familyId
                || string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException)
        {
            return false;
        }
    }

    private static bool TrySetInstanceParameterValue(Parameter parameter, string? requestedValue)
    {
        if (string.IsNullOrWhiteSpace(requestedValue))
        {
            return false;
        }

        if (parameter.StorageType != StorageType.String)
        {
            return false;
        }

        return parameter.Set(requestedValue);
    }

    private static bool TryResolveFamily(Document document, string targetId, out Family family)
    {
        family = null!;

        if (!long.TryParse(targetId, out var elementValue))
        {
            return false;
        }

        var element = document.GetElement(new ElementId(elementValue));
        switch (element)
        {
            case Family resolvedFamily:
                family = resolvedFamily;
                return true;
            case FamilySymbol familySymbol:
                family = familySymbol.Family;
                return true;
            case FamilyInstance familyInstance when familyInstance.Symbol?.Family is Family instanceFamily:
                family = instanceFamily;
                return true;
            default:
                return false;
        }
    }

    private static bool TryResolveOrCreateFamilyParameter(
        Document document,
        string? sharedParameterFilePath,
        FamilyManager familyManager,
        FamilyParameterCorrectionAggregator.ParameterCorrectionState parameterCorrection,
        out FamilyParameter familyParameter,
        out bool wasParameterAdded,
        out string? errorMessage)
    {
        familyParameter = null!;
        wasParameterAdded = false;
        errorMessage = null;

        var existingParameter = familyManager.get_Parameter(parameterCorrection.ParameterName);
        if (existingParameter is not null)
        {
            familyParameter = existingParameter;
            return true;
        }

        if (parameterCorrection.SampleRequest.RequestType == WriteRequestType.ParameterUpdate)
        {
            errorMessage = $"Parameter '{parameterCorrection.ParameterName}' was not found on the family.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(sharedParameterFilePath))
        {
            var externalDefinition = ResolveExternalDefinition(
                document,
                sharedParameterFilePath,
                parameterCorrection.SampleRequest,
                parameterCorrection.ParameterName);
            if (externalDefinition is not null)
            {
                familyParameter = familyManager.AddParameter(
                    externalDefinition,
                    GroupTypeId.Data,
                    isInstance: parameterCorrection.IsInstance);
                wasParameterAdded = true;
                return true;
            }
        }

        familyParameter = familyManager.AddParameter(
            parameterCorrection.ParameterName,
            GroupTypeId.Data,
            SpecTypeId.String.Text,
            isInstance: parameterCorrection.IsInstance);
        wasParameterAdded = true;
        return true;
    }

    private static ExternalDefinition? ResolveExternalDefinition(
        Document document,
        string sharedParameterFilePath,
        WriteRequest writeRequest,
        string parameterName)
    {
        if (!File.Exists(sharedParameterFilePath))
        {
            return null;
        }

        var application = document.Application;
        var originalSharedParameterFile = application.SharedParametersFilename;

        try
        {
            application.SharedParametersFilename = sharedParameterFilePath;
            var definitionFile = application.OpenSharedParameterFile();
            if (definitionFile is null)
            {
                return null;
            }

            if (writeRequest.Payload?.TryGetValue("parameterDefinitionGuid", out var guidValue) == true
                && Guid.TryParse(guidValue, out var guid))
            {
                var byGuid = FindExternalDefinitionByGuid(definitionFile, guid);
                if (byGuid is not null)
                {
                    return byGuid;
                }
            }

            var projectGuid = FindProjectSharedParameterGuid(document, parameterName);
            if (projectGuid.HasValue)
            {
                var byProjectGuid = FindExternalDefinitionByGuid(definitionFile, projectGuid.Value);
                if (byProjectGuid is not null)
                {
                    return byProjectGuid;
                }
            }

            return FindExternalDefinitionByName(definitionFile, parameterName);
        }
        finally
        {
            application.SharedParametersFilename = originalSharedParameterFile;
        }
    }

    private static Guid? FindProjectSharedParameterGuid(Document document, string parameterName)
    {
        foreach (var sharedParameter in new FilteredElementCollector(document)
                     .OfClass(typeof(SharedParameterElement))
                     .Cast<SharedParameterElement>())
        {
            if (string.Equals(sharedParameter.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return sharedParameter.GuidValue;
            }
        }

        return null;
    }

    private static ExternalDefinition? FindExternalDefinitionByGuid(DefinitionFile definitionFile, Guid guid)
    {
        foreach (DefinitionGroup group in definitionFile.Groups)
        {
            foreach (Definition definition in group.Definitions)
            {
                if (definition is ExternalDefinition externalDefinition
                    && externalDefinition.GUID == guid)
                {
                    return externalDefinition;
                }
            }
        }

        return null;
    }

    private static ExternalDefinition? FindExternalDefinitionByName(DefinitionFile definitionFile, string parameterName)
    {
        foreach (DefinitionGroup group in definitionFile.Groups)
        {
            foreach (Definition definition in group.Definitions)
            {
                if (definition is ExternalDefinition externalDefinition
                    && string.Equals(externalDefinition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return externalDefinition;
                }
            }
        }

        return null;
    }
}

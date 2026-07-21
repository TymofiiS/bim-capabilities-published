using System.Text.Json;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

internal static class TransactionSerialization
{
    public static JsonSerializerOptions Options => WriteRequestSerialization.Options;
}

namespace BIMCapabilities.Adapters.Revit.Tests;

public class Revit2026FamilyReadContractTests
{
    [Fact]
    public void Family_parameter_context_collector_uses_family_transaction_for_edit_family_reads()
    {
        var sourcePath = ResolveAdapterSourcePath(
            Path.Combine("Translation", "Revit2026", "Revit2026FamilyParameterContextCollector.cs"));
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("document.EditFamily(family)", source, StringComparison.Ordinal);
        Assert.Contains("new Transaction(familyDocument", source, StringComparison.Ordinal);
        Assert.Contains("familyTransaction.RollBack()", source, StringComparison.Ordinal);
        Assert.Contains("familyDocument.Close(false)", source, StringComparison.Ordinal);
        Assert.Contains("familyManager.CurrentType = familyType", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Family_type_handle_does_not_call_edit_family_directly()
    {
        var sourcePath = ResolveAdapterSourcePath(
            Path.Combine("Translation", "Revit2026", "Revit2026FamilyTypeHandle.cs"));
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("EditFamily", source, StringComparison.Ordinal);
    }

    private static string ResolveAdapterSourcePath(string relativePath)
    {
        var testProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var adapterSourceDirectory = Path.GetFullPath(
            Path.Combine(testProjectDirectory, "..", "..", "src", "Adapters", "RevitAdapter"));

        return Path.Combine(adapterSourceDirectory, relativePath);
    }
}

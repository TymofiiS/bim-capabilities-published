namespace BIMCapabilities.Generation.Tests;

internal static class BimRuleGenerationTestPrompts
{
    internal const string ArchitectureOpeningsPrompt = """
        Create company rule:

        Doors:
        - name starts with DR_
        - contain RoomName
        - contain FireRating
        - do not contain imported CAD

        Windows:
        - name starts with WN_
        - contain RoomName
        - contain AcousticRating
        - do not contain imported CAD

        Use shared parameters from:
        D:\Company\SharedParameters.txt

        Generate validation report.
        """;

    internal const string DoorOnlyPrompt = """
        Architecture openings standard.

        Doors:
        - name starts with DR_
        - contain RoomName
        - contain FireRating
        - do not contain imported CAD
        """;

    internal const string WindowOnlyPrompt = """
        Architecture openings standard.

        Windows:
        - name starts with WN_
        - contain RoomName
        - contain AcousticRating
        - do not contain imported CAD
        """;

    internal const string InteriorOpeningsPrompt = """
        Interior design company rule:

        Doors:
        - name starts with DR_
        - contain RoomName
        - contain FinishType
        - do not contain imported CAD

        Windows:
        - name starts with WN_
        - contain RoomName
        - contain FinishType
        - do not contain imported CAD

        Use shared parameters from:
        D:\Company\InteriorSharedParameters.txt
        """;

    internal const string MepEquipmentPrompt = """
        MEP equipment validation rule:

        Mechanical Equipment:
        - name starts with ME_
        - contain Manufacturer
        - contain ModelNumber
        - do not contain imported CAD

        Use shared parameters from:
        D:\Company\MepSharedParameters.txt

        Generate validation report.
        """;

    internal const string FurniturePrompt = """
        Create company rule:

        Furniture:
        - contain Manufacturer
        - do not contain imported CAD

        Use shared parameters from:
        ../shared-parameters/CompanySharedParameters.txt

        Generate validation report.
        """;
}

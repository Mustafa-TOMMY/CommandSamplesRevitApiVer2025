using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Analyze Family Instance
    //
    // This command demonstrates the relationship between:
    //
    // FamilyInstance
    //      ↓
    // FamilySymbol
    //      ↓
    // Family
    //
    // A FamilyInstance is an actual placed instance inside the project.
    //
    // The FamilySymbol represents the Type used by that instance.
    //
    // The Family represents the family definition that contains the types.
    //
    // Workflow:
    //
    // User selects FamilyInstance
    //          ↓
    // FamilyInstance
    //          ↓
    // Symbol
    //          ↓
    // Family
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class AnalyzeFamilyInstanceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Family Instance");

                //=====================================================
                // Get Selected Element

                Element element = doc.GetElement(reference);

                //=====================================================
                // Validate FamilyInstance

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Families",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family Symbol

                FamilySymbol symbol = familyInstance.Symbol;

                if (symbol == null)
                {
                    TaskDialog.Show(
                        "Families",
                        "The FamilyInstance does not have a valid FamilySymbol.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family = symbol.Family;

                if (family == null)
                {
                    TaskDialog.Show(
                        "Families",
                        "The FamilySymbol does not have a valid Family.");

                    return Result.Failed;
                }

                //=====================================================
                // Display Information

                string result = "FAMILY INSTANCE\n" + "========================================\n\n" +

                    $"Instance Id:\n" +
                    $"{familyInstance.Id}\n\n" +

                    $"Instance Class:\n" +
                    $"{familyInstance.GetType().Name}\n\n" +

                    "----------------------------------------\n\n" +

                    "FAMILY SYMBOL / TYPE\n" +
                    "========================================\n\n" +

                    $"Symbol Id:\n" +
                    $"{symbol.Id}\n\n" +

                    $"Type Name:\n" +
                    $"{symbol.Name}\n\n" +

                    "----------------------------------------\n\n" +

                    "FAMILY\n" +
                    "========================================\n\n" +

                    $"Family Id:\n" +
                    $"{family.Id}\n\n" +

                    $"Family Name:\n" +
                    $"{family.Name}\n\n" +

                    $"Placement Type:\n" +
                    $"{family.FamilyPlacementType}";

                //=====================================================

                TaskDialog.Show(
                    "Family Analysis",
                    result);

                //=====================================================

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
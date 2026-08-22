using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Open Family Document
    //
    // This command demonstrates how to obtain a Family Document from
    // a Family loaded inside a Project Document.
    //
    // Workflow:
    //
    // Project Document
    //      ↓
    // Select FamilyInstance
    //      ↓
    // Get Family
    //      ↓
    // EditFamily()
    //      ↓
    // Family Document
    //
    // Important:
    //
    // A Family loaded into a Project is NOT itself a Family Document.
    //
    // The Project contains the Family.
    //
    // EditFamily() creates/opens a separate Family Document context
    // that allows us to work with the Family itself.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 04
    public class OpenFamilyDocumentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;

                UIDocument uiDoc = uiApp.ActiveUIDocument;

                Document projectDoc = uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Family Instance");

                Element element = projectDoc.GetElement(reference);

                //=====================================================
                // Validate Family Instance

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Family Document",
                        "The selected element is not a Family Instance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family = familyInstance.Symbol.Family;

                if (family == null)
                {
                    TaskDialog.Show(
                        "Family Document",
                        "The selected Family Instance has no Family.");

                    return Result.Failed;
                }

                //=====================================================
                // Open Family Document

                Document familyDoc = projectDoc.EditFamily(family);

                if (familyDoc == null)
                {
                    TaskDialog.Show(
                        "Family Document",
                        "The Family Document could not be opened.");

                    return Result.Failed;
                }

                //=====================================================
                // Display Information

                TaskDialog.Show(
                    "Family Document",
                    $"Family:\n{family.Name}\n\n" +
                    $"Project Document:\n{projectDoc.Title}\n\n" +
                    $"Family Document:\n{familyDoc.Title}\n\n" +
                    $"Is Family Document:\n" +
                    $"{familyDoc.IsFamilyDocument}");

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
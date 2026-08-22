using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Edit Family Command
    //
    // This command demonstrates:
    //
    // Project Document
    //        ↓
    //  FamilyInstance
    //        ↓
    //   FamilySymbol
    //        ↓
    //      Family
    //        ↓
    //   EditFamily()
    //        ↓
    //  Family Document
    //
    // Important Concept:
    //
    // A Family in Revit is defined in its own separate Document context.
    //
    // When you call Document.EditFamily(family), Revit opens the Family definition
    // into a new in-memory Document (Family Document).
    //
    // Project Document  ≠  Family Document
    //  (doc.IsFamilyDocument == false)  vs  (familyDoc.IsFamilyDocument == true)
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class EditFamilyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select a FamilyInstance in the Project
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Family Instance to edit its Family");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Edit Family", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Obtain FamilySymbol & Family
                //=====================================================

                FamilySymbol symbol = familyInstance.Symbol;
                if (symbol == null)
                {
                    TaskDialog.Show("Edit Family", "The FamilyInstance does not have a valid FamilySymbol.");
                    return Result.Failed;
                }

                Family family = symbol.Family;
                if (family == null)
                {
                    TaskDialog.Show("Edit Family", "The FamilySymbol does not have a valid Family.");
                    return Result.Failed;
                }

                //=====================================================
                // 3. Verify Family Is Editable
                //=====================================================

                if (!family.IsEditable)
                {
                    TaskDialog.Show("Edit Family",
                        $"The family '{family.Name}' is not editable.\n" +
                        $"System families (Walls, Floors) and in-place families cannot be opened with EditFamily().");
                    return Result.Failed;
                }

                //=====================================================
                // 4. Open Family Document via EditFamily()
                //=====================================================

                Document familyDoc = doc.EditFamily(family);

                if (familyDoc == null)
                {
                    TaskDialog.Show("Edit Family", "Could not open the Family Document.");
                    return Result.Failed;
                }

                try
                {
                    //=====================================================
                    // 5. Compare Project Document vs Family Document
                    //=====================================================

                    string info = "EDIT FAMILY DOCUMENT COMPARISON\n" +
                        "========================================\n\n" +

                        "PROJECT DOCUMENT:\n" +
                        $"Title: {doc.Title}\n" +
                        $"Is Family Document: {doc.IsFamilyDocument}\n" +
                        $"Path: {(string.IsNullOrEmpty(doc.PathName) ? "Unsaved Project" : doc.PathName)}\n\n" +

                        "----------------------------------------\n\n" +

                        "FAMILY DOCUMENT:\n" +
                        $"Title: {familyDoc.Title}\n" +
                        $"Is Family Document: {familyDoc.IsFamilyDocument}\n" +
                        $"Owner Family Name: {familyDoc.OwnerFamily?.Name ?? family.Name}\n" +
                        $"Path: {(string.IsNullOrEmpty(familyDoc.PathName) ? "In-Memory Family" : familyDoc.PathName)}";

                    TaskDialog.Show("Edit Family", info);
                }
                finally
                {
                    //=====================================================
                    // 6. Close Family Document cleanly without saving
                    //=====================================================
                    familyDoc.Close(false);
                }

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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Manager Inspection Command
    //
    // This command demonstrates:
    //
    // Project Document
    //       ≠
    // Family Document
    //       ↓
    // FamilyManager
    //
    // Important Concept:
    //
    // Inside a Family Document, the gateway for inspecting and managing
    // Family Types and Family Parameters is the FamilyManager property:
    //
    // familyDocument.FamilyManager
    //
    // It provides access to:
    // - FamilyManager.CurrentType
    // - FamilyManager.Types (Collection of FamilyType objects)
    // - FamilyManager.Parameters (Collection of FamilyParameter objects)
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 07
    public class FamilyManagerInspectionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select a FamilyInstance
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Family Instance to inspect its FamilyManager");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("FamilyManager", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                Family family = familyInstance.Symbol?.Family;

                if (family == null || !family.IsEditable)
                {
                    TaskDialog.Show("FamilyManager", "The family is null or not editable.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Open Family Document & Access FamilyManager
                //=====================================================

                Document familyDoc = doc.EditFamily(family);
                if (familyDoc == null)
                {
                    TaskDialog.Show("FamilyManager", "Could not open Family Document.");
                    return Result.Failed;
                }

                try
                {
                    FamilyManager familyMgr = familyDoc.FamilyManager;

                    //=====================================================
                    // 3. Inspect FamilyManager State
                    //=====================================================

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("FAMILY MANAGER INSPECTION");
                    sb.AppendLine("========================================");
                    sb.AppendLine($"Family Document : {familyDoc.Title}");
                    sb.AppendLine($"Current Type    : {familyMgr.CurrentType?.Name ?? "No Current Type"}");
                    sb.AppendLine($"Total Types     : {familyMgr.Types.Size}");
                    sb.AppendLine($"Total Parameters: {familyMgr.Parameters.Size}");
                    sb.AppendLine();
                    sb.AppendLine("FAMILY TYPES IN FAMILY DOCUMENT:");
                    sb.AppendLine("========================================");

                    int typeIndex = 1;
                    foreach (FamilyType familyType in familyMgr.Types)
                    {
                        bool isCurrent = (familyMgr.CurrentType != null && familyType.Name == familyMgr.CurrentType.Name);
                        sb.AppendLine($"#{typeIndex} Type Name: {familyType.Name}{(isCurrent ? " (ACTIVE/CURRENT)" : "")}");
                        typeIndex++;
                    }

                    TaskDialog.Show("FamilyManager Inspection", sb.ToString());
                }
                finally
                {
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

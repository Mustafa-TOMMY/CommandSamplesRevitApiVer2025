using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Type Management Command
    //
    // This command demonstrates:
    //
    // Family Document
    //       ↓
    // FamilyManager
    //       ↓
    // Family Types (FamilyType)
    //       ↓
    // CurrentType
    //
    // Important Conceptual Distinction:
    //
    // Family Document Side:
    //     Types are represented by FamilyType objects inside FamilyManager.Types.
    //     The active type is accessed via FamilyManager.CurrentType.
    //
    // Project Document Side:
    //     Types are represented by FamilySymbol elements in the project database.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 10
    public class FamilyTypeManagementCommand : IExternalCommand
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
                    "Select a Family Instance to inspect Family Types in FamilyManager");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Family Types", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                Family family = familyInstance.Symbol?.Family;
                if (family == null || !family.IsEditable)
                {
                    TaskDialog.Show("Family Types", "The family is null or not editable.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Open Family Document & Access FamilyManager
                //=====================================================

                Document familyDoc = doc.EditFamily(family);
                if (familyDoc == null)
                {
                    TaskDialog.Show("Family Types", "Could not open Family Document.");
                    return Result.Failed;
                }

                try
                {
                    FamilyManager familyMgr = familyDoc.FamilyManager;
                    FamilyType initialCurrentType = familyMgr.CurrentType;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("FAMILY TYPE MANAGEMENT (FamilyManager)");
                    sb.AppendLine("========================================");
                    sb.AppendLine($"Family Document : {familyDoc.Title}");
                    sb.AppendLine($"Initial Current : {initialCurrentType?.Name ?? "None"}");
                    sb.AppendLine($"Total Types     : {familyMgr.Types.Size}");
                    sb.AppendLine();
                    sb.AppendLine("FAMILY TYPES DEFINED IN FAMILY MANAGER:");
                    sb.AppendLine("========================================");

                    int index = 1;
                    FamilyType targetTypeToSwitch = null;

                    foreach (FamilyType type in familyMgr.Types)
                    {
                        bool isCurrent = (initialCurrentType != null && type.Name == initialCurrentType.Name);
                        sb.AppendLine($"Type #{index}: {type.Name}{(isCurrent ? " [CURRENT]" : "")}");

                        if (!isCurrent && targetTypeToSwitch == null)
                        {
                            targetTypeToSwitch = type;
                        }

                        index++;
                    }

                    // Demonstrate switching CurrentType in Family Document if another type exists
                    if (targetTypeToSwitch != null)
                    {
                        using (Transaction t = new Transaction(familyDoc, "Switch Family Type"))
                        {
                            t.Start();
                            familyMgr.CurrentType = targetTypeToSwitch;
                            t.Commit();
                        }

                        sb.AppendLine();
                        sb.AppendLine("----------------------------------------");
                        sb.AppendLine($"Demonstration: CurrentType switched to '{familyMgr.CurrentType.Name}'");
                    }

                    TaskDialog.Show("Family Type Management", sb.ToString());
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

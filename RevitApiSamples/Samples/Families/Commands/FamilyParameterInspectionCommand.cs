using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Parameter Inspection Command
    //
    // This command demonstrates:
    //
    // FamilyDocument
    //       ↓
    // FamilyManager
    //       ↓
    // Parameters (FamilyParameterSet)
    //       ↓
    // FamilyParameter
    //
    // Important Distinction:
    //
    // FamilyParameter:
    //     Represents a parameter definition INSIDE the Family Document.
    //     Belongs to FamilyManager on the family-definition side.
    //
    // Parameter:
    //     Represents a parameter instance bound to an Element or ElementType
    //     INSIDE the Project Document.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 08
    public class FamilyParameterInspectionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select FamilyInstance
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Family Instance to inspect its Family Parameters");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Family Parameters", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                Family family = familyInstance.Symbol?.Family;

                if (family == null || !family.IsEditable)
                {
                    TaskDialog.Show("Family Parameters", "The family is null or not editable.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Open Family Document & Access FamilyManager
                //=====================================================

                Document familyDoc = doc.EditFamily(family);
                if (familyDoc == null)
                {
                    TaskDialog.Show("Family Parameters", "Could not open Family Document.");
                    return Result.Failed;
                }

                try
                {
                    FamilyManager familyMgr = familyDoc.FamilyManager;
                    FamilyParameterSet parameters = familyMgr.Parameters;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("FAMILY PARAMETER INSPECTION");
                    sb.AppendLine("========================================");
                    sb.AppendLine($"Family Document : {familyDoc.Title}");
                    sb.AppendLine($"Parameter Count : {parameters.Size}");
                    sb.AppendLine();
                    sb.AppendLine("FAMILY PARAMETERS:");
                    sb.AppendLine("========================================");

                    int index = 1;
                    foreach (FamilyParameter famParam in parameters)
                    {
                        string name = famParam.Definition.Name;
                        string isInstanceStr = famParam.IsInstance ? "Instance" : "Type";
                        string isSharedStr = famParam.IsShared ? "Yes" : "No";
                        string isReadOnlyStr = famParam.IsReadOnly ? "Yes" : "No";
                        string formula = string.IsNullOrEmpty(famParam.Formula) ? "None" : famParam.Formula;

                        sb.AppendLine($"#{index}: {name}");
                        sb.AppendLine($"  Scope     : {isInstanceStr}");
                        sb.AppendLine($"  Shared    : {isSharedStr}");
                        sb.AppendLine($"  ReadOnly  : {isReadOnlyStr}");
                        sb.AppendLine($"  Formula   : {formula}");
                        sb.AppendLine("----------------------------------------");

                        index++;
                    }

                    TaskDialog.Show("Family Parameters", sb.ToString());
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

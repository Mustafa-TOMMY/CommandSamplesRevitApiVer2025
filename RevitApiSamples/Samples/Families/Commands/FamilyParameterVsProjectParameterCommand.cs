using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Parameter vs Project Parameter Command
    //
    // This command demonstrates the fundamental architectural difference:
    //
    // FAMILY SIDE:
    // ----------------------------------------
    // FamilyDocument
    //      ↓
    // FamilyManager
    //      ↓
    // FamilyParameter  (Defined inside .rfa; applies only to this family)
    //
    //
    // PROJECT SIDE:
    // ----------------------------------------
    // Project Document
    //      ↓
    // ParameterBindings (BindingMap)
    //      ↓
    // Category Binding  (Defined inside .rvt; applies to ALL elements of bound categories)
    //
    //
    // SHARED PARAMETERS:
    // ----------------------------------------
    // External .txt File with GUIDs
    // Can be loaded into FamilyDocument (via FamilyManager) OR Project Document (via BindingMap).
    // Can be scheduled and tagged.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 09
    public class FamilyParameterVsProjectParameterCommand : IExternalCommand
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
                    "Select a Family Instance to compare Family vs Project Parameters");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Parameter Comparison", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                Family family = familyInstance.Symbol?.Family;
                if (family == null || !family.IsEditable)
                {
                    TaskDialog.Show("Parameter Comparison", "The family is null or not editable.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Inspect Family Parameters via FamilyManager
                //=====================================================

                int familyParamCount = 0;
                int familySharedCount = 0;
                StringBuilder familyParamNames = new StringBuilder();

                Document familyDoc = doc.EditFamily(family);
                if (familyDoc != null)
                {
                    try
                    {
                        FamilyManager familyMgr = familyDoc.FamilyManager;
                        familyParamCount = familyMgr.Parameters.Size;

                        foreach (FamilyParameter fp in familyMgr.Parameters)
                        {
                            if (fp.IsShared) familySharedCount++;
                            if (familyParamNames.Length < 200)
                            {
                                familyParamNames.Append(fp.Definition.Name).Append(", ");
                            }
                        }
                    }
                    finally
                    {
                        familyDoc.Close(false);
                    }
                }

                //=====================================================
                // 3. Inspect Project Parameters via BindingMap
                //=====================================================

                BindingMap bindingMap = doc.ParameterBindings;
                DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
                iterator.Reset();

                int projectParamCount = 0;
                StringBuilder projectParamNames = new StringBuilder();

                while (iterator.MoveNext())
                {
                    Definition def = iterator.Key;
                    projectParamCount++;
                    if (projectParamNames.Length < 200)
                    {
                        projectParamNames.Append(def.Name).Append(", ");
                    }
                }

                //=====================================================
                // 4. Build Comparative Summary
                //=====================================================

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("FAMILY PARAMETER VS PROJECT PARAMETER");
                sb.AppendLine("========================================");
                sb.AppendLine();
                sb.AppendLine("1. FAMILY PARAMETER SIDE (FamilyManager):");
                sb.AppendLine($"   Scope         : Defined in '{family.Name}.rfa'");
                sb.AppendLine($"   Total Params  : {familyParamCount}");
                sb.AppendLine($"   Shared Params : {familySharedCount}");
                sb.AppendLine($"   Examples      : {familyParamNames.ToString().TrimEnd(',', ' ')}");
                sb.AppendLine("   Rule          : Only applies to instances/types of THIS family.");
                sb.AppendLine();
                sb.AppendLine("----------------------------------------");
                sb.AppendLine();
                sb.AppendLine("2. PROJECT PARAMETER SIDE (BindingMap):");
                sb.AppendLine($"   Scope         : Defined in Project '{doc.Title}'");
                sb.AppendLine($"   Total Bound   : {projectParamCount}");
                sb.AppendLine($"   Examples      : {projectParamNames.ToString().TrimEnd(',', ' ')}");
                sb.AppendLine("   Rule          : Applies to ALL elements of bound categories across all families.");
                sb.AppendLine();
                sb.AppendLine("----------------------------------------");
                sb.AppendLine();
                sb.AppendLine("3. SHARED PARAMETER RECONCILIATION:");
                sb.AppendLine("   - External .txt file with unique GUIDs.");
                sb.AppendLine("   - Required if a Family Parameter must be tagged or scheduled in a Project.");

                TaskDialog.Show("Parameter Comparison", sb.ToString());

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

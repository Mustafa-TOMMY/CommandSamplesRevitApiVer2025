using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Placement Type Command
    //
    // This command demonstrates:
    //
    // Family
    //    ↓
    // FamilyPlacementType
    //
    // Why FamilyPlacementType Matters:
    //
    // Before calling Document.Create.NewFamilyInstance(), you MUST know how the
    // Family is placed in 3D space:
    //
    // - OneLevelBased    → Requires XYZ point + Level
    // - TwoLevelsBased   → Requires XYZ point + Base Level + Top Level (e.g. Columns)
    // - WorkPlaneBased   → Requires Reference/Face + XYZ point + Vector
    // - ViewBased        → Requires View + XYZ point (Annotations, Detail Items)
    // - CurveBased       → Requires Curve + Level / Face (Line-based families, Beams)
    //
    // Calling the wrong NewFamilyInstance() overload for a placement type will
    // cause an InvalidOperationException at runtime.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 11
    public class FamilyPlacementTypeCommand : IExternalCommand
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
                    "Select a Family Instance to inspect its FamilyPlacementType");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Family Placement", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                FamilySymbol symbol = familyInstance.Symbol;
                Family family = symbol?.Family;

                if (family == null)
                {
                    TaskDialog.Show("Family Placement", "The selected instance does not have a valid Family.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Inspect FamilyPlacementType
                //=====================================================

                FamilyPlacementType placementType = family.FamilyPlacementType;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("FAMILY PLACEMENT TYPE ANALYSIS");
                sb.AppendLine("========================================");
                sb.AppendLine($"Family Name     : {family.Name}");
                sb.AppendLine($"Symbol / Type   : {symbol.Name}");
                sb.AppendLine($"Placement Type  : {placementType}");
                sb.AppendLine();
                sb.AppendLine("PLACEMENT REQUIREMENTS GUIDE:");
                sb.AppendLine("========================================");

                switch (placementType)
                {
                    case FamilyPlacementType.OneLevelBased:
                        sb.AppendLine("• OneLevelBased:");
                        sb.AppendLine("  Requires: Location XYZ point + Target Level.");
                        sb.AppendLine("  Overload: doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType)");
                        break;

                    case FamilyPlacementType.TwoLevelsBased:
                        sb.AppendLine("• TwoLevelsBased:");
                        sb.AppendLine("  Requires: Location XYZ point + Base Level + Top Level.");
                        sb.AppendLine("  Overload: doc.Create.NewFamilyInstance(pt, symbol, baseLevel, structuralType)");
                        break;

                    case FamilyPlacementType.WorkPlaneBased:
                        sb.AppendLine("• WorkPlaneBased / FaceBased:");
                        sb.AppendLine("  Requires: Face / Reference + XYZ point + Direction vector.");
                        sb.AppendLine("  Overload: doc.Create.NewFamilyInstance(reference, pt, dir, symbol)");
                        break;

                    case FamilyPlacementType.ViewBased:
                        sb.AppendLine("• ViewBased:");
                        sb.AppendLine("  Requires: Target View + XYZ point.");
                        sb.AppendLine("  Overload: doc.Create.NewFamilyInstance(pt, symbol, view)");
                        break;

                    case FamilyPlacementType.CurveBased:
                        sb.AppendLine("• CurveBased:");
                        sb.AppendLine("  Requires: Line / Curve + Level / Host.");
                        sb.AppendLine("  Overload: doc.Create.NewFamilyInstance(curve, symbol, level, structuralType)");
                        break;

                    default:
                        sb.AppendLine($"• {placementType}:");
                        sb.AppendLine("  Standard element placement rules apply.");
                        break;
                }

                TaskDialog.Show("Family Placement Type", sb.ToString());

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

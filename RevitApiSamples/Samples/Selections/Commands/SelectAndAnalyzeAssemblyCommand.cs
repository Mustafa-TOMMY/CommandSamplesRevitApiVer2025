using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Selection.Commands
{
    // ============================================================================
    // Select And Analyze Assembly
    //
    // This command demonstrates how to:
    //
    // User
    //   ↓
    // Select Assembly
    //   ↓
    // AssemblyInstance
    //   ↓
    // GetMemberIds()
    //   ↓
    // Analyze Member Elements
    //
    // Important:
    //
    // An Assembly is a container of Elements.
    //
    // The AssemblyInstance belongs to the current Project Document,
    // while its member Elements are also retrieved from that Document.
    //
    // This command focuses on the relationship:
    //
    // Selection → Assembly → Members → Element Information
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 14
    public class SelectAndAnalyzeAssemblyCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp =
                    commandData.Application;

                UIDocument uiDoc =
                    uiApp.ActiveUIDocument;

                Document doc =
                    uiDoc.Document;

                //=====================================================
                // Select Assembly

                Reference reference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select an Assembly");

                //=====================================================
                // Get Selected Element

                Element selectedElement =
                    doc.GetElement(reference);

                //=====================================================
                // Validate Assembly

                AssemblyInstance assembly =
                    selectedElement as AssemblyInstance;

                if (assembly == null)
                {
                    TaskDialog.Show(
                        "Assembly Analysis",
                        "The selected element is not an Assembly.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Assembly Members

                ICollection<ElementId> memberIds =
                    assembly.GetMemberIds();

                //=====================================================
                // Build Result

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "ASSEMBLY ANALYSIS");

                sb.AppendLine(
                    "========================================");

                sb.AppendLine(
                    $"Assembly Id:\n{assembly.Id}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Assembly Name:\n{assembly.Name}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Member Count:\n{memberIds.Count}");

                sb.AppendLine();

                sb.AppendLine(
                    "MEMBERS");

                sb.AppendLine(
                    "========================================");

                int index = 1;

                foreach (ElementId memberId in memberIds)
                {
                    Element member =
                        doc.GetElement(memberId);

                    if (member == null)
                        continue;

                    sb.AppendLine(
                        $"Member #{index}");

                    sb.AppendLine(
                        $"Element Id    : {member.Id}");

                    sb.AppendLine(
                        $"Class         : {member.GetType().Name}");

                    sb.AppendLine(
                        $"Category      : " +
                        $"{member.Category?.Name ?? "None"}");

                    //=================================================
                    // Family Information

                    FamilyInstance familyInstance =
                        member as FamilyInstance;

                    if (familyInstance != null)
                    {
                        FamilySymbol symbol =
                            familyInstance.Symbol;

                        Family family =
                            symbol?.Family;

                        sb.AppendLine(
                            $"Family        : " +
                            $"{family?.Name ?? "None"}");

                        sb.AppendLine(
                            $"Type          : " +
                            $"{symbol?.Name ?? "None"}");

                        sb.AppendLine(
                            $"Placement     : " +
                            $"{family?.FamilyPlacementType.ToString() ?? "None"}");
                    }
                    else
                    {
                        sb.AppendLine(
                            "Family        : Not a FamilyInstance");

                        sb.AppendLine(
                            "Type          : Not a FamilyInstance");

                        sb.AppendLine(
                            "Placement     : Not applicable");
                    }

                    //=================================================
                    // Location

                    Location location =
                        member.Location;

                    if (location is LocationPoint locationPoint)
                    {
                        XYZ point =
                            locationPoint.Point;

                        sb.AppendLine(
                            $"Location      : Point");

                        sb.AppendLine(
                            $"Point         : " +
                            $"({point.X:F3}, " +
                            $"{point.Y:F3}, " +
                            $"{point.Z:F3})");
                    }
                    else if (location is LocationCurve locationCurve)
                    {
                        Curve curve =
                            locationCurve.Curve;

                        XYZ startPoint =
                            curve.GetEndPoint(0);

                        XYZ endPoint =
                            curve.GetEndPoint(1);

                        XYZ direction =
                            (endPoint - startPoint).Normalize();

                        double length =
                            curve.Length;

                        sb.AppendLine(
                            $"Location      : Curve");

                        sb.AppendLine(
                            $"Start Point   : " +
                            $"({startPoint.X:F3}, " +
                            $"{startPoint.Y:F3}, " +
                            $"{startPoint.Z:F3})");

                        sb.AppendLine(
                            $"End Point     : " +
                            $"({endPoint.X:F3}, " +
                            $"{endPoint.Y:F3}, " +
                            $"{endPoint.Z:F3})");

                        sb.AppendLine(
                            $"Direction     : " +
                            $"({direction.X:F3}, " +
                            $"{direction.Y:F3}, " +
                            $"{direction.Z:F3})");

                        sb.AppendLine(
                            $"Length        : " +
                            $"{length:F3} ft");
                    }
                    else
                    {
                        sb.AppendLine(
                            "Location      : None / Unsupported");
                    }

                    sb.AppendLine(
                        "----------------------------------------");

                    index++;
                }

                //=====================================================

                TaskDialog.Show(
                    "Assembly Analysis",
                    sb.ToString());

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
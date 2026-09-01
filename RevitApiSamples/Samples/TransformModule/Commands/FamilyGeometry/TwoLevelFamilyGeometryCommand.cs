using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // Two-Level Family Geometry Command
    //
    // Part B - Command 05
    //
    // Example:
    //
    // Structural Column
    // Type: IPE 200
    // Base Level: Level 1
    // Top Level : Level 2
    //
    // Important:
    //
    // "IPE 200" describes the structural section/type.
    //
    // "TwoLevelsBased" describes the placement architecture of the Family.
    //
    // The command does NOT assume that every TwoLevelsBased Family has the same
    // runtime Location representation.
    //
    // Strategy:
    //
    // 1. Verify FamilyPlacementType
    // 2. Inspect Base Level / Top Level
    // 3. Inspect actual Location runtime type
    // 4. If LocationPoint -> inspect its point and rotation
    // 5. If LocationCurve -> inspect Start / End / Length / Direction
    // 6. Inspect Transform
    // 7. Derive only information that is actually supported
    //
    // This command is READ-ONLY.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class TwoLevelFamilyGeometryCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
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
                        "Select a Two-Level FamilyInstance, such as a structural column");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Two-Level Family",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Family / Symbol
                //=====================================================

                FamilySymbol symbol = familyInstance.Symbol;

                Family family = symbol?.Family;

                if (symbol == null || family == null)
                {
                    TaskDialog.Show(
                        "Two-Level Family",
                        "Could not obtain Family or FamilySymbol.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Get Placement Type
                //=====================================================

                FamilyPlacementType placementType = family.FamilyPlacementType;

                //=====================================================
                // 4. Verify TwoLevelsBased
                //=====================================================

                if (placementType != FamilyPlacementType.TwoLevelsBased)
                {
                    TaskDialog.Show(
                        "Two-Level Family",
                        $"The selected Family has placement type:\n\n" +
                        $"{placementType}\n\n" +
                        "This command is intended for TwoLevelsBased families.");

                    return Result.Failed;
                }

                //=====================================================
                // 5. Get Base Level
                //=====================================================
                var baseLevelID = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?
                            .AsElementId() ?? ElementId.InvalidElementId;

                Element baseLevelElement = doc.GetElement(baseLevelID);

                Level baseLevel = baseLevelElement as Level;

                //=====================================================
                // 6. Get Top Level
                //=====================================================
                var topLevelId = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?
                    .AsElementId() ?? ElementId.InvalidElementId;

                Element topLevelElement = doc.GetElement(topLevelId);

                Level topLevel = topLevelElement as Level;

                //=====================================================
                // 7. Inspect Actual Location
                //=====================================================

                Location location = familyInstance.Location;
                string locationType = location == null ? "null" : location.GetType().Name;

                //=====================================================
                // 8. Prepare Geometry Variables
                //=====================================================

                XYZ startPoint = null;
                XYZ endPoint = null;
                XYZ direction = null;

                double actualLength = 0.0;

                double rotation = double.NaN;

                bool lengthAvailable = false;
                bool directionAvailable = false;
                bool rotationAvailable = false;

                string geometrySource = "Not determined";

                //=====================================================
                // 9. LocationPoint Analysis
                //=====================================================

                LocationPoint locationPoint = location as LocationPoint;

                if (locationPoint != null)
                {
                    geometrySource = "LocationPoint";
                    startPoint = locationPoint.Point;
                    rotation = locationPoint.Rotation;

                    rotationAvailable = true;

                    // -------------------------------------------------
                    // A LocationPoint gives us an insertion point.
                    // It does NOT automatically give us an end point
                    // or physical member length.
                    // Therefore we do not invent an End Point here.
                    // -------------------------------------------------
                }

                //=====================================================
                // 10. LocationCurve Analysis
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                if (locationCurve != null)
                {
                    geometrySource = "LocationCurve";

                    Curve curve = locationCurve.Curve;

                    if (curve != null)
                    {
                        startPoint = curve.GetEndPoint(0);

                        endPoint = curve.GetEndPoint(1);

                        actualLength = curve.Length;

                        lengthAvailable = actualLength > 1e-9;

                        XYZ rawDirection = endPoint - startPoint;

                        if (rawDirection.GetLength() > 1e-9)
                        {
                            direction = rawDirection.Normalize();
                            directionAvailable = true;
                        }
                    }
                }

                //=====================================================
                // 11. Transform Analysis
                //=====================================================

                Transform transform = null;

                try
                {
                    transform = familyInstance.GetTransform();
                }
                catch
                {
                    transform = null;
                }

                //=====================================================
                // 12. Level-Based Vertical Information
                //=====================================================

                double levelElevationDifference = double.NaN;

                if (baseLevel != null && topLevel != null)
                {
                    levelElevationDifference = topLevel.Elevation - baseLevel.Elevation;
                }

                //=====================================================
                // 13. Build Report
                //=====================================================

                #region Report Strcture
                StringBuilder sb =
            new StringBuilder();

                sb.AppendLine(
                    "TWO-LEVEL FAMILY GEOMETRY");

                sb.AppendLine(
                    "========================================");

                //=====================================================
                // Family
                //=====================================================

                sb.AppendLine(
                    "1. FAMILY INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Element Id      : {familyInstance.Id}");

                sb.AppendLine(
                    $"Family Name     : {family.Name}");

                sb.AppendLine(
                    $"Symbol / Type   : {symbol.Name}");

                sb.AppendLine(
                    $"Placement Type  : {placementType}");

                sb.AppendLine();

                //=====================================================
                // Example Context
                //=====================================================

                sb.AppendLine(
                    "2. EXAMPLE CONTEXT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Example: Structural Column / IPE 200");

                sb.AppendLine();

                sb.AppendLine(
                    "IPE 200 describes the structural section/type.");

                sb.AppendLine(
                    "TwoLevelsBased describes the placement architecture.");

                sb.AppendLine();

                //=====================================================
                // Levels
                //=====================================================

                sb.AppendLine(
                    "3. LEVEL INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Base Level:");

                if (baseLevel != null)
                {
                    sb.AppendLine(
                        $"  {baseLevel.Name}");

                    sb.AppendLine(
                        $"  Elevation: {baseLevel.Elevation:F6} ft");
                }
                else
                {
                    sb.AppendLine(
                        "  Not available.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "Top Level:");

                if (topLevel != null)
                {
                    sb.AppendLine(
                        $"  {topLevel.Name}");

                    sb.AppendLine(
                        $"  Elevation: {topLevel.Elevation:F6} ft");
                }
                else
                {
                    sb.AppendLine(
                        "  Not available.");
                }

                sb.AppendLine();

                if (!double.IsNaN(levelElevationDifference))
                {
                    sb.AppendLine(
                        "Level Elevation Difference:");

                    sb.AppendLine(
                        $"  {levelElevationDifference:F6} ft");
                }

                sb.AppendLine();

                //=====================================================
                // Location
                //=====================================================

                sb.AppendLine(
                    "4. ACTUAL LOCATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Runtime Type : {locationType}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Geometry Source : {geometrySource}");

                sb.AppendLine();

                //=====================================================
                // LocationPoint
                //=====================================================

                if (locationPoint != null)
                {
                    sb.AppendLine(
                        "5. LOCATION POINT DATA");

                    sb.AppendLine(
                        "----------------------------------------");

                    sb.AppendLine(
                        "Insertion Point:");

                    sb.AppendLine(
                        $"  ({locationPoint.Point.X:F6}, " +
                        $"{locationPoint.Point.Y:F6}, " +
                        $"{locationPoint.Point.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Rotation:");

                    sb.AppendLine(
                        $"  {locationPoint.Rotation:F6} rad");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Important:");

                    sb.AppendLine(
                        "LocationPoint does not by itself provide " +
                        "the physical member End Point or Actual Length.");

                    sb.AppendLine();
                }

                //=====================================================
                // LocationCurve
                //=====================================================

                if (locationCurve != null)
                {
                    sb.AppendLine(
                        "5. LOCATION CURVE DATA");

                    sb.AppendLine(
                        "----------------------------------------");

                    sb.AppendLine(
                        "Start Point:");

                    sb.AppendLine(
                        $"  ({startPoint.X:F6}, " +
                        $"{startPoint.Y:F6}, " +
                        $"{startPoint.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "End Point:");

                    sb.AppendLine(
                        $"  ({endPoint.X:F6}, " +
                        $"{endPoint.Y:F6}, " +
                        $"{endPoint.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Actual Curve Length:");

                    sb.AppendLine(
                        $"  {actualLength:F6} ft");

                    sb.AppendLine();

                    if (directionAvailable)
                    {
                        sb.AppendLine(
                            "3D Direction:");

                        sb.AppendLine(
                            $"  ({direction.X:F6}, " +
                            $"{direction.Y:F6}, " +
                            $"{direction.Z:F6})");
                    }

                    sb.AppendLine();
                }

                //=====================================================
                // Transform
                //=====================================================

                sb.AppendLine(
                    "6. TRANSFORM");

                sb.AppendLine(
                    "----------------------------------------");

                if (transform != null)
                {
                    sb.AppendLine(
                        "Origin:");

                    sb.AppendLine(
                        $"  ({transform.Origin.X:F6}, " +
                        $"{transform.Origin.Y:F6}, " +
                        $"{transform.Origin.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisX:");

                    sb.AppendLine(
                        $"  ({transform.BasisX.X:F6}, " +
                        $"{transform.BasisX.Y:F6}, " +
                        $"{transform.BasisX.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisY:");

                    sb.AppendLine(
                        $"  ({transform.BasisY.X:F6}, " +
                        $"{transform.BasisY.Y:F6}, " +
                        $"{transform.BasisY.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisZ:");

                    sb.AppendLine(
                        $"  ({transform.BasisZ.X:F6}, " +
                        $"{transform.BasisZ.Y:F6}, " +
                        $"{transform.BasisZ.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "Transform not available.");
                }

                sb.AppendLine();

                //=====================================================
                // Main Geometry Values
                //=====================================================

                sb.AppendLine(
                    "7. MAIN GEOMETRIC VALUES");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Start Point:");

                if (startPoint != null)
                {
                    sb.AppendLine(
                        $"  ({startPoint.X:F6}, " +
                        $"{startPoint.Y:F6}, " +
                        $"{startPoint.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "  Not directly available.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "End Point:");

                if (endPoint != null)
                {
                    sb.AppendLine(
                        $"  ({endPoint.X:F6}, " +
                        $"{endPoint.Y:F6}, " +
                        $"{endPoint.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "  Not directly available.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "3D Direction:");

                if (directionAvailable)
                {
                    sb.AppendLine(
                        $"  ({direction.X:F6}, " +
                        $"{direction.Y:F6}, " +
                        $"{direction.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "  Not directly available.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "Rotation:");

                if (rotationAvailable)
                {
                    sb.AppendLine(
                        $"  {rotation:F6} rad");
                }
                else
                {
                    sb.AppendLine(
                        "  Not directly available as LocationPoint.Rotation.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "Actual Length:");

                if (lengthAvailable)
                {
                    sb.AppendLine(
                        $"  {actualLength:F6} ft");
                }
                else
                {
                    sb.AppendLine(
                        "  Not directly available from Location.");
                }

                sb.AppendLine();

                //=====================================================
                // Level Geometry
                //=====================================================

                sb.AppendLine(
                    "8. LEVEL GEOMETRY");

                sb.AppendLine(
                    "----------------------------------------");

                if (baseLevel != null &&
                    topLevel != null)
                {
                    XYZ verticalLevelVector =
                        new XYZ(
                            0,
                            0,
                            topLevel.Elevation -
                            baseLevel.Elevation);

                    sb.AppendLine(
                        "Level-to-Level Vector:");

                    sb.AppendLine(
                        $"  ({verticalLevelVector.X:F6}, " +
                        $"{verticalLevelVector.Y:F6}, " +
                        $"{verticalLevelVector.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Important:");

                    sb.AppendLine(
                        "This represents the difference between the " +
                        "two Level elevations.");

                    sb.AppendLine(
                        "It is NOT automatically the physical Family " +
                        "member direction if the member is slanted.");
                }
                else
                {
                    sb.AppendLine(
                        "Base/Top Level geometry could not be determined.");
                }

                sb.AppendLine();

                //=====================================================
                // Final Strategy
                //=====================================================

                sb.AppendLine(
                    "9. TWO-LEVEL STRATEGY");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "TwoLevelsBased");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "Base Level + Top Level");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "Inspect Actual Location");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "LocationPoint / LocationCurve");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "Use Native Geometry When Available");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "Derive Missing Values Only When Justified");

                sb.AppendLine();

                sb.AppendLine(
                    "Do NOT assume that Level-to-Level vector is " +
                    "automatically the Family's physical direction.");

                #endregion

                //=====================================================
                // 10. Display
                //=====================================================

                TaskDialog.Show("Two-Level Family Geometry", sb.ToString());

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
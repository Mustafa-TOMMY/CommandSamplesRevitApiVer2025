using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // LocationCurve Family Geometry Command
    //
    // Part B - Command 03
    //
    // Purpose:
    //
    // Analyze FamilyInstances whose actual Location is represented by
    // LocationCurve.
    //
    // Primary native geometric source:
    //
    // FamilyInstance
    //       ↓
    // LocationCurve
    //       ↓
    // Curve
    //
    // From the Curve we can directly obtain:
    //
    // - Start Point
    // - End Point
    // - Actual Length
    // - 3D Direction
    //
    // Direction:
    //
    // End Point - Start Point
    // -----------------------
    //          Length
    //
    // Rotation:
    //
    // LocationCurve does not provide a LocationPoint.Rotation equivalent.
    // Therefore, this command does NOT invent a rotation angle.
    //
    // For full 3D orientation, the FamilyInstance Transform can be inspected:
    //
    // - BasisX
    // - BasisY
    // - BasisZ
    //
    // Important:
    //
    // This command does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class LocationCurveFamilyGeometryCommand : IExternalCommand
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
                        "Select a LocationCurve FamilyInstance");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Actual Location
                //=====================================================

                Location location = familyInstance.Location;

                if (location == null)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "The FamilyInstance does not have a Location.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Verify Runtime Location Type
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                if (locationCurve == null)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "The selected FamilyInstance does not have a LocationCurve.");

                    return Result.Failed;
                }

                //=====================================================
                // 4. Get Curve
                //=====================================================

                Curve curve = locationCurve.Curve;

                if (curve == null)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "LocationCurve does not contain a valid Curve.");

                    return Result.Failed;
                }

                //=====================================================
                // 5. Get Start / End Points
                //=====================================================

                XYZ startPoint = curve.GetEndPoint(0);

                XYZ endPoint = curve.GetEndPoint(1);

                //=====================================================
                // 6. Get Actual Length
                //=====================================================

                double actualLength = curve.Length;

                if (actualLength <= 1e-9)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "The Curve has zero or near-zero length.");

                    return Result.Failed;
                }

                //=====================================================
                // 7. Calculate 3D Direction
                // Direction = End - Start
                // Then normalize it.
                //=====================================================

                XYZ directionVector = endPoint - startPoint;

                double directionLength = directionVector.GetLength();

                if (directionLength <= 1e-9)
                {
                    TaskDialog.Show(
                        "LocationCurve Family",
                        "Could not calculate a valid direction vector.");

                    return Result.Failed;
                }

                XYZ direction = directionVector.Normalize();

                //=====================================================
                // 8. Verify Length Using Point Distance
                //
                // For a straight Line:
                //
                // Distance(Start, End) = Curve.Length
                //
                // For a general curved Curve:
                //
                // Distance(Start, End) may be different from
                // Curve.Length.
                //=====================================================

                double endpointDistance = startPoint.DistanceTo(endPoint);

                //=====================================================
                // 9. Detect Whether Curve Is a Line
                //=====================================================

                Line line = curve as Line;

                bool isStraightLine = line != null;

                //=====================================================
                // 10. Get Transform
                // Used for full orientation analysis.
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
                // 11. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb =
                            new StringBuilder();

                sb.AppendLine(
                    "LOCATIONCURVE FAMILY GEOMETRY");

                sb.AppendLine(
                    "========================================");

                //=====================================================
                // Family Information
                //=====================================================

                sb.AppendLine(
                    "1. FAMILY INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Element Id      : {familyInstance.Id}");

                sb.AppendLine(
                    $"Family Name     : " +
                    $"{familyInstance.Symbol?.Family?.Name ?? "Unknown"}");

                sb.AppendLine(
                    $"Symbol / Type   : " +
                    $"{familyInstance.Symbol?.Name ?? "Unknown"}");

                sb.AppendLine();

                //=====================================================
                // Location Information
                //=====================================================

                sb.AppendLine(
                    "2. LOCATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Runtime Type    : {location.GetType().Name}");

                sb.AppendLine(
                    "LocationCurve   : Available");

                sb.AppendLine();

                //=====================================================
                // Curve Information
                //=====================================================

                sb.AppendLine(
                    "3. CURVE INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Curve Type      : {curve.GetType().Name}");

                sb.AppendLine(
                    $"Is Straight Line: {isStraightLine}");

                sb.AppendLine();

                //=====================================================
                // Start Point
                //=====================================================

                sb.AppendLine(
                    "4. START POINT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({startPoint.X:F6}, " +
                    $"{startPoint.Y:F6}, " +
                    $"{startPoint.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // End Point
                //=====================================================

                sb.AppendLine(
                    "5. END POINT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({endPoint.X:F6}, " +
                    $"{endPoint.Y:F6}, " +
                    $"{endPoint.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Direction Vector
                //=====================================================

                sb.AppendLine(
                    "6. 3D DIRECTION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Raw Vector:");

                sb.AppendLine(
                    $"End - Start = " +
                    $"({directionVector.X:F6}, " +
                    $"{directionVector.Y:F6}, " +
                    $"{directionVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "Normalized Direction:");

                sb.AppendLine(
                    $"({direction.X:F6}, " +
                    $"{direction.Y:F6}, " +
                    $"{direction.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Length
                //=====================================================

                sb.AppendLine(
                    "7. ACTUAL LENGTH");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Curve.Length:");

                sb.AppendLine(
                    $"  {actualLength:F6} ft");

                sb.AppendLine();

                sb.AppendLine(
                    $"Endpoint Distance:");

                sb.AppendLine(
                    $"  {endpointDistance:F6} ft");

                sb.AppendLine();

                if (isStraightLine)
                {
                    sb.AppendLine(
                        "For this straight Line:");

                    sb.AppendLine(
                        "Curve.Length ≈ Distance(Start, End)");
                }
                else
                {
                    sb.AppendLine(
                        "This is not a straight Line.");

                    sb.AppendLine(
                        "Curve.Length represents the actual curve path.");

                    sb.AppendLine(
                        "Distance(Start, End) represents the chord.");

                    sb.AppendLine(
                        "They may therefore be different.");
                }

                sb.AppendLine();

                //=====================================================
                // Rotation
                //=====================================================

                sb.AppendLine(
                    "8. ROTATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "LocationCurve does not expose a " +
                    "LocationPoint.Rotation property.");

                sb.AppendLine();

                sb.AppendLine(
                    "Therefore:");

                sb.AppendLine(
                    "No universal scalar rotation angle is " +
                    "calculated from Start/End alone.");

                sb.AppendLine();

                sb.AppendLine(
                    "For full 3D orientation, inspect the " +
                    "FamilyInstance Transform.");

                //=====================================================
                // Transform
                //=====================================================

                if (transform != null)
                {
                    sb.AppendLine();

                    sb.AppendLine(
                        "9. FAMILY TRANSFORM");

                    sb.AppendLine(
                        "----------------------------------------");

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

                //=====================================================
                // Final Strategy
                //=====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "10. GEOMETRY STRATEGY");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Primary Source:");

                sb.AppendLine(
                    "LocationCurve → Curve");

                sb.AppendLine();

                sb.AppendLine(
                    "Start Point:");

                sb.AppendLine(
                    "Curve.GetEndPoint(0)");

                sb.AppendLine();

                sb.AppendLine(
                    "End Point:");

                sb.AppendLine(
                    "Curve.GetEndPoint(1)");

                sb.AppendLine();

                sb.AppendLine(
                    "Actual Length:");

                sb.AppendLine(
                    "Curve.Length");

                sb.AppendLine();

                sb.AppendLine(
                    "3D Direction:");

                sb.AppendLine(
                    "(End - Start).Normalize()");

                sb.AppendLine();

                sb.AppendLine(
                    "Rotation:");

                sb.AppendLine(
                    "Requires orientation analysis; " +
                    "do not assume a LocationPoint-style scalar.");

                #endregion

                //=====================================================
                // 12. Display
                //=====================================================

                TaskDialog.Show(
                    "LocationCurve Family Geometry",
                    sb.ToString());

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
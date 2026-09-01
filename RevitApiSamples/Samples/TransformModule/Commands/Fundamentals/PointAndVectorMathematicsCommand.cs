using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.Fundamentals
{
    // ============================================================================
    // Location Geometry Analysis Command
    //
    // Command 03
    //
    // Purpose:
    //
    // Select a real Revit Element and analyze its Location.
    //
    // The command handles the two main Location representations:
    //
    // LocationPoint
    //      ↓
    //      Point
    //      Rotation
    //
    // LocationCurve
    //      ↓
    //      Start Point
    //      End Point
    //      Direction
    //      Actual Length
    //
    // Important:
    //
    // Some values are provided directly by Revit.
    // Some values must be calculated from other geometric data.
    //
    // This command explicitly identifies which is which.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class LocationGeometryAnalysisCommand : IExternalCommand
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
                // 1. Select Element
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select an element to analyze its Location");

                Element element = doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Location Analysis",
                        "Could not find the selected element.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Location
                //=====================================================

                Location location = element.Location;

                if (location == null)
                {
                    TaskDialog.Show(
                        "Location Analysis",
                        "The selected element does not have a Location.");

                    return Result.Failed;
                }

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("LOCATION GEOMETRY ANALYSIS");
                sb.AppendLine("========================================");
                sb.AppendLine($"Element Id : {element.Id}");
                sb.AppendLine($"Category   : {element.Category?.Name ?? "None"}");
                sb.AppendLine($"Location   : {location.GetType().Name}");

                sb.AppendLine();

                //=====================================================
                // 3. LocationPoint
                //=====================================================

                LocationPoint locationPoint = location as LocationPoint;

                if (locationPoint != null)
                {
                    XYZ point = locationPoint.Point;

                    double rotation = locationPoint.Rotation;

                    sb.AppendLine("LOCATION POINT");
                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine($"Start Point : N/A");

                    sb.AppendLine($"End Point   : N/A");

                    sb.AppendLine();

                    sb.AppendLine("Point:");
                    sb.AppendLine($"  ({point.X:F4}, " +
                        $"{point.Y:F4}, " +
                        $"{point.Z:F4})");

                    sb.AppendLine("  Source: Revit LocationPoint.Point");

                    sb.AppendLine();

                    sb.AppendLine("Rotation:");

                    sb.AppendLine($"  Radians : {rotation:F6}");

                    sb.AppendLine($"  Degrees : " +
                        $"{rotation * 180.0 / Math.PI:F2}");

                    sb.AppendLine("  Source: Revit LocationPoint.Rotation");

                    sb.AppendLine();

                    sb.AppendLine("Direction:");

                    sb.AppendLine("  Not directly represented by LocationPoint.");

                    sb.AppendLine();

                    sb.AppendLine("Actual Length:");

                    sb.AppendLine("  N/A for LocationPoint.");

                    sb.AppendLine();

                    sb.AppendLine("SUMMARY:");

                    sb.AppendLine("  Point    → Revit");
                    sb.AppendLine("  Rotation → Revit");
                    sb.AppendLine("  Direction → Not directly provided here");
                    sb.AppendLine("  Length   → Not applicable");
                }

                //=====================================================
                // 4. LocationCurve
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                if (locationCurve != null)
                {
                    Curve curve = locationCurve.Curve;

                    XYZ startPoint = curve.GetEndPoint(0);

                    XYZ endPoint = curve.GetEndPoint(1);

                    // Direction is calculated from the two points.
                    XYZ vector = endPoint - startPoint;

                    XYZ direction = vector.Normalize();

                    // Actual geometric distance.
                    double actualLength = curve.Length;

                    sb.AppendLine("LOCATION CURVE");
                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine("Start Point:");

                    sb.AppendLine($"  ({startPoint.X:F4}, " +
                        $"{startPoint.Y:F4}, " +
                        $"{startPoint.Z:F4})");

                    sb.AppendLine("  Source: Revit Curve.GetEndPoint(0)");

                    sb.AppendLine();

                    sb.AppendLine("End Point:");

                    sb.AppendLine($"  ({endPoint.X:F4}, " +
                        $"{endPoint.Y:F4}, " +
                        $"{endPoint.Z:F4})");

                    sb.AppendLine("  Source: Revit Curve.GetEndPoint(1)");

                    sb.AppendLine();

                    sb.AppendLine("Direction:");

                    sb.AppendLine(
                        $"  ({direction.X:F4}, " +
                        $"{direction.Y:F4}, " +
                        $"{direction.Z:F4})");

                    sb.AppendLine("  Source: Calculated from End - Start");

                    sb.AppendLine($"  Vector Length: {direction.GetLength():F4}");

                    sb.AppendLine();

                    sb.AppendLine("Actual Length:");

                    sb.AppendLine($"  {actualLength:F4} ft");

                    sb.AppendLine("  Source: Revit Curve.Length");

                    sb.AppendLine();

                    sb.AppendLine("Rotation:");

                    sb.AppendLine("  Not directly represented by LocationCurve " +
                        "as LocationPoint.Rotation.");

                    sb.AppendLine();

                    sb.AppendLine("SUMMARY:");

                    sb.AppendLine("  Start Point → Revit");
                    sb.AppendLine("  End Point   → Revit");
                    sb.AppendLine("  Direction   → Calculated");
                    sb.AppendLine("  Length      → Revit");
                    sb.AppendLine("  Rotation    → Not directly available here");
                }

                //=====================================================
                // 5. Unknown Location Type
                //=====================================================

                if (locationPoint == null && locationCurve == null)
                {
                    sb.AppendLine();
                    sb.AppendLine("OTHER LOCATION TYPE");
                    sb.AppendLine("----------------------------------------");
                    sb.AppendLine($"Runtime Type: {location.GetType().FullName}");
                    sb.AppendLine();
                    sb.AppendLine("This command currently handles " +
                        "LocationPoint and LocationCurve.");
                }

                //=====================================================
                // 6. Display
                //=====================================================

                TaskDialog.Show("Location Geometry Analysis", sb.ToString());

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
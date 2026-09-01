using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.Fundamentals
{
    // ============================================================================
    // Location Point vs Location Curve Command
    //
    // This command demonstrates the two common Location representations:
    //
    // Element
    //    ↓
    // Location
    //    ├── LocationPoint
    //    │      ├── Point
    //    │      └── Rotation
    //    │
    //    └── LocationCurve
    //           └── Curve
    //
    // Important Concept:
    //
    // Not every Element has the same type of Location.
    //
    // Point-based elements:
    //     LocationPoint
    //
    // Curve-based elements:
    //     LocationCurve
    //
    // The Location representation depends on the nature of the element.
    //
    // This command only INSPECTS the Location.
    // It does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class LocationPointVsLocationCurveCommand : IExternalCommand
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
                // 1. Select an Element
                //=====================================================

                Reference reference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select an element to inspect its Location");

                Element element = doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Location",
                        "Could not find the selected element.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Element Location
                //=====================================================

                Location location = element.Location;

                if (location == null)
                {
                    TaskDialog.Show(
                        "Location",
                        "The selected element does not expose a Location.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Build Basic Information
                //=====================================================

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("LOCATION INSPECTION");

                sb.AppendLine("========================================");

                sb.AppendLine($"Element Id   : {element.Id}");

                sb.AppendLine($"Category     : {element.Category?.Name ?? "None"}");

                sb.AppendLine($"Location API : {location.GetType().Name}");

                sb.AppendLine();

                //=====================================================
                // 4. LocationPoint
                //=====================================================

                LocationPoint locationPoint = location as LocationPoint;

                if (locationPoint != null)
                {
                    XYZ point = locationPoint.Point;

                    double rotation = locationPoint.Rotation;

                    sb.AppendLine("LOCATION POINT");

                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine($"Point X : {point.X:F4}");

                    sb.AppendLine($"Point Y : {point.Y:F4}");

                    sb.AppendLine($"Point Z : {point.Z:F4}");

                    sb.AppendLine();

                    sb.AppendLine($"Rotation (radians) : {rotation:F4}");

                    sb.AppendLine($"Rotation (degrees) : " + $"{rotation * 180.0 / Math.PI:F2}");

                    sb.AppendLine();

                    sb.AppendLine("Interpretation:");

                    sb.AppendLine("This element exposes its location " +
                        "primarily through a single XYZ point " +
                        "and a rotation.");
                }

                //=====================================================
                // 5. LocationCurve
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                if (locationCurve != null)
                {
                    Curve curve = locationCurve.Curve;

                    XYZ startPoint = curve.GetEndPoint(0);

                    XYZ endPoint = curve.GetEndPoint(1);

                    XYZ direction = (endPoint - startPoint).Normalize();

                    double length = curve.Length;

                    sb.AppendLine("LOCATION CURVE");

                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine($"Curve Type : {curve.GetType().Name}");

                    sb.AppendLine();

                    sb.AppendLine("Start Point:");

                    sb.AppendLine($"({startPoint.X:F4}, " +
                        $"{startPoint.Y:F4}, " +
                        $"{startPoint.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine("End Point:");

                    sb.AppendLine($"({endPoint.X:F4}, " +
                        $"{endPoint.Y:F4}, " +
                        $"{endPoint.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine("Direction:");

                    sb.AppendLine($"({direction.X:F4}, " +
                        $"{direction.Y:F4}, " +
                        $"{direction.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine($"Length : {length:F4} ft");

                    sb.AppendLine();

                    sb.AppendLine("Interpretation:");

                    sb.AppendLine("This element exposes its location " +
                        "through a Curve rather than a single point.");
                }

                //=====================================================
                // 6. Unknown / Other Location Type
                //=====================================================

                if (locationPoint == null && locationCurve == null)
                {
                    sb.AppendLine("OTHER LOCATION TYPE");

                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine($"Runtime Type : {location.GetType().FullName}");

                    sb.AppendLine();

                    sb.AppendLine("This Location type is not handled by " +
                        "this fundamental sample.");
                }

                //=====================================================
                // 7. Display Result
                //=====================================================

                TaskDialog.Show("Location Point vs Location Curve",
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
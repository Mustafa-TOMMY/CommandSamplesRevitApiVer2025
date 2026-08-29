using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Get Point On Curve Command
    //
    // This command demonstrates how to get points along the curve of a
    // Curve-Based FamilyInstance (e.g., Beam, Inclined Framing, Pipe).
    //
    // Workflow:
    //
    // CurveBased FamilyInstance
    //       ↓
    // element.Location as LocationCurve
    //       ↓
    // LocationCurve.Curve
    //       ↓
    // curve.GetEndPoint(0)         ← Start Point (t = 0.0)
    // curve.Evaluate(0.5, true)    ← Midpoint    (t = 0.5, normalized)
    // curve.GetEndPoint(1)         ← End Point   (t = 1.0)
    //
    // Key concept:
    //
    // curve.Evaluate(t, normalized)
    //   - t = 0.0  → Start of curve
    //   - t = 0.5  → Midpoint of curve
    //   - t = 1.0  → End of curve
    //   - normalized = true → t is a proportion (0.0 to 1.0)
    //   - normalized = false → t is an actual arc-length parameter
    //
    // The direction vector dZ component reveals the elevation change:
    //   - dZ = 0   → Horizontal element
    //   - dZ > 0   → Element rises from Start to End
    //   - dZ < 0   → Element descends from Start to End
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 07
    public class GetPointOnCurveCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select a Curve-Based FamilyInstance

                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Curve-Based FamilyInstance (e.g., Beam, Inclined Column, Pipe)");

                Element element = doc.GetElement(selRef);

                //=====================================================
                // 2. Validate: Element must have a LocationCurve

                LocationCurve locCurve = element.Location as LocationCurve;

                if (locCurve == null)
                {
                    TaskDialog.Show(
                        "Get Point On Curve",
                        "Selected element does not have a LocationCurve.\n\n" +
                        "Please select a Curve-Based element such as:\n" +
                        "  • Structural Beams / Framing\n" +
                        "  • Walls\n" +
                        "  • MEP Ducts, Pipes, Conduits\n" +
                        "  • CurveBased FamilyInstances");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Access the Underlying Curve

                Curve curve = locCurve.Curve;

                //=====================================================
                // 4. Get Key Points Using GetEndPoint() and Evaluate()

                // GetEndPoint(0) and GetEndPoint(1) are the definitive
                // start and end — equivalent to Evaluate(0.0) and Evaluate(1.0).
                XYZ startPoint  = curve.GetEndPoint(0);
                XYZ endPoint    = curve.GetEndPoint(1);

                // Evaluate(t, normalized: true) gives a point at a normalized
                // parameter t along the curve (0.0 = start, 1.0 = end).
                XYZ quarterPoint = curve.Evaluate(0.25, true);
                XYZ midPoint     = curve.Evaluate(0.50, true);
                XYZ threeQuarterPoint = curve.Evaluate(0.75, true);

                //=====================================================
                // 5. Compute Direction and Length

                double length = curve.Length;

                // Direction vector from start to end (normalized = unit vector)
                XYZ direction = (endPoint - startPoint).Normalize();

                // dZ of the direction vector reveals inclination in the Z axis
                // dZ = 0   → perfectly horizontal
                // dZ = 1   → perfectly vertical (pointing straight up)
                // dZ = -1  → perfectly vertical (pointing straight down)
                double elevationAngleDeg = Math.Asin(
                    Math.Max(-1.0, Math.Min(1.0, direction.Z))) * (180.0 / Math.PI);

                double deltaZ = endPoint.Z - startPoint.Z;

                string inclinationLabel;
                if (Math.Abs(direction.Z) < 0.01)
                    inclinationLabel = "Horizontal";
                else if (Math.Abs(direction.Z) > 0.99)
                    inclinationLabel = "Vertical";
                else
                    inclinationLabel = $"Inclined at {elevationAngleDeg:F2}°";

                //=====================================================
                // 6. Build Report

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("GET POINT ON CURVE");
                sb.AppendLine("========================================");
                sb.AppendLine($"Element      : {element.Name}  (ID: {element.Id})");
                sb.AppendLine($"Curve Type   : {curve.GetType().Name}");
                sb.AppendLine($"Length       : {length:F4} ft  ({length * 0.3048:F3} m)");
                sb.AppendLine();

                sb.AppendLine("POINTS ALONG CURVE  [curve.Evaluate(t, normalized: true)]");
                sb.AppendLine("========================================");
                sb.AppendLine($"t = 0.00  Start           : {XYZToString(startPoint)}");
                sb.AppendLine($"t = 0.25  Quarter Point   : {XYZToString(quarterPoint)}");
                sb.AppendLine($"t = 0.50  Midpoint        : {XYZToString(midPoint)}");
                sb.AppendLine($"t = 0.75  Three-Quarters  : {XYZToString(threeQuarterPoint)}");
                sb.AppendLine($"t = 1.00  End             : {XYZToString(endPoint)}");
                sb.AppendLine();

                sb.AppendLine("DIRECTION VECTOR  (Start → End, normalized)");
                sb.AppendLine("========================================");
                sb.AppendLine($"Direction    : {XYZToString(direction)}");
                sb.AppendLine($"  dX         : {direction.X:F4}   (East-West component)");
                sb.AppendLine($"  dY         : {direction.Y:F4}   (North-South component)");
                sb.AppendLine($"  dZ         : {direction.Z:F4}   ← Elevation / inclination component");
                sb.AppendLine();

                sb.AppendLine("ELEVATION ANALYSIS");
                sb.AppendLine("========================================");
                sb.AppendLine($"Delta Z (Rise): {deltaZ:F4} ft  ({deltaZ * 0.3048:F4} m)");
                sb.AppendLine($"Elevation Angle: {elevationAngleDeg:F2}°  (0°=horizontal, 90°=vertical)");
                sb.AppendLine($"Element Status : {inclinationLabel}");

                TaskDialog.Show("Get Point On Curve", sb.ToString());

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

        //=====================================================

        private string XYZToString(XYZ pt) =>
            $"({pt.X:F4}, {pt.Y:F4}, {pt.Z:F4})";
    }
}

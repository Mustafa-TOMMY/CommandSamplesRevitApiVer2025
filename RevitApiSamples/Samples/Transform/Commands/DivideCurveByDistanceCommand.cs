using System;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Divide Curve By Distance Command (Command 09)
    //
    // This command demonstrates how to sample points along any curve at custom
    // fixed distance intervals (e.g., every 3 ft on a 12 ft beam) or custom offsets.
    //
    // Core Formula:
    //   Normalized Parameter = DistanceAlongCurve / TotalLength
    //   XYZ Point = curve.Evaluate(normalizedParam, normalized: true);
    //
    // Examples:
    //   For a 12 ft curve with 3 ft step:
    //     - Distance  0 ft  →  t = 0 / 12  = 0.00  (Start Point)
    //     - Distance  3 ft  →  t = 3 / 12  = 0.25  (Quarter Point)
    //     - Distance  6 ft  →  t = 6 / 12  = 0.50  (Midpoint)
    //     - Distance  9 ft  →  t = 9 / 12  = 0.75  (Three-Quarter Point)
    //     - Distance 12 ft  →  t = 12 / 12 = 1.00  (End Point)
    //
    // True vs. False:
    //   - Evaluate(t, true)  : 't' is proportional (0.0 to 1.0). Use this for
    //                          percentages, equal divisions, or distance/length ratios.
    //   - Evaluate(t, false) : 't' is raw parameter [t_start, t_end]. Use this when
    //                          working with IntersectionResult or Project() results.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class DivideCurveByDistanceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Select a Curve-Based Element (e.g., Beam, Wall, Pipe, Duct)
                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Curve-Based element (Beam, Wall, Pipe) to divide by distance");

                Element element = doc.GetElement(selRef);
                if (element.Location is not LocationCurve locCurve)
                {
                    TaskDialog.Show(
                        "Divide Curve By Distance",
                        "The selected element does not have a LocationCurve.\n" +
                        "Please select a Beam, Wall, Duct, Pipe, or CurveBased Family.");
                    return Result.Failed;
                }

                Curve curve = locCurve.Curve;
                double totalLength = curve.Length; // Length in internal units (feet)

                // 2. Define custom interval step (e.g. 3.0 feet or 1/4th of length)
                // If the element is short (< 3 ft), divide into 4 equal segments instead.
                double stepDistance = 3.0; // 3 feet default
                if (totalLength < 3.0)
                {
                    stepDistance = totalLength / 4.0;
                }

                // 3. Raw Parameter Bounds vs Normalized Bounds
                double rawStartParam = curve.GetEndParameter(0);
                double rawEndParam = curve.GetEndParameter(1);

                // Midpoint evaluated both ways:
                XYZ midPointNormalized = curve.Evaluate(0.5, normalized: true);
                double rawMidParam = (rawStartParam + rawEndParam) / 2.0;
                XYZ midPointRaw = curve.Evaluate(rawMidParam, normalized: false);

                // 4. Sample points at each interval along the curve
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DIVIDE CURVE BY DISTANCE (Custom Intervals)");
                sb.AppendLine("==================================================");
                sb.AppendLine($"Element      : {element.Name} (ID: {element.Id})");
                sb.AppendLine($"Curve Type   : {curve.GetType().Name}");
                sb.AppendLine($"Total Length : {totalLength:F4} ft ({totalLength * 0.3048:F3} m)");
                sb.AppendLine($"Step Interval: {stepDistance:F2} ft ({stepDistance * 0.3048:F3} m)");
                sb.AppendLine();

                sb.AppendLine("MIDPOINT EVALUATION (True vs. False)");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine($"1) Normalized [Evaluate(0.5, true)] :");
                sb.AppendLine($"   {XYZToString(midPointNormalized)}");
                sb.AppendLine($"2) Raw Param  [Evaluate({rawMidParam:F3}, false)] :");
                sb.AppendLine($"   {XYZToString(midPointRaw)}");
                sb.AppendLine($"   (Both yield the exact same 3D point!)");
                sb.AppendLine();

                sb.AppendLine("SAMPLED POINTS ALONG CURVE");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("Dist (ft) | t (norm) | 3D Point (X, Y, Z) | Tangent Dir");
                sb.AppendLine("--------------------------------------------------");

                int pointIndex = 0;
                for (double currentDist = 0.0; currentDist <= totalLength + 1e-6; currentDist += stepDistance)
                {
                    // Guard against small floating point overshoot at the end
                    double clampedDist = Math.Min(currentDist, totalLength);
                    double tNormalized = clampedDist / totalLength;

                    // Ensure t is strictly in [0.0, 1.0]
                    tNormalized = Math.Clamp(tNormalized, 0.0, 1.0);

                    // Evaluate point at normalized parameter
                    XYZ pt = curve.Evaluate(tNormalized, normalized: true);

                    // Compute local coordinate frame and tangent vector using ComputeDerivatives
                    Autodesk.Revit.DB.Transform derivatives = curve.ComputeDerivatives(tNormalized, normalized: true);
                    XYZ tangent = derivatives.BasisX.Normalize();

                    string label = (clampedDist == 0.0) ? " [START]" :
                                   (Math.Abs(tNormalized - 0.5) < 1e-4) ? " [MID]" :
                                   (clampedDist >= totalLength - 1e-6) ? " [END]" : "";

                    sb.AppendLine($"#{pointIndex:D2} @ {clampedDist:F2}' | t={tNormalized:F2} | {XYZToString(pt)}{label}");
                    sb.AppendLine($"     Tangent: ({tangent.X:F3}, {tangent.Y:F3}, {tangent.Z:F3})");

                    pointIndex++;
                }

                sb.AppendLine();
                sb.AppendLine("KEY TAKEAWAY");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("• Use 'true'  when specifying a proportion (0.0 to 1.0) or Distance / Length.");
                sb.AppendLine("• Use 'false' when evaluating a raw geometric parameter [t_start, t_end].");

                TaskDialog.Show("Divide Curve By Distance", sb.ToString());

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

        private string XYZToString(XYZ pt) =>
            $"({pt.X:F3}, {pt.Y:F3}, {pt.Z:F3})";
    }
}

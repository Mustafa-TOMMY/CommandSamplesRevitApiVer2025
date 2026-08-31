using System;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Calculate Direction And End Point Command (Command 11)
    //
    // Demonstrates how Family Creation & Placement Architecture governs the
    // retrieval and calculation of 3D direction and end points across different
    // Revit element categories and hosting types.
    //
    // The 4 Core Methods:
    //
    // Method 01: HandOrientation & 3D Transform Matrix Basis
    //       └─ Direct 3D vectors from Revit (Loadable Family: Doors, Equipment, Face-Hosted)
    //
    // Method 02: (End - Start).Normalize() from LocationCurve
    //       └─ Direct 3D curve tangent (Linear System Families: Walls, Pipes, Ducts, Beams)
    //
    // Method 03: LocationPoint.Rotation (2D Plan Polar Fallback)
    //       └─ Scalar angle θ around vertical axis: (cos θ, sin θ, 0)
    //       └─ Limitation: Forces Z = 0; cannot represent 3D pitch/slope.
    //
    // Method 04: Parameterized 3D Elevation & Slope Reconstruction
    //       └─ For Level-Hosted Point Families with Infeed (Z1) & Outfeed (Z2) parameters
    //       └─ ΔZ = Z_out - Z_in
    //       └─ sin(α) = ΔZ / Length,  cos(α) = sqrt(1 - sin²α)
    //       └─ u_3D = (cos θ · cos α,  sin θ · cos α,  sin α)
    //       └─ Explains why translating origin Z causes the "Double-Elevation Defect"
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class CalculateDirectionAndEndPointCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Select an Element
                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element (FamilyInstance, Wall, Beam, Pipe, etc.) to analyze 3D Direction & End Point");

                Element element = doc.GetElement(selRef);
                if (element == null) return Result.Cancelled;

                bool isLoadableFamily = (element is FamilyInstance);
                string familyType = isLoadableFamily ? "Loadable Family" : "System Family";

                Location location = element.Location;
                bool hasLocationPoint = (location is LocationPoint);
                bool hasLocationCurve = (location is LocationCurve);

                string locationType = hasLocationPoint ? "LocationPoint" :
                                      hasLocationCurve ? "LocationCurve" : "Unknown";

                XYZ? startPoint = null;
                double rotation = 0;

                if (location is LocationPoint locPoint)
                {
                    startPoint = locPoint.Point;
                    rotation = locPoint.Rotation;
                }
                else if (location is LocationCurve locCurve)
                {
                    startPoint = locCurve.Curve.GetEndPoint(0);
                }

                if (startPoint == null)
                {
                    TaskDialog.Show("Error", "Selected element has no valid spatial location.");
                    return Result.Failed;
                }

                // 2. Calculate Direction & End Point Using the 4 Methods
                XYZ? endPoint1 = null;  // Method 01 (HandOrientation / Matrix)
                XYZ? endPoint2 = null;  // Method 02 (LocationCurve)
                XYZ? endPoint3 = null;  // Method 03 (2D Polar)
                XYZ? endPoint4 = null;  // Method 04 (Parameterized 3D Infeed/Outfeed)

                double elementLength = 10.0; // Default 10 ft

                // Method 01: HandOrientation & Transform Matrix
                if (isLoadableFamily && element is FamilyInstance familyInstance)
                {
                    elementLength = TryGetLength(familyInstance, defaultLength: 10.0);
                    endPoint1 = Method01_HandOrientation(familyInstance, startPoint, elementLength);
                }

                // Method 02: LocationCurve End - Start
                if (hasLocationCurve && location is LocationCurve lc)
                {
                    elementLength = lc.Curve.Length;
                    endPoint2 = lc.Curve.GetEndPoint(1);
                }

                // Method 03: 2D Polar Trigonometry
                if (hasLocationPoint)
                {
                    endPoint3 = Method03_RotationAndLength(startPoint, rotation, elementLength);
                }

                // Method 04: Parameterized Elevation (Infeed / Outfeed Slope)
                if (hasLocationPoint)
                {
                    double infeedZ = startPoint.Z;
                    double outfeedZ = startPoint.Z + 2.0; // Example: 2 ft elevation rise over length

                    if (element is FamilyInstance fi)
                    {
                        infeedZ = fi.LookupParameter("ILUS_Infeed_Elevation")?.AsDouble() ?? infeedZ;
                        outfeedZ = fi.LookupParameter("ILUS_Outfeed_Elevation")?.AsDouble() ?? outfeedZ;
                    }

                    endPoint4 = Method04_ParameterizedElevationWithSlope(
                        startPoint, rotation, elementLength, infeedZ, outfeedZ);
                }

                // 3. Build Detailed Report
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("=======================================================");
                sb.AppendLine("3D DIRECTION & END POINT ANALYSIS (4 METHODS)");
                sb.AppendLine("=======================================================");
                sb.AppendLine($"Element Name     : {element.Name}");
                sb.AppendLine($"Element ID       : {element.Id}");
                sb.AppendLine($"Category         : {element.Category?.Name ?? "N/A"}");
                sb.AppendLine($"Family Type      : {familyType}");
                sb.AppendLine($"Location Type    : {locationType}");
                sb.AppendLine($"Start Point      : {PointToString(startPoint)} ft");
                sb.AppendLine($"Length           : {elementLength:F3} ft ({UnitUtils.ConvertFromInternalUnits(elementLength, UnitTypeId.Meters):F2} m)");
                sb.AppendLine();

                // Section 1
                sb.AppendLine("METHOD 01: HandOrientation & Transform Matrix");
                sb.AppendLine("-------------------------------------------------------");
                sb.AppendLine("• Compatible : Loadable Families (FamilyInstance)");
                sb.AppendLine("• Advantage  : Directly reflects 3D basis vectors and UI flips");
                if (isLoadableFamily && element is FamilyInstance fiInstance)
                {
                    XYZ hand = fiInstance.HandOrientation;
                    XYZ facing = fiInstance.FacingOrientation;
                    Autodesk.Revit.DB.Transform tf = fiInstance.GetTotalTransform();
                    sb.AppendLine($"  Hand Vector   : {PointToString(hand)}");
                    sb.AppendLine($"  Facing Vector : {PointToString(facing)}");
                    sb.AppendLine($"  Transform Z   : {PointToString(tf.BasisZ)} (Up/Normal)");
                    sb.AppendLine($"  End Point     : {PointToString(endPoint1)} ft");
                }
                else
                {
                    sb.AppendLine("  ✗ N/A — Element is a System Family (No FamilyInstance transform)");
                }
                sb.AppendLine();

                // Section 2
                sb.AppendLine("METHOD 02: LocationCurve (P2 - P1).Normalize()");
                sb.AppendLine("-------------------------------------------------------");
                sb.AppendLine("• Compatible : Linear Elements (Walls, Pipes, Ducts, Beams)");
                sb.AppendLine("• Advantage  : True 3D spatial curve coordinates in world space");
                if (hasLocationCurve && location is LocationCurve lcurve)
                {
                    XYZ dir2 = (lcurve.Curve.GetEndPoint(1) - lcurve.Curve.GetEndPoint(0)).Normalize();
                    sb.AppendLine($"  3D Direction  : {PointToString(dir2)}");
                    sb.AppendLine($"  End Point     : {PointToString(endPoint2)} ft");
                }
                else
                {
                    sb.AppendLine("  ✗ N/A — Element is Point-Based (No LocationCurve)");
                }
                sb.AppendLine();

                // Section 3
                sb.AppendLine("METHOD 03: 2D Polar Trigonometry (cos θ, sin θ, 0)");
                sb.AppendLine("-------------------------------------------------------");
                sb.AppendLine("• Compatible : LocationPoint Elements");
                sb.AppendLine("• Limitation : 1D Plan rotation only — Z is hardcoded to 0");
                if (hasLocationPoint)
                {
                    double dirX = Math.Cos(rotation);
                    double dirY = Math.Sin(rotation);
                    sb.AppendLine($"  Rotation      : {rotation:F4} rad ({RadianToDegree(rotation):F1}°)");
                    sb.AppendLine($"  2D Direction  : ({dirX:F4}, {dirY:F4}, 0.0000)");
                    sb.AppendLine($"  End Point     : {PointToString(endPoint3)} ft");
                }
                else
                {
                    sb.AppendLine("  ✗ N/A — Element is Curve-Based");
                }
                sb.AppendLine();

                // Section 4
                sb.AppendLine("METHOD 04: Parameterized 3D Elevation (Infeed/Outfeed Slope)");
                sb.AppendLine("-------------------------------------------------------");
                sb.AppendLine("• Compatible : Level-Hosted Point Families with Slope Parameters");
                sb.AppendLine("• Advantage  : Reconstructs true 3D conveyor trajectory without double elevation");
                if (hasLocationPoint)
                {
                    sb.AppendLine($"  End Point     : {PointToString(endPoint4)} ft");
                    sb.AppendLine("  *Note: Rise (ΔZ) is handled via family instance parameters,");
                    sb.AppendLine("         NOT by translating the Level-hosted insertion point in Z.");
                }
                else
                {
                    sb.AppendLine("  ✗ N/A — Element is Curve-Based");
                }
                sb.AppendLine();

                TaskDialog.Show("Calculate Direction & End Point Result", sb.ToString());
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

        private XYZ Method01_HandOrientation(FamilyInstance familyInstance, XYZ startPoint, double length)
        {
            XYZ hand = familyInstance.HandOrientation;
            return startPoint + (hand * length);
        }

        private XYZ Method03_RotationAndLength(XYZ startPoint, double rotation, double length)
        {
            double dirX = Math.Cos(rotation);
            double dirY = Math.Sin(rotation);

            return new XYZ(
                startPoint.X + length * dirX,
                startPoint.Y + length * dirY,
                startPoint.Z);
        }

        private XYZ Method04_ParameterizedElevationWithSlope(
            XYZ startPoint,
            double rotation,
            double length,
            double infeedZ,
            double outfeedZ)
        {
            double deltaZ = outfeedZ - infeedZ;
            double sinAlpha = Math.Max(-1.0, Math.Min(1.0, deltaZ / length));
            double cosAlpha = Math.Sqrt(1.0 - sinAlpha * sinAlpha);

            double dirX = Math.Cos(rotation);
            double dirY = Math.Sin(rotation);

            double horizontalRun = length * cosAlpha;

            return new XYZ(
                startPoint.X + horizontalRun * dirX,
                startPoint.Y + horizontalRun * dirY,
                startPoint.Z + deltaZ);
        }

        private double TryGetLength(FamilyInstance fi, double defaultLength)
        {
            string[] paramNames = { "Length", "Conveyor_Length", "Span", "Cut Length" };
            foreach (string name in paramNames)
            {
                Parameter p = fi.LookupParameter(name);
                if (p != null && p.StorageType == StorageType.Double && p.AsDouble() > 0.001)
                    return p.AsDouble();
            }
            return defaultLength;
        }

        private string PointToString(XYZ? pt) =>
            pt != null ? $"({pt.X:F3}, {pt.Y:F3}, {pt.Z:F3})" : "N/A";

        private double RadianToDegree(double radians) =>
            radians * (180.0 / Math.PI);
    }
}
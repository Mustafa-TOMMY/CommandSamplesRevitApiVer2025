using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Point Family Start End Command
    //
    // PROBLEM:
    //
    // A PointBased FamilyInstance (OneLevelBased, TwoLevelsBased) only exposes
    // a single XYZ via LocationPoint — its base/origin. If the element is inclined
    // in 3D space, you CANNOT determine the top point or direction from
    // LocationPoint alone.
    //
    // You have:  Start Point (LocationPoint)  +  Length (parameter)
    // You need:  End Point  +  3D Direction  +  Elevation angle
    //
    // SOLUTION:
    //
    // GetTransform() gives you the element's local coordinate frame in world space:
    //
    //   Transform.Origin  ← Same as LocationPoint (base/origin in world space)
    //   Transform.BasisX  ← Local X axis (element width direction)
    //   Transform.BasisY  ← Local Y axis (element depth direction)
    //   Transform.BasisZ  ← Local Z axis = the 3D AXIS of the element
    //
    // For a plumb (vertical) column: BasisZ = (0, 0, 1) = world Z up
    // For an inclined column:        BasisZ = direction the element leans toward
    //
    // Then:
    //   EndPoint = StartPoint + Transform.BasisZ * Length
    //
    // The BasisZ.Z component tells you the elevation:
    //   BasisZ.Z = 1.0  → perfectly vertical
    //   BasisZ.Z = 0.0  → perfectly horizontal
    //   BasisZ.Z = 0.5  → inclined at ~30° from horizontal (sin 30° = 0.5)
    //
    // This command handles BOTH cases:
    //   - CurveBased (LocationCurve):  Start/End from curve directly
    //   - PointBased (LocationPoint):  Start from point, End from BasisZ + Length
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 08
    public class PointFamilyStartEndCommand : IExternalCommand
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

                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a FamilyInstance — PointBased (Column) or CurveBased (Beam)");

                Element element = doc.GetElement(selRef);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Point Family Start/End",
                        "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Identify Location Type

                LocationPoint locPoint = familyInstance.Location as LocationPoint;
                LocationCurve locCurve = familyInstance.Location as LocationCurve;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("POINT FAMILY — START / END / DIRECTION");
                sb.AppendLine("========================================");
                sb.AppendLine($"Family       : {familyInstance.Symbol?.Family?.Name ?? "N/A"}");
                sb.AppendLine($"Type         : {familyInstance.Symbol?.Name ?? "N/A"}");
                sb.AppendLine($"Placement    : {familyInstance.Symbol?.Family?.FamilyPlacementType}");
                sb.AppendLine($"Element Id   : {element.Id}");
                sb.AppendLine();

                XYZ startPoint = null;
                XYZ endPoint   = null;
                XYZ direction  = null;
                double length  = 0;

                //=====================================================
                // CASE A: CurveBased FamilyInstance (Beam, Inclined Framing)
                // Start and End are directly available from LocationCurve.

                if (locCurve != null)
                {
                    sb.AppendLine("LOCATION TYPE: LocationCurve  (CurveBased Element)");
                    sb.AppendLine("========================================");
                    sb.AppendLine("  Start and End are read directly from the curve.");
                    sb.AppendLine();

                    Curve curve = locCurve.Curve;
                    startPoint = curve.GetEndPoint(0);
                    endPoint   = curve.GetEndPoint(1);
                    length     = curve.Length;
                    direction  = (endPoint - startPoint).Normalize();

                    sb.AppendLine($"Start Point  : {XYZToString(startPoint)}");
                    sb.AppendLine($"End Point    : {XYZToString(endPoint)}");
                    sb.AppendLine($"Length       : {length:F4} ft  ({length * 0.3048:F4} m)");
                }

                //=====================================================
                // CASE B: PointBased FamilyInstance (Column, Inclined Element)
                // Only LocationPoint is available. We derive the End Point
                // using GetTransform().BasisZ (the element's local axis) + Length.

                else if (locPoint != null)
                {
                    sb.AppendLine("LOCATION TYPE: LocationPoint  (PointBased Element)");
                    sb.AppendLine("========================================");
                    sb.AppendLine("  Only the base point is directly available.");
                    sb.AppendLine("  Using GetTransform().BasisZ to derive 3D direction.");
                    sb.AppendLine();

                    startPoint = locPoint.Point;

                    //=====================================================
                    // GetTransform() reveals the full local coordinate frame.
                    //
                    // BasisZ = the element's local "up" / axis direction in world space.
                    //
                    // For a plumb column:    BasisZ = XYZ(0, 0, 1)  (world Z)
                    // For an inclined column: BasisZ tilts in world space, Z < 1.

                    Autodesk.Revit.DB.Transform tfm = familyInstance.GetTransform();

                    XYZ basisX = tfm.BasisX; // Local width direction
                    XYZ basisY = tfm.BasisY; // Local depth direction
                    XYZ basisZ = tfm.BasisZ; // Local axis = inclination direction

                    sb.AppendLine("GetTransform() — Local Coordinate Frame:");
                    sb.AppendLine($"  Origin  (= StartPoint) : {XYZToString(tfm.Origin)}");
                    sb.AppendLine($"  BasisX  (width dir)    : {XYZToString(basisX)}");
                    sb.AppendLine($"  BasisY  (depth dir)    : {XYZToString(basisY)}");
                    sb.AppendLine($"  BasisZ  (axis dir)     : {XYZToString(basisZ)}  ← KEY");
                    sb.AppendLine();

                    // Try to resolve the length from common parameters
                    length = TryGetLength(familyInstance);
                    direction = basisZ; // BasisZ IS the normalized direction

                    sb.AppendLine($"Start Point (Base)   : {XYZToString(startPoint)}");

                    if (length > 0)
                    {
                        // Compute EndPoint = StartPoint + BasisZ * Length
                        endPoint = startPoint + basisZ * length;
                        sb.AppendLine($"Length (from param)  : {length:F4} ft  ({length * 0.3048:F4} m)");
                        sb.AppendLine($"End Point (Top)      : {XYZToString(endPoint)}");
                        sb.AppendLine($"  Formula: Start + BasisZ * Length");
                    }
                    else
                    {
                        sb.AppendLine("Length Parameter     : Not found on this element.");
                        sb.AppendLine("  → End Point requires a length parameter.");
                        sb.AppendLine("  → Direction only: use BasisZ as the 3D axis.");
                    }
                }
                else
                {
                    sb.AppendLine("Location Type: Unknown — no LocationPoint or LocationCurve.");
                    TaskDialog.Show("Point Family Start/End", sb.ToString());
                    return Result.Succeeded;
                }

                //=====================================================
                // 3. 3D Direction Analysis

                if (direction != null)
                {
                    double elevAngleDeg = Math.Asin(
                        Math.Max(-1.0, Math.Min(1.0, direction.Z))) * (180.0 / Math.PI);

                    double xyAngleDeg = Math.Atan2(direction.Y, direction.X) * (180.0 / Math.PI);

                    double deltaZ = (endPoint != null)
                        ? endPoint.Z - startPoint.Z
                        : direction.Z * length;

                    string inclinationType;
                    if (Math.Abs(direction.Z) < 0.01)
                        inclinationType = "Horizontal  (dZ ≈ 0 — no elevation change)";
                    else if (Math.Abs(direction.Z) > 0.99)
                        inclinationType = "Vertical  (dZ ≈ ±1 — purely vertical)";
                    else
                        inclinationType = $"Inclined in 3D  (elevation angle: {elevAngleDeg:F2}°)";

                    sb.AppendLine();
                    sb.AppendLine("3D DIRECTION ANALYSIS");
                    sb.AppendLine("========================================");
                    sb.AppendLine($"Direction Vector     : {XYZToString(direction)}");
                    sb.AppendLine($"  dX                 : {direction.X:F4}   (East-West)");
                    sb.AppendLine($"  dY                 : {direction.Y:F4}   (North-South)");
                    sb.AppendLine($"  dZ                 : {direction.Z:F4}   ← Elevation component");
                    sb.AppendLine();
                    sb.AppendLine($"XY Plan Angle        : {xyAngleDeg:F2}°  (rotation in plan)");
                    sb.AppendLine($"Elevation Angle      : {elevAngleDeg:F2}°  (0°=horiz, 90°=vert)");
                    sb.AppendLine($"Delta Z (Rise)       : {deltaZ:F4} ft  ({deltaZ * 0.3048:F4} m)");
                    sb.AppendLine($"Element Type         : {inclinationType}");
                }

                TaskDialog.Show("Point Family — Start / End / Direction", sb.ToString());

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
        // Try to resolve the element length from common built-in
        // and named parameters.

        private double TryGetLength(FamilyInstance fi)
        {
            // Try Built-in parameters — confirmed valid in Revit 2025 API
            BuiltInParameter[] candidates =
            {
                BuiltInParameter.INSTANCE_LENGTH_PARAM,
                BuiltInParameter.FAMILY_HEIGHT_PARAM,
                BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM
            };

            foreach (BuiltInParameter bip in candidates)
            {
                try
                {
                    Parameter p = fi.get_Parameter(bip);
                    if (p != null && p.StorageType == StorageType.Double)
                    {
                        double val = p.AsDouble();
                        if (val > 0.001) return val;
                    }
                }
                catch { }
            }

            // Try common named parameters
            string[] names = { "Length", "Height", "Unconnected Height", "Cut Length" };
            foreach (string name in names)
            {
                Parameter p = fi.LookupParameter(name);
                if (p != null && p.StorageType == StorageType.Double)
                {
                    double val = p.AsDouble();
                    if (val > 0.001) return val;
                }
            }

            return 0;
        }

        //=====================================================

        private string XYZToString(XYZ pt) =>
            $"({pt.X:F4}, {pt.Y:F4}, {pt.Z:F4})";
    }
}

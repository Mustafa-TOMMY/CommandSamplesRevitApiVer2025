using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Get End Point and Direction from LocationPoint Family
    //
    // PROBLEM:
    // When a FamilyInstance uses a LocationPoint (single insertion XYZ), Revit does
    // NOT provide a built-in "EndPoint" property.
    //
    // If the family represents directional equipment (e.g. conveyor, machine,
    // cantilever fixture, pipe accessory, or directional component), how do we
    // determine its true 3D End Point (Outfeed), 3D Direction, and Z-elevation?
    //
    // THIS COMMAND DEMONSTRATES:
    //
    // 1. Method 1 (Universal / Generic): Vector Ray Projection
    //    EndPoint = StartPoint + (HandOrientation * Length)
    //
    // 2. Method 2 (Universal 3D Transform): Transform.OfPoint(localEndPoint)
    //
    // 3. Method 3 (Domain-Specific MEP): ConnectorManager (Inflow/Outflow origins)
    //
    // 4. Method 4 (Geometric Solid Extents): Furthest 3D vertex along orientation
    //
    // 5. Method 5 (2D Polar Fallback): LocationPoint.Rotation (cos θ, sin θ, 0)
    //    and explains why it fails on 3D slopes/inclinations.
    //
    // 6. Infeed vs Outfeed Z-Level Analysis relative to World Origin (0,0,0):
    //    - Infeed Z (Z1) vs Outfeed Z (Z2)
    //    - Height Delta (ΔZ = Z2 - Z1)
    //    - Horizontal Run and 3D Slope Percentage
    //
    // SYSTEM FAMILIES vs LOADABLE FAMILIES:
    // - System Families: Mostly use LocationCurve (Walls, Beams, Pipes, Ducts).
    //   Start/End are read directly from Curve.GetEndPoint(0) and (1).
    // - Loadable Families: Mostly use LocationPoint. Must use Transform / HandOrientation.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 10
    public class GetLocationPointEndPointCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Pick target element
                Reference pickedRef = uiDoc.Selection.PickObject(
                    ObjectType.Element, 
                    "Select an element (Loadable Family or System Family) to calculate its End Point & Direction");

                Element element = doc.GetElement(pickedRef);
                if (element == null) return Result.Failed;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("LOCATION POINT -> END POINT & DIRECTION ANALYSIS");
                sb.AppendLine("==================================================");
                sb.AppendLine($"Element Name: {element.Name}");
                sb.AppendLine($"Category    : {element.Category?.Name ?? "N/A"}");
                sb.AppendLine($"Element Id  : {element.Id}");
                sb.AppendLine();

                // ================================================================
                // CASE 1: Loadable Family (FamilyInstance with LocationPoint)
                // ================================================================
                if (element is FamilyInstance familyInstance && element.Location is LocationPoint locPoint)
                {
                    XYZ startPoint = locPoint.Point; // Insertion Point (Infeed / Base)
                    
                    // Attempt to find a Length parameter (Instance or Type)
                    double length = GetElementLength(familyInstance, defaultLengthFeet: 10.0);

                    sb.AppendLine("[LOADABLE FAMILY — LOCATIONPOINT]");
                    sb.AppendLine($"Family Name : {familyInstance.Symbol?.Family?.Name ?? "N/A"}");
                    sb.AppendLine($"Type Name   : {familyInstance.Symbol?.Name ?? "N/A"}");
                    sb.AppendLine($"Start/Origin: ({startPoint.X:F3}, {startPoint.Y:F3}, {startPoint.Z:F3}) ft");
                    sb.AppendLine($"Length Used : {length:F3} ft ({UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters):F2} m)");
                    sb.AppendLine();

                    // ------------------------------------------------------------
                    // Method 1 (Generic): Vector Ray Projection with HandOrientation
                    // ------------------------------------------------------------
                    XYZ handDir = familyInstance.HandOrientation; // Local X in 3D world space
                    XYZ facingDir = familyInstance.FacingOrientation; // Local Y in 3D world space
                    XYZ endPointHand = startPoint + (handDir * length);

                    sb.AppendLine("1. METHOD 1 (Generic Vector Ray - HandOrientation):");
                    sb.AppendLine($"   Hand Direction : ({handDir.X:F3}, {handDir.Y:F3}, {handDir.Z:F3})");
                    sb.AppendLine($"   Facing Direction: ({facingDir.X:F3}, {facingDir.Y:F3}, {facingDir.Z:F3})");
                    sb.AppendLine($"   End Point (Hand): ({endPointHand.X:F3}, {endPointHand.Y:F3}, {endPointHand.Z:F3}) ft");
                    sb.AppendLine($"   Hand Flipped?   : {familyInstance.HandFlipped}");
                    sb.AppendLine();

                    // ------------------------------------------------------------
                    // Method 2 (Universal 3D Transform): Transform.OfPoint
                    // ------------------------------------------------------------
                    Autodesk.Revit.DB.Transform totalTransform = familyInstance.GetTotalTransform();
                    XYZ localEndPoint = new XYZ(length, 0, 0); // L along local X
                    XYZ endPointTransform = totalTransform.OfPoint(localEndPoint);

                    sb.AppendLine("2. METHOD 2 (3D Transform Matrix - Transform.OfPoint):");
                    sb.AppendLine($"   Basis X (Local X): ({totalTransform.BasisX.X:F3}, {totalTransform.BasisX.Y:F3}, {totalTransform.BasisX.Z:F3})");
                    sb.AppendLine($"   Basis Y (Local Y): ({totalTransform.BasisY.X:F3}, {totalTransform.BasisY.Y:F3}, {totalTransform.BasisY.Z:F3})");
                    sb.AppendLine($"   Basis Z (Local Z): ({totalTransform.BasisZ.X:F3}, {totalTransform.BasisZ.Y:F3}, {totalTransform.BasisZ.Z:F3})");
                    sb.AppendLine($"   End Point (Matrix): ({endPointTransform.X:F3}, {endPointTransform.Y:F3}, {endPointTransform.Z:F3}) ft");
                    sb.AppendLine();

                    // ------------------------------------------------------------
                    // Method 3 (Domain-Specific MEP Connectors): ConnectorManager
                    // ------------------------------------------------------------
                    sb.AppendLine("3. METHOD 3 (MEP Connectors - If Present):");
                    MEPModel mepModel = familyInstance.MEPModel;
                    if (mepModel?.ConnectorManager != null && mepModel.ConnectorManager.Connectors.Size > 0)
                    {
                        int connIdx = 1;
                        foreach (Connector conn in mepModel.ConnectorManager.Connectors)
                        {
                            sb.AppendLine($"   Port #{connIdx++} ({conn.Domain}, Dir={conn.Direction}): Origin=({conn.Origin.X:F3}, {conn.Origin.Y:F3}, {conn.Origin.Z:F3}) ft");
                        }
                    }
                    else
                    {
                        sb.AppendLine("   N/A (Non-MEP Family — MEPModel is null)");
                    }
                    sb.AppendLine();

                    // ------------------------------------------------------------
                    // Method 4 (2D Polar Fallback): LocationPoint.Rotation (cos θ, sin θ, 0)
                    // ------------------------------------------------------------
                    double rotationRad = locPoint.Rotation;
                    XYZ polarDir2D = new XYZ(Math.Cos(rotationRad), Math.Sin(rotationRad), 0.0);
                    XYZ endPoint2D = startPoint + (polarDir2D * length);

                    sb.AppendLine("4. METHOD 4 (2D Polar Fallback - LocationPoint.Rotation):");
                    sb.AppendLine($"   Rotation Angle  : {rotationRad:F4} rad ({(rotationRad * 180.0 / Math.PI):F1}°)");
                    sb.AppendLine($"   2D Vector (Z=0) : ({polarDir2D.X:F3}, {polarDir2D.Y:F3}, {polarDir2D.Z:F3})");
                    sb.AppendLine($"   End Point (2D)  : ({endPoint2D.X:F3}, {endPoint2D.Y:F3}, {endPoint2D.Z:F3}) ft");
                    sb.AppendLine("   *Note: Ignores 3D tilt/slope because Z is hardcoded to 0.");
                    sb.AppendLine();

                    // ------------------------------------------------------------
                    // 5. Infeed vs Outfeed Z-Level Analysis (Relative to Origin 0,0,0)
                    // ------------------------------------------------------------
                    XYZ pInfeed = startPoint;
                    XYZ pOutfeed = endPointHand;

                    double zInfeed = pInfeed.Z;
                    double zOutfeed = pOutfeed.Z;
                    double deltaZ = zOutfeed - zInfeed;
                    double horizontalRun = Math.Sqrt(Math.Pow(pOutfeed.X - pInfeed.X, 2) + Math.Pow(pOutfeed.Y - pInfeed.Y, 2));
                    double slopePercent = (horizontalRun > 0.0001) ? (deltaZ / horizontalRun) * 100.0 : 0.0;

                    sb.AppendLine("==================================================");
                    sb.AppendLine("ELEVATION & SLOPE ANALYSIS (Z-Level from 0,0,0):");
                    sb.AppendLine($"Infeed Z (Start Elevation) : {zInfeed:F3} ft ({UnitUtils.ConvertFromInternalUnits(zInfeed, UnitTypeId.Meters):F3} m)");
                    sb.AppendLine($"Outfeed Z (End Elevation)  : {zOutfeed:F3} ft ({UnitUtils.ConvertFromInternalUnits(zOutfeed, UnitTypeId.Meters):F3} m)");
                    sb.AppendLine($"Height Delta (ΔZ = Z2 - Z1): {deltaZ:F3} ft ({UnitUtils.ConvertFromInternalUnits(deltaZ, UnitTypeId.Millimeters):F1} mm)");
                    sb.AppendLine($"Horizontal Planar Run      : {horizontalRun:F3} ft");
                    sb.AppendLine($"Calculated Slope Gradient  : {slopePercent:F2}% ({(deltaZ > 0 ? "Sloping UP" : deltaZ < 0 ? "Sloping DOWN" : "Horizontal")})");
                }
                // ================================================================
                // CASE 2: Linear Elements with LocationCurve (System Families & Beams)
                // ================================================================
                else if (element.Location is LocationCurve locCurve)
                {
                    Curve curve = locCurve.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);
                    XYZ direction = (endPoint - startPoint).Normalize();
                    double length = curve.Length;

                    sb.AppendLine("[LINEAR ELEMENT — LOCATIONCURVE (System Family / Beam)]");
                    sb.AppendLine($"Start Point (0) : ({startPoint.X:F3}, {startPoint.Y:F3}, {startPoint.Z:F3}) ft");
                    sb.AppendLine($"End Point (1)   : ({endPoint.X:F3}, {endPoint.Y:F3}, {endPoint.Z:F3}) ft");
                    sb.AppendLine($"Length          : {length:F3} ft ({UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters):F2} m)");
                    sb.AppendLine($"3D Direction    : ({direction.X:F3}, {direction.Y:F3}, {direction.Z:F3})");
                    sb.AppendLine();
                    sb.AppendLine("Elevation Difference (ΔZ): " + (endPoint.Z - startPoint.Z).ToString("F3") + " ft");
                }
                else
                {
                    sb.AppendLine("The selected element does not have a standard LocationPoint or LocationCurve.");
                }

                TaskDialog.Show("LocationPoint End Point Analysis", sb.ToString());
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

        private double GetElementLength(FamilyInstance instance, double defaultLengthFeet)
        {
            // Check standard length parameters on instance
            string[] paramNames = { "Length", "Conveyor_Length", "Span", "Width", "Depth", "Height", "Distance" };

            foreach (string name in paramNames)
            {
                Parameter p = instance.LookupParameter(name);
                if (p != null && p.StorageType == StorageType.Double && p.AsDouble() > 0.001)
                {
                    return p.AsDouble();
                }
            }

            // Check standard length parameters on type symbol
            if (instance.Symbol != null)
            {
                foreach (string name in paramNames)
                {
                    Parameter p = instance.Symbol.LookupParameter(name);
                    if (p != null && p.StorageType == StorageType.Double && p.AsDouble() > 0.001)
                    {
                        return p.AsDouble();
                    }
                }
            }

            // Fallback to bounding box dimension
            BoundingBoxXYZ bbox = instance.get_BoundingBox(null);
            if (bbox != null)
            {
                double bboxLength = bbox.Max.X - bbox.Min.X;
                if (bboxLength > 0.5) return bboxLength;
            }

            return defaultLengthFeet;
        }
    }
}

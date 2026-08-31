using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Calculate Direction And End Point Command
    //
    // This command demonstrates 4 different methods to calculate the direction
    // and end point of an element based on Location and orientation.
    //
    // The 4 Methods:
    //
    // Method 01: HandOrientation / Face Orientation
    //       └─ Direct vector from Revit (Loadable Family only)
    //
    // Method 02: End - Start .Normalize()
    //       └─ From LocationCurve (Wall, Beam, Pipe)
    //          Works with both Loadable & System Family
    //
    // Method 03: Start + Rotation + Length
    //       └─ From LocationPoint + Angle
    //          Works with both Loadable & System Family
    //
    // Method 04: Infeed + Outfeed + Z Direction
    //       └─ Most detailed method with slope
    //          Works with both Loadable & System Family
    //          Infeed = distance before element
    //          Outfeed = distance after element
    //          Slope = vertical rise (independent of horizontal)
    //
    // Key Insight:
    // Infeed & Outfeed are HORIZONTAL distances
    // Slope is VERTICAL distance (INDEPENDENT)
    //
    // Formula for Method 04:
    //
    // EndPoint.X = Start.X + (Infeed + Length + Outfeed) × Cos(Rotation)
    // EndPoint.Y = Start.Y + (Infeed + Length + Outfeed) × Sin(Rotation)
    // EndPoint.Z = Start.Z + Slope
    //
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

                //=====================================================
                // STEP 1: Select an Element
                //=====================================================

                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element (FamilyInstance, Wall, Beam, etc.)");

                Element element = doc.GetElement(selRef);

                //=====================================================
                // STEP 2: Identify Element Type
                //=====================================================

                bool isLoadableFamily = (element is FamilyInstance);
                bool isSystemFamily = !isLoadableFamily;
                
                string familyType = isLoadableFamily ? "Loadable Family" : "System Family";

                Location location = element.Location;
                bool hasLocationPoint = (location is LocationPoint);
                bool hasLocationCurve = (location is LocationCurve);
                
                string locationType = hasLocationPoint ? "LocationPoint" : 
                                     hasLocationCurve ? "LocationCurve" : "Unknown";

                XYZ startPoint = null;
                double rotation = 0;

                // Extract start point and rotation
                if (location is LocationPoint locPoint)
                {
                    startPoint = locPoint.Point;
                    rotation = locPoint.Rotation;
                }
                else if (location is LocationCurve locCurve)
                {
                    Curve curve = locCurve.Curve;
                    startPoint = curve.GetEndPoint(0);
                }

                if (startPoint == null)
                {
                    TaskDialog.Show("Error", "Element has no valid location.");
                    return Result.Failed;
                }

                //=====================================================
                // STEP 3: Calculate Direction & End Point Using All 4 Methods
                //=====================================================

                XYZ endPoint1 = null;  // Method 01
                XYZ endPoint2 = null;  // Method 02
                XYZ endPoint3 = null;  // Method 03
                XYZ endPoint4 = null;  // Method 04

                // ═════════════════════════════════════════════════════════
                // METHOD 01: HandOrientation
                // ═════════════════════════════════════════════════════════
                // ✓ Works with: LOADABLE FAMILY ONLY
                // ✗ Does NOT work with: System Family
                // ✗ Requires: FamilyInstance type (not System type)
                // ✗ Location: Does NOT matter (Point or Curve)
                // ═════════════════════════════════════════════════════════

                if (isLoadableFamily && element is FamilyInstance familyInstance)
                {
                    endPoint1 = Method01_HandOrientation(familyInstance);
                }

                // ═════════════════════════════════════════════════════════
                // METHOD 02: End - Start .Normalize()
                // ═════════════════════════════════════════════════════════
                // ✓ Works with: LOADABLE FAMILY + SYSTEM FAMILY
                // ✓ Requires: LocationCurve ONLY
                // ✗ Does NOT work with: LocationPoint
                // Examples: Wall, Beam, Pipe, Curve-Based Elements
                // ═════════════════════════════════════════════════════════

                if (hasLocationCurve)
                {
                    endPoint2 = Method02_EndMinusStart(location as LocationCurve);
                }

                // ═════════════════════════════════════════════════════════
                // METHOD 03: Start + Rotation + Length
                // ═════════════════════════════════════════════════════════
                // ✓ Works with: LOADABLE FAMILY + SYSTEM FAMILY
                // ✓ Requires: LocationPoint ONLY
                // ✗ Does NOT work with: LocationCurve
                // ✗ Limitation: No vertical slope (Z = 0 always)
                // Examples: Door, Window, Furniture with rotation
                // ═════════════════════════════════════════════════════════

                if (hasLocationPoint)
                {
                    double elementLength = 3.0;  // Example: 3 feet
                    endPoint3 = Method03_RotationAndLength(startPoint, rotation, elementLength);
                }

                // ═════════════════════════════════════════════════════════
                // METHOD 04: Infeed + Outfeed + Z Direction (with Slope)
                // ═════════════════════════════════════════════════════════
                // ✓ Works with: LOADABLE FAMILY + SYSTEM FAMILY
                // ✓ Requires: LocationPoint ONLY
                // ✗ Does NOT work with: LocationCurve
                // ✓ Advantage: Handles slope/elevation
                // ✓ Includes: Infeed, Outfeed, Z Direction
                // IMPORTANT: Infeed & Outfeed ≠ Slope (they are independent!)
                // ═════════════════════════════════════════════════════════

                if (hasLocationPoint)
                {
                    double infeed = 0.5;       // 50cm before element
                    double elementLength = 3.0; // 3m element
                    double outfeed = 0.5;      // 50cm after element
                    double slope = 1.5;        // 1.5m rise (Z direction)

                    endPoint4 = Method04_InfeedOutfeedWithSlope(
                        startPoint, rotation, infeed, elementLength, outfeed, slope);
                }

                //=====================================================
                // STEP 4: Build Report
                //=====================================================

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("CALCULATE DIRECTION AND END POINT - ALL 4 METHODS");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine();

                sb.AppendLine("📋 ELEMENT INFORMATION:");
                sb.AppendLine("───────────────────────────────────────────────────────");
                sb.AppendLine($"Element Name         : {element.Name}");
                sb.AppendLine($"Element ID           : {element.Id}");
                sb.AppendLine($"Category             : {element.Category.Name}");
                sb.AppendLine();
                sb.AppendLine($"🔹 Family Type       : {familyType}");
                sb.AppendLine($"🔹 Location Type     : {locationType}");
                sb.AppendLine();

                sb.AppendLine("START POINT & ROTATION:");
                sb.AppendLine("───────────────────────────────────────────────────────");
                sb.AppendLine($"Start Point     : {PointToString(startPoint)}");
                sb.AppendLine($"Rotation (rad)  : {rotation:F6}");
                sb.AppendLine($"Rotation (deg)  : {RadianToDegree(rotation):F2}°");
                sb.AppendLine();

                sb.AppendLine("DIRECTION COMPONENTS:");
                sb.AppendLine("───────────────────────────────────────────────────────");
                double dirX = Math.Cos(rotation);
                double dirY = Math.Sin(rotation);
                sb.AppendLine($"Cos(Rotation)   : {dirX:F4}  ← X Direction");
                sb.AppendLine($"Sin(Rotation)   : {dirY:F4}  ← Y Direction");
                sb.AppendLine();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("METHOD 01: HandOrientation");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("✓ Compatible with : LOADABLE FAMILY ONLY");
                sb.AppendLine("✗ NOT compatible  : System Family");
                sb.AppendLine("✓ Location Type   : Any (Point or Curve)");
                sb.AppendLine("✓ Advantage       : Direct vector from Revit (no calculation)");
                sb.AppendLine();
                if (endPoint1 != null)
                {
                    sb.AppendLine($"✓ Direction    : {PointToString(endPoint1)}");
                }
                else if (isLoadableFamily)
                {
                    sb.AppendLine("⚠ Element is Loadable Family but HandOrientation is null");
                }
                else
                {
                    sb.AppendLine($"✗ NOT AVAILABLE - Element is {familyType}");
                }
                sb.AppendLine();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("METHOD 02: End - Start .Normalize()");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("✓ Compatible with : LOADABLE FAMILY + SYSTEM FAMILY");
                sb.AppendLine("✓ Location Type   : LocationCurve ONLY");
                sb.AppendLine("✗ NOT for         : LocationPoint");
                sb.AppendLine("✓ Advantage       : Works with both Loadable & System");
                sb.AppendLine("  Examples       : Wall, Beam, Pipe, Curve-Based Elements");
                sb.AppendLine();
                if (endPoint2 != null)
                {
                    sb.AppendLine($"✓ Direction    : {PointToString(endPoint2)}");
                    sb.AppendLine($"  Formula    : Direction = (Curve.End - Curve.Start).Normalize()");
                }
                else if (hasLocationCurve)
                {
                    sb.AppendLine("⚠ Element has LocationCurve but calculation failed");
                }
                else
                {
                    sb.AppendLine($"✗ NOT AVAILABLE - Element has {locationType}, needs LocationCurve");
                }
                sb.AppendLine();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("METHOD 03: Start + Rotation + Length");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("✓ Compatible with : LOADABLE FAMILY + SYSTEM FAMILY");
                sb.AppendLine("✓ Location Type   : LocationPoint ONLY");
                sb.AppendLine("✗ NOT for         : LocationCurve");
                sb.AppendLine("✓ Advantage       : Generic, works for any element with rotation");
                sb.AppendLine("✗ Limitation      : Ignores vertical slope (Z = 0)");
                sb.AppendLine("  Examples       : Door, Window, Furniture, Inclined Column");
                sb.AppendLine();
                if (endPoint3 != null)
                {
                    sb.AppendLine($"✓ End Point    : {PointToString(endPoint3)}");
                    sb.AppendLine($"  Parameters :");
                    sb.AppendLine($"    • Length  : 3.0 ft");
                    sb.AppendLine($"  Formula    :");
                    sb.AppendLine($"    X = Start.X + Length × Cos(Rotation) = {endPoint3.X:F4}");
                    sb.AppendLine($"    Y = Start.Y + Length × Sin(Rotation) = {endPoint3.Y:F4}");
                    sb.AppendLine($"    Z = Start.Z (no slope) = {endPoint3.Z:F4}");
                }
                else if (hasLocationPoint)
                {
                    sb.AppendLine("⚠ Element has LocationPoint but calculation failed");
                }
                else
                {
                    sb.AppendLine($"✗ NOT AVAILABLE - Element has {locationType}, needs LocationPoint");
                }
                sb.AppendLine();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("METHOD 04: Infeed + Outfeed + Z Direction");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("✓ Compatible with : LOADABLE FAMILY + SYSTEM FAMILY");
                sb.AppendLine("✓ Location Type   : LocationPoint ONLY");
                sb.AppendLine("✗ NOT for         : LocationCurve");
                sb.AppendLine("✓ Advantage       : Most detailed (handles slope correctly)");
                sb.AppendLine("✓ Includes        : Infeed, Outfeed, Z Direction (slope)");
                sb.AppendLine("  Examples       : Inclined Stairs, Ramps, Sloped Elements");
                sb.AppendLine();
                if (endPoint4 != null)
                {
                    sb.AppendLine($"✓ End Point    : {PointToString(endPoint4)}");
                    sb.AppendLine();
                    sb.AppendLine($"  Parameters :");
                    sb.AppendLine($"    • Infeed        : 0.50 ft (distance BEFORE element - horizontal)");
                    sb.AppendLine($"    • Length        : 3.00 ft (element length)");
                    sb.AppendLine($"    • Outfeed       : 0.50 ft (distance AFTER element - horizontal)");
                    sb.AppendLine($"    • Slope (Z)     : 1.50 ft (vertical rise - INDEPENDENT!)");
                    sb.AppendLine($"    • Total Horiz   : 4.00 ft (Infeed + Length + Outfeed)");
                    sb.AppendLine();
                    sb.AppendLine($"  Formula (IMPORTANT: Slope is INDEPENDENT):");
                    sb.AppendLine($"    Horizontal Distance = Infeed + Length + Outfeed = 4.00 ft");
                    sb.AppendLine($"    X = Start.X + HorizDist × Cos(Rotation)");
                    sb.AppendLine($"      = {startPoint.X:F4} + 4.00 × {dirX:F4} = {endPoint4.X:F4}");
                    sb.AppendLine($"    Y = Start.Y + HorizDist × Sin(Rotation)");
                    sb.AppendLine($"      = {startPoint.Y:F4} + 4.00 × {dirY:F4} = {endPoint4.Y:F4}");
                    sb.AppendLine($"    Z = Start.Z + Slope (NOT distributed over horizontal distance!)");
                    sb.AppendLine($"      = {startPoint.Z:F4} + 1.50 = {endPoint4.Z:F4}");
                }
                else if (hasLocationPoint)
                {
                    sb.AppendLine("⚠ Element has LocationPoint but calculation failed");
                }
                else
                {
                    sb.AppendLine($"✗ NOT AVAILABLE - Element has {locationType}, needs LocationPoint");
                }
                sb.AppendLine();

                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("KEY INSIGHT: Infeed & Outfeed vs Slope");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("❌ WRONG:");
                sb.AppendLine("   Z = Start.Z + (Infeed + Length + Outfeed) × Slope");
                sb.AppendLine("   └─ This multiplies horizontal distance by vertical slope!");
                sb.AppendLine("   └─ Would give HUGE Z values (4.0 × 1.5 = 6.0 instead of 1.5)");
                sb.AppendLine();
                sb.AppendLine("✓ CORRECT:");
                sb.AppendLine("   X = Start.X + (Infeed+Length+Outfeed) × Cos(Rotation)");
                sb.AppendLine("   Y = Start.Y + (Infeed+Length+Outfeed) × Sin(Rotation)");
                sb.AppendLine("   Z = Start.Z + Slope  ← Slope is COMPLETELY SEPARATE!");
                sb.AppendLine();
                sb.AppendLine("Infeed & Outfeed = HORIZONTAL distances (X & Y only)");
                sb.AppendLine("Slope = VERTICAL distance (Z only, independent)");
                sb.AppendLine();

                TaskDialog.Show("Calculate Direction And End Point - All 4 Methods", sb.ToString());

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
        // METHOD 01: HandOrientation
        //=====================================================

        private XYZ Method01_HandOrientation(FamilyInstance familyInstance)
        {
            // Direct vector from Revit
            // No calculation needed
            return familyInstance.HandOrientation;
        }

        //=====================================================
        // METHOD 02: End - Start
        //=====================================================

        private XYZ Method02_EndMinusStart(LocationCurve locCurve)
        {
            Curve curve = locCurve.Curve;

            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);

            // Direction = (End - Start).Normalize()
            XYZ direction = endPoint.Subtract(startPoint).Normalize();

            return direction;
        }

        //=====================================================
        // METHOD 03: Start + Rotation + Length
        //=====================================================

        private XYZ Method03_RotationAndLength(XYZ startPoint, double rotation, double length)
        {
            // Calculate direction from angle
            double directionX = Math.Cos(rotation);
            double directionY = Math.Sin(rotation);
            double directionZ = 0;  // Horizontal only

            // Calculate end point
            XYZ endPoint = new XYZ(
                startPoint.X + length * directionX,
                startPoint.Y + length * directionY,
                startPoint.Z + directionZ
            );

            return endPoint;
        }

        //=====================================================
        // METHOD 04: Infeed + Outfeed + Z Direction (with Slope)
        //=====================================================

        private XYZ Method04_InfeedOutfeedWithSlope(
            XYZ startPoint,
            double rotation,
            double infeed,
            double elementLength,
            double outfeed,
            double slope)
        {
            // STEP 1: Calculate total horizontal distance
            // (Infeed and Outfeed are HORIZONTAL only)
            double totalHorizontalDistance = infeed + elementLength + outfeed;

            // STEP 2: Calculate direction from angle
            double directionX = Math.Cos(rotation);
            double directionY = Math.Sin(rotation);

            // STEP 3: Calculate end point
            // NOTE: Slope is INDEPENDENT from horizontal distance!
            XYZ endPoint = new XYZ(
                startPoint.X + totalHorizontalDistance * directionX,
                startPoint.Y + totalHorizontalDistance * directionY,
                startPoint.Z + slope  // Slope is separate!
            );

            return endPoint;
        }

        //=====================================================
        // Helper Methods
        //=====================================================

        private string PointToString(XYZ pt)
        {
            if (pt == null) return "N/A";
            return $"({pt.X:F4}, {pt.Y:F4}, {pt.Z:F4})";
        }

        private double RadianToDegree(double radians)
        {
            return radians * (180.0 / Math.PI);
        }
    }
}
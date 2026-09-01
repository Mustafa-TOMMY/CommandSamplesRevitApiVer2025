using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // Transform-Based Family Geometry Command
    //
    // Part B - Command 06
    //
    // Purpose:
    //
    // Inspect the 3D coordinate system of a FamilyInstance through:
    //
    // FamilyInstance.GetTransform()
    //
    // The Transform provides:
    //
    // - Origin
    // - BasisX
    // - BasisY
    // - BasisZ
    //
    // Important:
    //
    // BasisX/BasisY/BasisZ represent the FamilyInstance local coordinate axes.
    //
    // We DO NOT assume that BasisX is always the family's longitudinal
    // or physical direction.
    //
    // If a LocationCurve exists, the command compares the curve direction
    // against all three Transform basis vectors.
    //
    // This allows us to determine which local axis is actually aligned
    // with the physical curve direction.
    //
    // The command is READ-ONLY.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class TransformBasedFamilyGeometryCommand : IExternalCommand
    {
        private const double Tolerance = 1e-6;
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
                        "Select a FamilyInstance to inspect its 3D Transform");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Transform-Based Family",
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
                        "Transform-Based Family",
                        "Could not obtain Family or FamilySymbol.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Get Transform
                //=====================================================

                Transform transform = null;

                try
                {
                    transform = familyInstance.GetTransform();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show(
                        "Transform-Based Family",
                        "Could not obtain the FamilyInstance Transform.\n\n" +
                        ex.Message);

                    return Result.Failed;
                }

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Transform-Based Family",
                        "The FamilyInstance returned a null Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 4. Extract Transform Components
                //=====================================================

                XYZ origin = transform.Origin;

                XYZ basisX = transform.BasisX.Normalize();

                XYZ basisY = transform.BasisY.Normalize();

                XYZ basisZ = transform.BasisZ.Normalize();

                //=====================================================
                // 5. Inspect Actual Location
                //=====================================================

                Location location = familyInstance.Location;

                string locationType = location == null ? "null" : location.GetType().Name;

                //=====================================================
                // 6. Try to Obtain Physical Curve Direction
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                XYZ curveDirection = null;
                XYZ curveStart = null;
                XYZ curveEnd = null;

                double curveLength = 0.0;

                if (locationCurve != null && locationCurve.Curve != null)
                {
                    Curve curve = locationCurve.Curve;

                    curveStart = curve.GetEndPoint(0);

                    curveEnd = curve.GetEndPoint(1);

                    curveLength = curve.Length;

                    XYZ rawDirection = curveEnd - curveStart;

                    if (rawDirection.GetLength() > Tolerance)
                    {
                        curveDirection = rawDirection.Normalize();
                    }
                }

                //=====================================================
                // 7. Compare Curve Direction Against Transform Axes
                //
                // Dot Product:
                //
                // +1  → Same direction
                // -1  → Opposite direction
                //  0  → Perpendicular
                //=====================================================

                double dotX = double.NaN;
                double dotY = double.NaN;
                double dotZ = double.NaN;

                double angleX = double.NaN;
                double angleY = double.NaN;
                double angleZ = double.NaN;

                if (curveDirection != null)
                {
                    dotX = curveDirection.DotProduct(basisX);

                    dotY = curveDirection.DotProduct(basisY);

                    dotZ = curveDirection.DotProduct(basisZ);

                    dotX = Clamp(dotX, -1.0, 1.0);

                    dotY = Clamp(dotY, -1.0, 1.0);

                    dotZ = Clamp(dotZ, -1.0, 1.0);

                    angleX = Math.Acos(Math.Abs(dotX)) * 180.0 / Math.PI;

                    angleY = Math.Acos(Math.Abs(dotY)) * 180.0 / Math.PI;

                    angleZ = Math.Acos(Math.Abs(dotZ)) * 180.0 / Math.PI;
                }

                //=====================================================
                // 8. Determine Closest Axis
                //=====================================================

                string closestAxis = "Not Available";

                double closestAlignment = double.NaN;

                if (curveDirection != null)
                {
                    double absX = Math.Abs(dotX);

                    double absY = Math.Abs(dotY);

                    double absZ = Math.Abs(dotZ);

                    if (absX >= absY && absX >= absZ)
                    {
                        closestAxis = "BasisX";

                        closestAlignment = absX;
                    }
                    else if (absY >= absX && absY >= absZ)
                    {
                        closestAxis = "BasisY";

                        closestAlignment = absY;
                    }
                    else
                    {
                        closestAxis = "BasisZ";
                        closestAlignment = absZ;
                    }
                }

                //=====================================================
                // 9. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb =
                            new StringBuilder();

                sb.AppendLine(
                    "TRANSFORM-BASED FAMILY GEOMETRY");

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
                    $"Family Name     : {family.Name}");

                sb.AppendLine(
                    $"Symbol / Type   : {symbol.Name}");

                sb.AppendLine(
                    $"Placement Type  : {family.FamilyPlacementType}");

                sb.AppendLine(
                    $"Location Type   : {locationType}");

                sb.AppendLine();

                //=====================================================
                // Transform Origin
                //=====================================================

                sb.AppendLine(
                    "2. TRANSFORM ORIGIN");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({origin.X:F6}, " +
                    $"{origin.Y:F6}, " +
                    $"{origin.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // BasisX
                //=====================================================

                sb.AppendLine(
                    "3. BASIS X");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({basisX.X:F6}, " +
                    $"{basisX.Y:F6}, " +
                    $"{basisX.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // BasisY
                //=====================================================

                sb.AppendLine(
                    "4. BASIS Y");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({basisY.X:F6}, " +
                    $"{basisY.Y:F6}, " +
                    $"{basisY.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // BasisZ
                //=====================================================

                sb.AppendLine(
                    "5. BASIS Z");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({basisZ.X:F6}, " +
                    $"{basisZ.Y:F6}, " +
                    $"{basisZ.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Coordinate System Interpretation
                //=====================================================

                sb.AppendLine(
                    "6. LOCAL COORDINATE SYSTEM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "BasisX = Family local X axis");

                sb.AppendLine(
                    "BasisY = Family local Y axis");

                sb.AppendLine(
                    "BasisZ = Family local Z axis");

                sb.AppendLine();

                sb.AppendLine(
                    "Important:");

                sb.AppendLine(
                    "These axes describe the FamilyInstance " +
                    "coordinate system.");

                sb.AppendLine(
                    "Their semantic meaning as 'length', 'width', " +
                    "or 'up' depends on the Family definition.");

                sb.AppendLine();

                //=====================================================
                // LocationCurve Comparison
                //=====================================================

                sb.AppendLine(
                    "7. CURVE DIRECTION COMPARISON");

                sb.AppendLine(
                    "----------------------------------------");

                if (curveDirection != null)
                {
                    sb.AppendLine(
                        "LocationCurve detected.");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Start Point:");

                    sb.AppendLine(
                        $"  ({curveStart.X:F6}, " +
                        $"{curveStart.Y:F6}, " +
                        $"{curveStart.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "End Point:");

                    sb.AppendLine(
                        $"  ({curveEnd.X:F6}, " +
                        $"{curveEnd.Y:F6}, " +
                        $"{curveEnd.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Curve Length:");

                    sb.AppendLine(
                        $"  {curveLength:F6} ft");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Actual Curve Direction:");

                    sb.AppendLine(
                        $"  ({curveDirection.X:F6}, " +
                        $"{curveDirection.Y:F6}, " +
                        $"{curveDirection.Z:F6})");

                    sb.AppendLine();

                    //=================================================
                    // BasisX Comparison
                    //=================================================

                    sb.AppendLine(
                        "Curve vs BasisX:");

                    sb.AppendLine(
                        $"  Dot Product = {dotX:F6}");

                    sb.AppendLine(
                        $"  Angle       = {angleX:F6}°");

                    sb.AppendLine();

                    //=================================================
                    // BasisY Comparison
                    //=================================================

                    sb.AppendLine(
                        "Curve vs BasisY:");

                    sb.AppendLine(
                        $"  Dot Product = {dotY:F6}");

                    sb.AppendLine(
                        $"  Angle       = {angleY:F6}°");

                    sb.AppendLine();

                    //=================================================
                    // BasisZ Comparison
                    //=================================================

                    sb.AppendLine(
                        "Curve vs BasisZ:");

                    sb.AppendLine(
                        $"  Dot Product = {dotZ:F6}");

                    sb.AppendLine(
                        $"  Angle       = {angleZ:F6}°");

                    sb.AppendLine();

                    //=================================================
                    // Closest Axis
                    //=================================================

                    sb.AppendLine(
                        "Closest Transform Axis:");

                    sb.AppendLine(
                        $"  {closestAxis}");

                    sb.AppendLine();

                    sb.AppendLine(
                        $"Alignment:");

                    sb.AppendLine(
                        $"  {closestAlignment:F6}");
                }
                else
                {
                    sb.AppendLine(
                        "No LocationCurve detected.");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Transform axes are therefore inspected " +
                        "as the FamilyInstance's local coordinate system.");

                    sb.AppendLine();

                    sb.AppendLine(
                        "No physical curve direction is assumed.");
                }

                sb.AppendLine();

                //=====================================================
                // Main Geometry Interpretation
                //=====================================================

                sb.AppendLine(
                    "8. GEOMETRY INTERPRETATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Origin:");

                sb.AppendLine(
                    "Defines the origin of the instance Transform.");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisX / BasisY / BasisZ:");

                sb.AppendLine(
                    "Define the three local coordinate axes.");

                sb.AppendLine();

                if (curveDirection != null)
                {
                    sb.AppendLine(
                        "Because a LocationCurve exists:");

                    sb.AppendLine(
                        "The actual physical curve direction can be " +
                        "compared directly with the local axes.");

                    sb.AppendLine();

                    sb.AppendLine(
                        $"Closest axis = {closestAxis}");
                }
                else
                {
                    sb.AppendLine(
                        "Because no LocationCurve exists:");

                    sb.AppendLine(
                        "The Transform alone does not define which " +
                        "axis represents the family's business/physical length.");
                }

                sb.AppendLine();

                //=====================================================
                // Main Five Values
                //=====================================================

                sb.AppendLine(
                    "9. MAIN GEOMETRIC VALUES");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Start Point:");

                if (curveStart != null)
                {
                    sb.AppendLine(
                        $"  ({curveStart.X:F6}, " +
                        $"{curveStart.Y:F6}, " +
                        $"{curveStart.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "  Not defined by Transform alone.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "End Point:");

                if (curveEnd != null)
                {
                    sb.AppendLine(
                        $"  ({curveEnd.X:F6}, " +
                        $"{curveEnd.Y:F6}, " +
                        $"{curveEnd.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "  Not defined by Transform alone.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "3D Direction:");

                if (curveDirection != null)
                {
                    sb.AppendLine(
                        $"  ({curveDirection.X:F6}, " +
                        $"{curveDirection.Y:F6}, " +
                        $"{curveDirection.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Source: LocationCurve.");
                }
                else
                {
                    sb.AppendLine(
                        "  Transform provides three candidate axes:");

                    sb.AppendLine(
                        "  BasisX / BasisY / BasisZ");

                    sb.AppendLine();

                    sb.AppendLine(
                        "The semantic direction must be determined " +
                        "from Family geometry or definition.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "Actual Length:");

                if (curveLength > Tolerance)
                {
                    sb.AppendLine(
                        $"  {curveLength:F6} ft");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Source: LocationCurve.Curve.Length.");
                }
                else
                {
                    sb.AppendLine(
                        "  Not defined by Transform alone.");
                }

                sb.AppendLine();

                sb.AppendLine(
                    "Rotation:");

                sb.AppendLine(
                    "Transform provides orientation axes.");

                sb.AppendLine(
                    "A single scalar rotation angle is not universally " +
                    "defined for arbitrary 3D orientation.");

                sb.AppendLine();

                //=====================================================
                // Final Rule
                //=====================================================

                sb.AppendLine(
                    "10. CORE RULE");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Transform = Coordinate System");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisX/BasisY/BasisZ tell us HOW the Family is oriented.");

                sb.AppendLine();

                sb.AppendLine(
                    "They do NOT automatically tell us WHICH axis " +
                    "represents the business meaning of length.");

                sb.AppendLine();

                sb.AppendLine(
                    "That semantic meaning must come from:");

                sb.AppendLine(
                    "• Native geometry");

                sb.AppendLine(
                    "• Location");

                sb.AppendLine(
                    "• Family definition");

                sb.AppendLine(
                    "• Parameters");
                #endregion

                //=====================================================
                // 11. Display
                //=====================================================

                TaskDialog.Show(
                    "Transform-Based Family Geometry",
                    sb.ToString());

                return Result.Succeeded;
            }
            catch (
                Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return Result.Failed;
            }
        }
        /// <summary>
        /// Clamps a numeric value so that it remains within the specified range.
        /// </summary>
        /// <returns>
        /// The original value if it is within the range;
        /// otherwise, the nearest boundary value (<paramref name="min"/> or
        /// <paramref name="max"/>).
        /// </returns>
        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
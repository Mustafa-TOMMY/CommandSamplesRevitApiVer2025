using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // Location Point 3D Analysis Command
    //
    // Command 05
    //
    // Purpose:
    //
    // Analyze a point-based FamilyInstance using three engineering parameters:
    //
    //      Length
    //      Infeed Elevation
    //      Outfeed Elevation
    //
    // Then derive:
    //
    //      1. Start Point
    //      2. End Point
    //      3. 3D Direction
    //      4. Rotation
    //      5. Horizontal Projection
    //
    // Conceptual Workflow:
    //
    // FamilyInstance
    //       ↓
    // LocationPoint
    //       ↓
    // Location Point + Rotation
    //
    // Parameters
    //       ├── Length
    //       ├── Infeed Elevation
    //       └── Outfeed Elevation
    //                 ↓
    //              ΔZ
    //                 ↓
    //       Horizontal Projection
    //                 ↓
    //          3D Direction
    //                 ↓
    //             End Point
    //
    // IMPORTANT:
    //
    // This command assumes that:
    //
    // LocationPoint.Point = Infeed / Start Point
    //
    // If the Family's LocationPoint represents its center instead,
    // the Start/End point calculation must be changed.
    //
    // Parameter names are intentionally configurable through constants.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class LocationPoint3DAnalysisCommand : IExternalCommand
    {
        //=====================================================
        // Parameter Names
        // Change these names if the company's Family parameters
        // use different names.
        //=====================================================

        private const string LengthParameterName = "Length";
        private const string InfeedElevationParameterName = "Infeed";
        private const string OutfeedElevationParameterName = "Outfeed";

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
                // 1. Select FamilyInstance
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a point-based FamilyInstance to analyze");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Verify LocationPoint
                //=====================================================

                LocationPoint locationPoint = familyInstance.Location as LocationPoint;

                if (locationPoint == null)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "The selected FamilyInstance does not have a LocationPoint.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Find Required Parameters
                //=====================================================

                Parameter lengthParameter = FindParameter(familyInstance, LengthParameterName);

                Parameter infeedParameter = FindParameter(familyInstance, InfeedElevationParameterName);

                Parameter outfeedParameter = FindParameter(familyInstance, OutfeedElevationParameterName);

                //=====================================================
                // 4. Validate Parameter Discovery
                //=====================================================

                StringBuilder missingParameters = new StringBuilder();

                if (lengthParameter == null)
                {
                    missingParameters.AppendLine(
                        $"Length parameter not found: " +
                        $"'{LengthParameterName}'");
                }

                if (infeedParameter == null)
                {
                    missingParameters.AppendLine(
                        $"Infeed parameter not found: " +
                        $"'{InfeedElevationParameterName}'");
                }

                if (outfeedParameter == null)
                {
                    missingParameters.AppendLine(
                        $"Outfeed parameter not found: " +
                        $"'{OutfeedElevationParameterName}'");
                }

                if (missingParameters.Length > 0)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "Required parameters were not found.\n\n" +
                        missingParameters +
                        "\nChange the parameter-name constants " +
                        "at the top of the command.");

                    return Result.Failed;
                }

                //=====================================================
                // 5. Read Parameter Values
                // AsDouble() returns Revit internal units.
                // For Length parameters this normally means feet.
                //=====================================================

                double length = lengthParameter.AsDouble();
                double infeedElevation = infeedParameter.AsDouble();
                double outfeedElevation = outfeedParameter.AsDouble();

                //=====================================================
                // 6. Validate Values
                //=====================================================

                if (length <= 0)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "Length must be greater than zero.");

                    return Result.Failed;
                }

                //=====================================================
                // 7. Get Location Point
                // Assumption:
                // LocationPoint.Point = Infeed / Start Point
                //=====================================================

                XYZ startPoint = locationPoint.Point;

                //=====================================================
                // 8. Calculate Vertical Difference
                //=====================================================

                double deltaZ = outfeedElevation - infeedElevation;

                //=====================================================
                // 9. Validate Geometry
                // L² = H² + ΔZ²
                // Therefore:
                // H = √(L² - ΔZ²)
                //=====================================================

                double horizontalSquared = (length * length) - (deltaZ * deltaZ);

                if (horizontalSquared < 0)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "Invalid geometry.\n\n" +
                        "The elevation difference is greater than " +
                        "the total Length.\n\n" +
                        $"Length = {length:F4} ft\n" +
                        $"Elevation Difference = {Math.Abs(deltaZ):F4} ft");

                    return Result.Failed;
                }

                double horizontalLength = Math.Sqrt(horizontalSquared);

                //=====================================================
                // 10. Get Family Transform
                //=====================================================

                Transform transform = familyInstance.GetTransform();

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "Could not obtain the FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 11. Get Horizontal Orientation
                // We use the Family local X axis as the horizontal
                // orientation of this Family.
                // This is a FAMILY CONVENTION, not a universal Revit rule.
                //=====================================================

                XYZ basisX = transform.BasisX;

                XYZ horizontalDirection = new XYZ(
                        basisX.X,
                        basisX.Y,
                        0);

                if (horizontalDirection.GetLength() < 1e-9)
                {
                    TaskDialog.Show(
                        "Location Point 3D",
                        "The Family local X axis does not provide " +
                        "a valid horizontal direction.");

                    return Result.Failed;
                }

                horizontalDirection = horizontalDirection.Normalize();

                //=====================================================
                // 12. Calculate 3D Direction
                // Horizontal component:
                // horizontalDirection * horizontalLength / length
                // Vertical component:
                // deltaZ / length
                //=====================================================

                XYZ direction = new XYZ(
                        horizontalDirection.X * (horizontalLength / length),
                        horizontalDirection.Y * (horizontalLength / length),
                        deltaZ / length);

                //=====================================================
                // 13. Normalize Direction
                //=====================================================

                direction = direction.Normalize();

                //=====================================================
                // 14. Calculate End Point
                // End = Start + Direction × Length
                //=====================================================

                XYZ endPoint = startPoint + direction * length;

                //=====================================================
                // 15. Validate Calculated End Elevation
                //=====================================================

                double calculatedEndElevation = endPoint.Z;

                double elevationError = calculatedEndElevation - (startPoint.Z + deltaZ);

                //=====================================================
                // 16. Get Rotation
                // LocationPoint.Rotation is already provided by Revit.
                //=====================================================

                double rotation = locationPoint.Rotation;

                double rotationDegrees = rotation * 180.0 / Math.PI;

                //=====================================================
                // 17. Calculate Direction Length
                //=====================================================

                double directionLength = direction.GetLength();

                //=====================================================
                // 18. Build Report
                //=====================================================

                StringBuilder sb = new StringBuilder();

                #region ReportStructure
                sb.AppendLine("LOCATION POINT 3D ANALYSIS");

                sb.AppendLine("========================================");

                sb.AppendLine(
                    $"Element Id : {familyInstance.Id}");

                sb.AppendLine($"Family     : " + $"{familyInstance.Symbol.Family.Name}");

                sb.AppendLine($"Type       : " + $"{familyInstance.Symbol.Name}");

                sb.AppendLine();

                //=====================================================
                // PARAMETERS
                //=====================================================

                sb.AppendLine("INPUT PARAMETERS");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"Length:");

                sb.AppendLine($"  {length:F4} ft");

                sb.AppendLine($"  Source: '{lengthParameter.Definition.Name}'");

                sb.AppendLine();

                sb.AppendLine("Infeed Elevation:");

                sb.AppendLine($"  {infeedElevation:F4} ft");

                sb.AppendLine($"  Source: '{infeedParameter.Definition.Name}'");

                sb.AppendLine();

                sb.AppendLine("Outfeed Elevation:");

                sb.AppendLine($"  {outfeedElevation:F4} ft");

                sb.AppendLine($"  Source: '{outfeedParameter.Definition.Name}'");

                sb.AppendLine();

                sb.AppendLine($"Elevation Difference (ΔZ):");

                sb.AppendLine($"  {deltaZ:F4} ft");

                sb.AppendLine();

                //=====================================================
                // ROTATION
                //=====================================================

                sb.AppendLine("ROTATION");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"Radians : {rotation:F6}");

                sb.AppendLine($"Degrees : {rotationDegrees:F2}");

                sb.AppendLine("Source  : Revit LocationPoint.Rotation");

                sb.AppendLine();

                //=====================================================
                // POINTS
                //=====================================================

                sb.AppendLine("3D POINTS");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("Start / Infeed Point:");

                sb.AppendLine(
                    $"  ({startPoint.X:F4}, " +
                    $"{startPoint.Y:F4}, " +
                    $"{startPoint.Z:F4})");

                sb.AppendLine("  Source: Revit LocationPoint.Point");

                sb.AppendLine();

                sb.AppendLine("End / Outfeed Point:");

                sb.AppendLine(
                    $"  ({endPoint.X:F4}, " +
                    $"{endPoint.Y:F4}, " +
                    $"{endPoint.Z:F4})");

                sb.AppendLine("Source: Calculated");

                sb.AppendLine();

                //=====================================================
                // DIRECTION
                //=====================================================

                sb.AppendLine("3D DIRECTION");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"X : {direction.X:F6}");

                sb.AppendLine($"Y : {direction.Y:F6}");

                sb.AppendLine($"Z : {direction.Z:F6}");

                sb.AppendLine();

                sb.AppendLine($"Direction Length : " + $"{directionLength:F6}");

                sb.AppendLine("Expected for normalized direction ≈ 1.0");

                sb.AppendLine();

                //=====================================================
                // HORIZONTAL COMPONENT
                //=====================================================

                sb.AppendLine("HORIZONTAL COMPONENT");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"Horizontal Length : " + $"{horizontalLength:F4} ft");

                sb.AppendLine();

                sb.AppendLine("Horizontal Direction:");

                sb.AppendLine($"  X : {horizontalDirection.X:F6}");

                sb.AppendLine($"  Y : {horizontalDirection.Y:F6}");

                sb.AppendLine($"  Z : {horizontalDirection.Z:F6}");

                sb.AppendLine();

                //=====================================================
                // VALIDATION
                //=====================================================

                sb.AppendLine("VALIDATION");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine(
                    $"Calculated End Elevation : " +
                    $"{calculatedEndElevation:F4} ft");

                sb.AppendLine(
                    $"Expected End Elevation   : " +
                    $"{startPoint.Z + deltaZ:F4} ft");

                sb.AppendLine($"Elevation Error          : " + $"{elevationError:F8} ft");

                sb.AppendLine();

                sb.AppendLine("FORMULAS");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("ΔZ = Outfeed Elevation - Infeed Elevation");

                sb.AppendLine("H = √(Length² - ΔZ²)");

                sb.AppendLine("3D Direction = " + "Horizontal Direction + Vertical Component");

                sb.AppendLine("End = Start + Direction × Length");

                #endregion

                //=====================================================
                // 19. Display
                //=====================================================

                TaskDialog.Show(
                    "Location Point 3D Analysis",
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

        // ============================================================================
        // Find Parameter
        //
        // Searches the selected FamilyInstance first.
        //
        // This method intentionally keeps parameter discovery isolated
        // from the geometry calculations.
        // ============================================================================

        private Parameter FindParameter(
            FamilyInstance familyInstance,
            string parameterName)
        {
            return familyInstance.LookupParameter(parameterName);
        }
    }
}
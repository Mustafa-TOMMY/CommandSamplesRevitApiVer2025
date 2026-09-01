using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.Fundamentals
{
    // ============================================================================
    // Transform.OfPoint Command
    //
    // Command 06
    //
    // Purpose:
    //
    // Demonstrate how Transform.OfPoint() converts a point from a local
    // coordinate system into the coordinate system represented by the Transform.
    //
    // Transform:
    //
    //        Origin
    //        BasisX
    //        BasisY
    //        BasisZ
    //
    // Point transformation:
    //
    // WorldPoint =
    //      Origin
    //      + LocalX * BasisX
    //      + LocalY * BasisY
    //      + LocalZ * BasisZ
    //
    // Important:
    //
    // OfPoint() transforms a POINT.
    // OfVector() will be handled separately in Command 07.
    //
    // This command does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class TransformOfPointCommand : IExternalCommand
    {
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
                        "Select a FamilyInstance to analyze Transform.OfPoint()");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Transform.OfPoint",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Transform
                //=====================================================

                Transform transform = familyInstance.GetTransform();

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Transform.OfPoint",
                        "Could not obtain the FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Read Coordinate System
                //=====================================================

                XYZ origin = transform.Origin;
                XYZ basisX = transform.BasisX;
                XYZ basisY = transform.BasisY;
                XYZ basisZ = transform.BasisZ;

                //=====================================================
                // 4. Create a Local Point
                // This point is intentionally expressed in the
                // local coordinate system of the Transform.
                //=====================================================

                XYZ localPoint = new XYZ(2, 3, 4);

                //=====================================================
                // 5. Transform Local Point → Model Point
                //=====================================================

                XYZ transformedPoint = transform.OfPoint(localPoint);

                //=====================================================
                // 6. Manually Calculate Expected Result
                // P =
                // Origin
                // + X * BasisX
                // + Y * BasisY
                // + Z * BasisZ
                //=====================================================

                XYZ manuallyCalculatedPoint = origin
                    + basisX * localPoint.X
                    + basisY * localPoint.Y
                    + basisZ * localPoint.Z;

                //=====================================================
                // 7. Validate
                //=====================================================

                double transformationError = transformedPoint.DistanceTo(manuallyCalculatedPoint);

                //=====================================================
                // 8. Get Actual Family Location Point
                //=====================================================

                LocationPoint locationPoint = familyInstance.Location as LocationPoint;

                XYZ familyLocationPoint = null;

                if (locationPoint != null)
                {
                    familyLocationPoint = locationPoint.Point;
                }

                //=====================================================
                // 9. Build Report
                //=====================================================

                #region Report structure
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("TRANSFORM.OFPOINT ANALYSIS");

                sb.AppendLine("========================================");

                sb.AppendLine($"Element Id : {familyInstance.Id}");

                sb.AppendLine($"Family     : " + $"{familyInstance.Symbol.Family.Name}");

                sb.AppendLine($"Type       : " + $"{familyInstance.Symbol.Name}");

                sb.AppendLine();

                //=====================================================
                // Coordinate System
                //=====================================================

                sb.AppendLine("TRANSFORM COORDINATE SYSTEM");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("Origin:");

                sb.AppendLine(
                    $"  ({origin.X:F4}, " +
                    $"{origin.Y:F4}, " +
                    $"{origin.Z:F4})");

                sb.AppendLine();

                sb.AppendLine("BasisX:");

                sb.AppendLine(
                    $"  ({basisX.X:F4}, " +
                    $"{basisX.Y:F4}, " +
                    $"{basisX.Z:F4})");

                sb.AppendLine();

                sb.AppendLine("BasisY:");

                sb.AppendLine(
                    $"  ({basisY.X:F4}, " +
                    $"{basisY.Y:F4}, " +
                    $"{basisY.Z:F4})");

                sb.AppendLine();

                sb.AppendLine("BasisZ:");

                sb.AppendLine(
                    $"  ({basisZ.X:F4}, " +
                    $"{basisZ.Y:F4}, " +
                    $"{basisZ.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Local Point
                //=====================================================

                sb.AppendLine("LOCAL POINT");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"X = {localPoint.X:F4}");

                sb.AppendLine($"Y = {localPoint.Y:F4}");

                sb.AppendLine($"Z = {localPoint.Z:F4}");

                sb.AppendLine();

                sb.AppendLine("Interpretation:");

                sb.AppendLine(
                    "This point is expressed using the " +
                    "Transform's local coordinate system.");

                sb.AppendLine();

                //=====================================================
                // Transformed Point
                //=====================================================

                sb.AppendLine("TRANSFORMED POINT");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("Transform.OfPoint(localPoint):");

                sb.AppendLine(
                    $"  ({transformedPoint.X:F4}, " +
                    $"{transformedPoint.Y:F4}, " +
                    $"{transformedPoint.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "This is the point expressed in the " +
                    "Transform's target/model coordinate system.");

                sb.AppendLine();

                //=====================================================
                // Manual Calculation
                //=====================================================

                sb.AppendLine("MANUAL CALCULATION");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("Origin");

                sb.AppendLine("+ LocalX × BasisX");

                sb.AppendLine("+ LocalY × BasisY");

                sb.AppendLine("+ LocalZ × BasisZ");

                sb.AppendLine();

                sb.AppendLine("Result:");

                sb.AppendLine(
                    $"  ({manuallyCalculatedPoint.X:F4}, " +
                    $"{manuallyCalculatedPoint.Y:F4}, " +
                    $"{manuallyCalculatedPoint.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Validation
                //=====================================================

                sb.AppendLine("VALIDATION");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine($"Transformation Error: " + $"{transformationError:F8} ft");

                sb.AppendLine();

                sb.AppendLine("Expected:");

                sb.AppendLine("Error ≈ 0");

                sb.AppendLine();

                //=====================================================
                // Actual Family Location
                //=====================================================

                sb.AppendLine("FAMILY LOCATION");

                sb.AppendLine("----------------------------------------");

                if (familyLocationPoint != null)
                {
                    sb.AppendLine($"LocationPoint.Point:");

                    sb.AppendLine(
                        $"  ({familyLocationPoint.X:F4}, " +
                        $"{familyLocationPoint.Y:F4}, " +
                        $"{familyLocationPoint.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine("Important:");

                    sb.AppendLine("This is the FamilyInstance's actual " + "model LocationPoint.");

                    sb.AppendLine("It should not automatically be confused " + "with an arbitrary local point.");
                }
                else
                {
                    sb.AppendLine("The selected FamilyInstance does not " + "have a LocationPoint.");
                }

                //=====================================================
                // Conceptual Summary
                //=====================================================

                sb.AppendLine();

                sb.AppendLine("CORE CONCEPT");

                sb.AppendLine("----------------------------------------");

                sb.AppendLine("Local Point");

                sb.AppendLine("      ↓");

                sb.AppendLine("Transform.OfPoint()");

                sb.AppendLine("      ↓");

                sb.AppendLine("Model / Target Point");

                sb.AppendLine();

                sb.AppendLine("Formula:");

                sb.AppendLine("P = Origin + X*BasisX + Y*BasisY + Z*BasisZ");
                #endregion

                //=====================================================
                // 10. Display
                //=====================================================

                TaskDialog.Show(
                    "Transform.OfPoint",
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
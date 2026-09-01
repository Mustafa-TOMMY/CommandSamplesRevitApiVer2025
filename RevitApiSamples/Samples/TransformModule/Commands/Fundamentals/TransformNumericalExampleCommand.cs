using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands
{
    // ============================================================================
    // Transform Numerical Example Command
    //
    // Command 10
    //
    // Purpose:
    //
    // Demonstrate Transform operations using explicit numerical values
    // without selecting an element from Revit.
    //
    // Coordinate System:
    //
    // Origin = (100, 200, 50)
    //
    // BasisX = (0, 1, 0)
    // BasisY = (-1, 0, 0)
    // BasisZ = (0, 0, 1)
    //
    // Local Point:
    //
    // (10, 20, 5)
    //
    // Local Vector:
    //
    // (10, 20, 5)
    //
    // Operations:
    //
    // 1. Transform.OfPoint()
    // 2. Transform.OfVector()
    // 3. Transform.Inverse
    // 4. Round-trip validation
    //
    // No Revit element is selected or modified.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class TransformNumericalExampleCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                //=====================================================
                // 1. Create Transform
                //=====================================================

                Transform transform = Transform.Identity;

                //=====================================================
                // 2. Define Coordinate System
                //=====================================================

                transform.Origin = new XYZ(100, 200, 50);
                transform.BasisX = new XYZ(0, 1, 0);
                transform.BasisY = new XYZ(-1, 0, 0);
                transform.BasisZ = new XYZ(0, 0, 1);

                //=====================================================
                // 3. Define Local Point
                // 4. Define Local Vector
                //=====================================================

                XYZ localPoint = new XYZ(10, 20, 5);
                XYZ localVector = new XYZ(10, 20, 5);

                //=====================================================
                // 5. Transform Point
                // 6. Transform Vector
                //=====================================================

                XYZ worldPoint = transform.OfPoint(localPoint);
                XYZ worldVector = transform.OfVector(localVector);

                //=====================================================
                // 7. Get Inverse Transform
                //=====================================================

                Transform inverse = transform.Inverse;

                //=====================================================
                // 8. Transform World Point Back to Local
                // 9. Transform World Vector Back to Local
                //=====================================================

                XYZ reconstructedLocalPoint = inverse.OfPoint(worldPoint);
                XYZ reconstructedLocalVector = inverse.OfVector(worldVector);

                //=====================================================
                // 10. Calculate Errors
                //=====================================================

                double pointError = localPoint.DistanceTo(reconstructedLocalPoint);
                double vectorError = localVector.DistanceTo(reconstructedLocalVector);

                //=====================================================
                // 11. Manual Point Calculation
                // Pworld =
                // Origin
                // + X * BasisX
                // + Y * BasisY
                // + Z * BasisZ
                //=====================================================

                XYZ manualWorldPoint =
                    transform.Origin
                    + transform.BasisX * localPoint.X
                    + transform.BasisY * localPoint.Y
                    + transform.BasisZ * localPoint.Z;

                //=====================================================
                // 12. Manual Vector Calculation
                // Vworld =
                // X * BasisX
                // + Y * BasisY
                // + Z * BasisZ
                //
                // Notice:
                // Origin is NOT included.
                //=====================================================

                XYZ manualWorldVector =
                    transform.BasisX * localVector.X
                    + transform.BasisY * localVector.Y
                    + transform.BasisZ * localVector.Z;


                double manualPointError = worldPoint.DistanceTo(manualWorldPoint);
                double manualVectorError = worldVector.DistanceTo(manualWorldVector);

                //=====================================================
                // 13. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb =
            new StringBuilder();

                sb.AppendLine(
                    "TRANSFORM NUMERICAL EXAMPLE");

                sb.AppendLine(
                    "========================================");

                //=====================================================
                // Coordinate System
                //=====================================================

                sb.AppendLine(
                    "1. COORDINATE SYSTEM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Origin:");

                sb.AppendLine(
                    "  (100, 200, 50)");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisX:");

                sb.AppendLine(
                    "  (0, 1, 0)");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisY:");

                sb.AppendLine(
                    "  (-1, 0, 0)");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisZ:");

                sb.AppendLine(
                    "  (0, 0, 1)");

                sb.AppendLine();

                //=====================================================
                // Local Point
                //=====================================================

                sb.AppendLine(
                    "2. LOCAL POINT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "P = (10, 20, 5)");

                sb.AppendLine();

                sb.AppendLine(
                    "Formula:");

                sb.AppendLine(
                    "Pworld = Origin");

                sb.AppendLine(
                    "       + X*BasisX");

                sb.AppendLine(
                    "       + Y*BasisY");

                sb.AppendLine(
                    "       + Z*BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "Substitution:");

                sb.AppendLine(
                    "(100,200,50)");

                sb.AppendLine(
                    "+ 10*(0,1,0)");

                sb.AppendLine(
                    "+ 20*(-1,0,0)");

                sb.AppendLine(
                    "+ 5*(0,0,1)");

                sb.AppendLine();

                sb.AppendLine(
                    "Result:");

                sb.AppendLine(
                    $"({worldPoint.X:F4}, " +
                    $"{worldPoint.Y:F4}, " +
                    $"{worldPoint.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Local Vector
                //=====================================================

                sb.AppendLine(
                    "3. LOCAL VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "V = (10, 20, 5)");

                sb.AppendLine();

                sb.AppendLine(
                    "Formula:");

                sb.AppendLine(
                    "Vworld = X*BasisX");

                sb.AppendLine(
                    "       + Y*BasisY");

                sb.AppendLine(
                    "       + Z*BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "Substitution:");

                sb.AppendLine(
                    "10*(0,1,0)");

                sb.AppendLine(
                    "+ 20*(-1,0,0)");

                sb.AppendLine(
                    "+ 5*(0,0,1)");

                sb.AppendLine();

                sb.AppendLine(
                    "Result:");

                sb.AppendLine(
                    $"({worldVector.X:F4}, " +
                    $"{worldVector.Y:F4}, " +
                    $"{worldVector.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Point vs Vector
                //=====================================================

                sb.AppendLine(
                    "4. POINT vs VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Point:");

                sb.AppendLine(
                    "OfPoint(P)");

                sb.AppendLine(
                    "includes Origin.");

                sb.AppendLine();

                sb.AppendLine(
                    "Vector:");

                sb.AppendLine(
                    "OfVector(V)");

                sb.AppendLine(
                    "does NOT include Origin.");

                sb.AppendLine();

                //=====================================================
                // Inverse
                //=====================================================

                sb.AppendLine(
                    "5. INVERSE TRANSFORM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "World Point:");

                sb.AppendLine(
                    $"({worldPoint.X:F4}, " +
                    $"{worldPoint.Y:F4}, " +
                    $"{worldPoint.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Inverse.OfPoint(WorldPoint):");

                sb.AppendLine(
                    $"({reconstructedLocalPoint.X:F4}, " +
                    $"{reconstructedLocalPoint.Y:F4}, " +
                    $"{reconstructedLocalPoint.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Original Local Point:");

                sb.AppendLine(
                    $"({localPoint.X:F4}, " +
                    $"{localPoint.Y:F4}, " +
                    $"{localPoint.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Vector Inverse
                //=====================================================

                sb.AppendLine(
                    "6. VECTOR INVERSE");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "World Vector:");

                sb.AppendLine(
                    $"({worldVector.X:F4}, " +
                    $"{worldVector.Y:F4}, " +
                    $"{worldVector.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Inverse.OfVector(WorldVector):");

                sb.AppendLine(
                    $"({reconstructedLocalVector.X:F4}, " +
                    $"{reconstructedLocalVector.Y:F4}, " +
                    $"{reconstructedLocalVector.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Original Local Vector:");

                sb.AppendLine(
                    $"({localVector.X:F4}, " +
                    $"{localVector.Y:F4}, " +
                    $"{localVector.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Validation
                //=====================================================

                sb.AppendLine(
                    "7. VALIDATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Point Round-Trip Error:");

                sb.AppendLine(
                    $"  {pointError:F10}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Vector Round-Trip Error:");

                sb.AppendLine(
                    $"  {vectorError:F10}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Manual Point Calculation Error:");

                sb.AppendLine(
                    $"  {manualPointError:F10}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Manual Vector Calculation Error:");

                sb.AppendLine(
                    $"  {manualVectorError:F10}");

                sb.AppendLine();

                sb.AppendLine(
                    "Expected:");

                sb.AppendLine(
                    "All errors ≈ 0");

                sb.AppendLine();

                //=====================================================
                // Final Summary
                //=====================================================

                sb.AppendLine(
                    "8. FINAL MENTAL MODEL");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "LOCAL POINT");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "OfPoint()");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "WORLD POINT");

                sb.AppendLine();

                sb.AppendLine(
                    "LOCAL VECTOR");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "OfVector()");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "WORLD VECTOR");

                sb.AppendLine();

                sb.AppendLine(
                    "WORLD");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "Inverse");

                sb.AppendLine(
                    "      ↓");

                sb.AppendLine(
                    "LOCAL");
                #endregion

                //=====================================================
                // 14. Display
                //=====================================================

                TaskDialog.Show("Transform Numerical Example", sb.ToString());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands
{
    // ============================================================================
    // Point vs Vector Transformation Command
    //
    // Command 08
    //
    // Purpose:
    //
    // Demonstrate the fundamental relationship between Points and Vectors
    // when using a Revit Transform.
    //
    // Core relationships:
    //
    // Point B - Point A
    //        ↓
    //      Vector
    //
    // Transform.OfPoint(Point)
    //        ↓
    //    World Point
    //
    // Transform.OfVector(Vector)
    //        ↓
    //    World Vector
    //
    // Important validation:
    //
    // Transform.OfPoint(B) - Transform.OfPoint(A)
    //
    // should equal:
    //
    // Transform.OfVector(B - A)
    //
    // This demonstrates that the Transform Origin affects Points
    // but does not affect Vectors.
    //
    // This command does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class PointVsVectorTransformationCommand : IExternalCommand
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
                        "Select a FamilyInstance to analyze Point vs Vector transformation");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Point vs Vector",
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
                        "Point vs Vector",
                        "Could not obtain the FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Create Two Local Points
                // These points exist in the local coordinate system
                // represented by the Transform.
                //=====================================================

                XYZ localPointA = new XYZ(1, 2, 3);
                XYZ localPointB = new XYZ(5, 7, 9);

                //=====================================================
                // 4. Derive Local Vector From Two Points
                // Vector = B - A
                //=====================================================

                XYZ localVector = localPointB - localPointA;

                //=====================================================
                // 5. Transform Both Points
                //=====================================================

                XYZ worldPointA = transform.OfPoint(localPointA);
                XYZ worldPointB = transform.OfPoint(localPointB);

                //=====================================================
                // 6. Transform the Vector
                //=====================================================

                XYZ worldVector = transform.OfVector(localVector);

                //=====================================================
                // 7. Derive World Vector From Transformed Points
                //=====================================================

                XYZ worldVectorFromPoints = worldPointB - worldPointA;

                //=====================================================
                // 8. Compare Both World Vectors
                //=====================================================

                double vectorDifference = worldVector.DistanceTo(worldVectorFromPoints);

                //=====================================================
                // 9. Normalize Directions
                //=====================================================

                XYZ localDirection = localVector.Normalize();

                XYZ worldDirection = worldVector.Normalize();

                XYZ worldDirectionFromPoints = worldVectorFromPoints.Normalize();

                //=====================================================
                // 10. Length Comparison
                //=====================================================

                double localVectorLength = localVector.GetLength();

                double worldVectorLength = worldVector.GetLength();

                double worldVectorFromPointsLength = worldVectorFromPoints.GetLength();

                //=====================================================
                // 11. Get Transform Origin
                // Used to explain why Point and Vector behave
                // differently.
                //=====================================================

                XYZ origin = transform.Origin;

                //=====================================================
                // 12. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(
                    "POINT vs VECTOR TRANSFORMATION");

                sb.AppendLine(
                    "========================================");

                sb.AppendLine(
                    $"Element Id : {familyInstance.Id}");

                sb.AppendLine(
                    $"Family     : " +
                    $"{familyInstance.Symbol.Family.Name}");

                sb.AppendLine(
                    $"Type       : " +
                    $"{familyInstance.Symbol.Name}");

                sb.AppendLine();

                //=====================================================
                // Transform
                //=====================================================

                sb.AppendLine(
                    "TRANSFORM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Origin:");

                sb.AppendLine(
                    $"  ({origin.X:F4}, " +
                    $"{origin.Y:F4}, " +
                    $"{origin.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Local Points
                //=====================================================

                sb.AppendLine(
                    "LOCAL POINTS");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Point A:");

                sb.AppendLine(
                    $"  ({localPointA.X:F4}, " +
                    $"{localPointA.Y:F4}, " +
                    $"{localPointA.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Point B:");

                sb.AppendLine(
                    $"  ({localPointB.X:F4}, " +
                    $"{localPointB.Y:F4}, " +
                    $"{localPointB.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Local Vector
                //=====================================================

                sb.AppendLine(
                    "LOCAL VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Vector = Point B - Point A");

                sb.AppendLine();

                sb.AppendLine(
                    $"({localVector.X:F4}, " +
                    $"{localVector.Y:F4}, " +
                    $"{localVector.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Length: {localVectorLength:F6}");

                sb.AppendLine();

                sb.AppendLine(
                    "Direction:");

                sb.AppendLine(
                    $"  ({localDirection.X:F6}, " +
                    $"{localDirection.Y:F6}, " +
                    $"{localDirection.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Transformed Points
                //=====================================================

                sb.AppendLine(
                    "TRANSFORMED POINTS");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "World Point A:");

                sb.AppendLine(
                    $"  ({worldPointA.X:F6}, " +
                    $"{worldPointA.Y:F6}, " +
                    $"{worldPointA.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "World Point B:");

                sb.AppendLine(
                    $"  ({worldPointB.X:F6}, " +
                    $"{worldPointB.Y:F6}, " +
                    $"{worldPointB.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // World Vector
                //=====================================================

                sb.AppendLine(
                    "WORLD VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Method 1:");

                sb.AppendLine(
                    "Transform.OfVector(localVector)");

                sb.AppendLine();

                sb.AppendLine(
                    $"({worldVector.X:F6}, " +
                    $"{worldVector.Y:F6}, " +
                    $"{worldVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Length: {worldVectorLength:F6}");

                sb.AppendLine();

                sb.AppendLine(
                    "Method 2:");

                sb.AppendLine(
                    "World Point B - World Point A");

                sb.AppendLine();

                sb.AppendLine(
                    $"({worldVectorFromPoints.X:F6}, " +
                    $"{worldVectorFromPoints.Y:F6}, " +
                    $"{worldVectorFromPoints.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Length: " +
                    $"{worldVectorFromPointsLength:F6}");

                sb.AppendLine();

                //=====================================================
                // World Direction
                //=====================================================

                sb.AppendLine(
                    "WORLD DIRECTIONS");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "From OfVector():");

                sb.AppendLine(
                    $"  ({worldDirection.X:F6}, " +
                    $"{worldDirection.Y:F6}, " +
                    $"{worldDirection.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "From World Points:");

                sb.AppendLine(
                    $"  ({worldDirectionFromPoints.X:F6}, " +
                    $"{worldDirectionFromPoints.Y:F6}, " +
                    $"{worldDirectionFromPoints.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Validation
                //=====================================================

                sb.AppendLine(
                    "VALIDATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Expected:");

                sb.AppendLine(
                    "WorldPointB - WorldPointA");

                sb.AppendLine(
                    "        ≈");

                sb.AppendLine(
                    "Transform.OfVector(PointB - PointA)");

                sb.AppendLine();

                sb.AppendLine(
                    $"Vector Difference: " +
                    $"{vectorDifference:F8}");

                sb.AppendLine();

                sb.AppendLine(
                    "Expected Difference ≈ 0");

                sb.AppendLine();

                //=====================================================
                // Point vs Vector Explanation
                //=====================================================

                sb.AppendLine(
                    "POINT vs VECTOR RULE");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "POINT:");

                sb.AppendLine(
                    "Transform.OfPoint(P)");

                sb.AppendLine(
                    "includes the Transform Origin.");

                sb.AppendLine();

                sb.AppendLine(
                    "VECTOR:");

                sb.AppendLine(
                    "Transform.OfVector(V)");

                sb.AppendLine(
                    "does NOT include the Transform Origin.");

                sb.AppendLine();

                sb.AppendLine(
                    "GEOMETRIC RELATION:");

                sb.AppendLine(
                    "Point B - Point A = Vector");

                sb.AppendLine();

                sb.AppendLine(
                    "Point A + Vector = Point B");

                #endregion

                //=====================================================
                // 13. Display
                //=====================================================

                TaskDialog.Show(
                    "Point vs Vector Transformation",
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
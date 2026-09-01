using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.Fundamentals
{
    // ============================================================================
    // Transform.OfVector Command
    //
    // Command 07
    //
    // Purpose:
    //
    // Demonstrate how Transform.OfVector() transforms a Vector from one
    // coordinate system to another.
    //
    // IMPORTANT DIFFERENCE:
    //
    // OfPoint():
    //
    //     Origin
    //     +
    //     X * BasisX
    //     +
    //     Y * BasisY
    //     +
    //     Z * BasisZ
    //
    // OfVector():
    //
    //     X * BasisX
    //     +
    //     Y * BasisY
    //     +
    //     Z * BasisZ
    //
    // The Transform Origin is NOT added to a Vector transformation.
    //
    // Why?
    //
    // A Point represents a position.
    // A Vector represents direction and magnitude.
    //
    // This command does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 07
    public class TransformOfVectorCommand : IExternalCommand
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
                        "Select a FamilyInstance to analyze Transform.OfVector()");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Transform.OfVector",
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
                        "Transform.OfVector",
                        "Could not obtain the FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Read Transform Coordinate System
                //=====================================================

                XYZ origin = transform.Origin;
                XYZ basisX = transform.BasisX;
                XYZ basisY = transform.BasisY;
                XYZ basisZ = transform.BasisZ;

                //=====================================================
                // 4. Create Local 3D Vector
                // Deliberately uses all three components.
                //=====================================================

                XYZ localVector = new XYZ(2, 3, 4);

                //=====================================================
                // 5. Transform Vector
                //=====================================================

                XYZ transformedVector = transform.OfVector(localVector);

                //=====================================================
                // 6. Manual Calculation
                // Notice:
                // Origin is NOT used.
                // V = X * BasisX + Y * BasisY + Z * BasisZ
                //=====================================================

                XYZ manuallyCalculatedVector =
                    basisX * localVector.X
                    + basisY * localVector.Y
                    + basisZ * localVector.Z;

                //=====================================================
                // 7. Validate
                //=====================================================

                double transformationError = transformedVector.DistanceTo(manuallyCalculatedVector);

                //=====================================================
                // 8. Vector Lengths
                //=====================================================

                double localLength = localVector.GetLength();
                double transformedLength = transformedVector.GetLength();

                //=====================================================
                // 9. Normalized Directions
                //=====================================================

                XYZ localDirection = localVector.Normalize();
                XYZ transformedDirection = transformedVector.Normalize();

                //=====================================================
                // 10. Demonstrate Why Origin Is NOT Used
                // If we incorrectly added Origin to the vector,
                // the result would become a point-like quantity.
                //=====================================================

                XYZ incorrectVectorWithOrigin = origin + transformedVector;

                //=====================================================
                // 11. Build Report
                //=====================================================

                #region Report Structure

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "TRANSFORM.OFVECTOR ANALYSIS");

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
                    "Origin:");

                sb.AppendLine(
                    $"  ({origin.X:F4}, " +
                    $"{origin.Y:F4}, " +
                    $"{origin.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisX:");

                sb.AppendLine(
                    $"  ({basisX.X:F4}, " +
                    $"{basisX.Y:F4}, " +
                    $"{basisX.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisY:");

                sb.AppendLine(
                    $"  ({basisY.X:F4}, " +
                    $"{basisY.Y:F4}, " +
                    $"{basisY.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisZ:");

                sb.AppendLine(
                    $"  ({basisZ.X:F4}, " +
                    $"{basisZ.Y:F4}, " +
                    $"{basisZ.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Local Vector
                //=====================================================

                sb.AppendLine(
                    "LOCAL VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"({localVector.X:F4}, " +
                    $"{localVector.Y:F4}, " +
                    $"{localVector.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Local Vector Length: " +
                    $"{localLength:F6}");

                sb.AppendLine();

                sb.AppendLine(
                    "Local Direction:");

                sb.AppendLine(
                    $"({localDirection.X:F6}, " +
                    $"{localDirection.Y:F6}, " +
                    $"{localDirection.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Transformed Vector
                //=====================================================

                sb.AppendLine(
                    "TRANSFORMED VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Transform.OfVector(localVector):");

                sb.AppendLine(
                    $"({transformedVector.X:F6}, " +
                    $"{transformedVector.Y:F6}, " +
                    $"{transformedVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Transformed Vector Length: " +
                    $"{transformedLength:F6}");

                sb.AppendLine();

                sb.AppendLine(
                    "Transformed Direction:");

                sb.AppendLine(
                    $"({transformedDirection.X:F6}, " +
                    $"{transformedDirection.Y:F6}, " +
                    $"{transformedDirection.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Manual Calculation
                //=====================================================

                sb.AppendLine(
                    "MANUAL CALCULATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "V = X*BasisX + Y*BasisY + Z*BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "Result:");

                sb.AppendLine(
                    $"({manuallyCalculatedVector.X:F6}, " +
                    $"{manuallyCalculatedVector.Y:F6}, " +
                    $"{manuallyCalculatedVector.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Validation
                //=====================================================

                sb.AppendLine(
                    "VALIDATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Transformation Error: " +
                    $"{transformationError:F8}");

                sb.AppendLine();

                sb.AppendLine(
                    "Expected:");

                sb.AppendLine(
                    "Error ≈ 0");

                sb.AppendLine();

                //=====================================================
                // Point vs Vector
                //=====================================================

                sb.AppendLine(
                    "POINT vs VECTOR");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Point transformation:");

                sb.AppendLine(
                    "OfPoint(P) = " +
                    "Origin + X*BasisX + Y*BasisY + Z*BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "Vector transformation:");

                sb.AppendLine(
                    "OfVector(V) = " +
                    "X*BasisX + Y*BasisY + Z*BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "The Transform Origin is NOT added to a Vector.");

                sb.AppendLine();

                //=====================================================
                // Incorrect Example
                //=====================================================

                sb.AppendLine(
                    "WHY ORIGIN IS NOT USED");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Incorrect:");

                sb.AppendLine(
                    "Origin + TransformedVector");

                sb.AppendLine();

                sb.AppendLine(
                    $"Result:");

                sb.AppendLine(
                    $"({incorrectVectorWithOrigin.X:F4}, " +
                    $"{incorrectVectorWithOrigin.Y:F4}, " +
                    $"{incorrectVectorWithOrigin.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    "Adding Origin produces a position-like " +
                    "quantity, not a pure transformed Vector.");
                #endregion

                //=====================================================
                // 12. Display
                //=====================================================

                TaskDialog.Show("Transform.OfVector", sb.ToString());

                return Result.Succeeded;
            }
            catch (OperationCanceledException)
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
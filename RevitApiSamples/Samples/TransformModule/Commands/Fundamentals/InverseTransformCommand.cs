using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands
{
    // ============================================================================
    // Inverse Transform Command
    //
    // Command 09
    //
    // Purpose:
    //
    // Demonstrate how to convert a point/vector from the Transform's
    // target/model coordinate system back into its local coordinate system.
    //
    // Forward:
    //
    // Local Point
    //      ↓
    // Transform.OfPoint()
    //      ↓
    // Model Point
    //
    // Reverse:
    //
    // Model Point
    //      ↓
    // Transform.Inverse
    //      ↓
    // Inverse.OfPoint()
    //      ↓
    // Local Point
    //
    // The same concept applies to Vectors using OfVector().
    //
    // This command does not modify the selected element.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class InverseTransformCommand : IExternalCommand
    {
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
                        "Select a FamilyInstance to analyze Inverse Transform");

                Element element = doc.GetElement(reference);
                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Inverse Transform",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Forward Transform
                //=====================================================

                Transform transform = familyInstance.GetTransform();

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Inverse Transform",
                        "Could not obtain the FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Get Inverse Transform
                //=====================================================

                Transform inverseTransform = transform.Inverse;

                if (inverseTransform == null)
                {
                    TaskDialog.Show(
                        "Inverse Transform",
                        "Could not obtain the inverse Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 4. Create a Local Point
                // This is our original point in the local coordinate
                // system.
                //=====================================================

                XYZ originalLocalPoint = new XYZ(2, 3, 4);

                //=====================================================
                // 5. Forward Transformation
                // Local → Model
                //=====================================================

                XYZ modelPoint = transform.OfPoint(originalLocalPoint);

                //=====================================================
                // 6. Inverse Transformation
                // Model → Local
                //=====================================================

                XYZ reconstructedLocalPoint = inverseTransform.OfPoint(modelPoint);

                //=====================================================
                // 7. Validate Point Round Trip
                //=====================================================

                double pointError = originalLocalPoint.DistanceTo(reconstructedLocalPoint);

                //=====================================================
                // 8. Create a Local Vector
                //=====================================================

                XYZ originalLocalVector = new XYZ(1, 2, 3);

                //=====================================================
                // 9. Forward Vector Transformation
                //=====================================================

                XYZ modelVector = transform.OfVector(originalLocalVector);

                //=====================================================
                // 10. Inverse Vector Transformation
                //=====================================================

                XYZ reconstructedLocalVector = inverseTransform.OfVector(modelVector);

                //=====================================================
                // 11. Validate Vector Round Trip
                //=====================================================

                double vectorError = originalLocalVector.DistanceTo(reconstructedLocalVector);

                //=====================================================
                // 12. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(
                    "INVERSE TRANSFORM ANALYSIS");

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
                // Forward Transform
                //=====================================================

                sb.AppendLine(
                    "FORWARD TRANSFORM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Local → Model");

                sb.AppendLine();

                sb.AppendLine(
                    $"Origin:");

                sb.AppendLine(
                    $"  ({transform.Origin.X:F4}, " +
                    $"{transform.Origin.Y:F4}, " +
                    $"{transform.Origin.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    $"BasisX:");

                sb.AppendLine(
                    $"  ({transform.BasisX.X:F4}, " +
                    $"{transform.BasisX.Y:F4}, " +
                    $"{transform.BasisX.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    $"BasisY:");

                sb.AppendLine(
                    $"  ({transform.BasisY.X:F4}, " +
                    $"{transform.BasisY.Y:F4}, " +
                    $"{transform.BasisY.Z:F4})");

                sb.AppendLine();

                sb.AppendLine(
                    $"BasisZ:");

                sb.AppendLine(
                    $"  ({transform.BasisZ.X:F4}, " +
                    $"{transform.BasisZ.Y:F4}, " +
                    $"{transform.BasisZ.Z:F4})");

                sb.AppendLine();

                //=====================================================
                // Point Transformation
                //=====================================================

                sb.AppendLine(
                    "POINT ROUND TRIP");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Original Local Point:");

                sb.AppendLine(
                    $"  ({originalLocalPoint.X:F6}, " +
                    $"{originalLocalPoint.Y:F6}, " +
                    $"{originalLocalPoint.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "After Forward Transform:");

                sb.AppendLine(
                    $"  ({modelPoint.X:F6}, " +
                    $"{modelPoint.Y:F6}, " +
                    $"{modelPoint.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "After Inverse Transform:");

                sb.AppendLine(
                    $"  ({reconstructedLocalPoint.X:F6}, " +
                    $"{reconstructedLocalPoint.Y:F6}, " +
                    $"{reconstructedLocalPoint.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Point Reconstruction Error:");

                sb.AppendLine(
                    $"  {pointError:F10}");

                sb.AppendLine();

                //=====================================================
                // Vector Transformation
                //=====================================================

                sb.AppendLine(
                    "VECTOR ROUND TRIP");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Original Local Vector:");

                sb.AppendLine(
                    $"  ({originalLocalVector.X:F6}, " +
                    $"{originalLocalVector.Y:F6}, " +
                    $"{originalLocalVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "After Forward Transform:");

                sb.AppendLine(
                    $"  ({modelVector.X:F6}, " +
                    $"{modelVector.Y:F6}, " +
                    $"{modelVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "After Inverse Transform:");

                sb.AppendLine(
                    $"  ({reconstructedLocalVector.X:F6}, " +
                    $"{reconstructedLocalVector.Y:F6}, " +
                    $"{reconstructedLocalVector.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    $"Vector Reconstruction Error:");

                sb.AppendLine(
                    $"  {vectorError:F10}");

                sb.AppendLine();

                //=====================================================
                // Core Concept
                //=====================================================

                sb.AppendLine(
                    "CORE CONCEPT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "FORWARD:");

                sb.AppendLine(
                    "Local Point");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Transform.OfPoint()");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Model Point");

                sb.AppendLine();

                sb.AppendLine(
                    "REVERSE:");

                sb.AppendLine(
                    "Model Point");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Transform.Inverse");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Inverse.OfPoint()");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Local Point");

                sb.AppendLine();

                //=====================================================
                // Validation
                //=====================================================

                sb.AppendLine(
                    "VALIDATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Local Point");

                sb.AppendLine(
                    "    ↓ Forward Transform");

                sb.AppendLine(
                    "Model Point");

                sb.AppendLine(
                    "    ↓ Inverse Transform");

                sb.AppendLine(
                    "Original Local Point");

                sb.AppendLine();

                sb.AppendLine(
                    $"Point Error  : {pointError:F10}");

                sb.AppendLine(
                    $"Vector Error : {vectorError:F10}");

                sb.AppendLine();

                sb.AppendLine(
                    "Expected:");

                sb.AppendLine(
                    "Both errors ≈ 0");

                #endregion

                //=====================================================
                // 13. Display
                //=====================================================

                TaskDialog.Show("Inverse Transform", sb.ToString());
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
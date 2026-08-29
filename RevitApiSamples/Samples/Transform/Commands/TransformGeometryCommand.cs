using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Note: Transform is a 4x4 matrix encoding translation (Origin), 
    // rotation (BasisX/Y/Z), and scale.
    // ============================================================================

    // Command 06
    [Transaction(TransactionMode.ReadOnly)]
    public class TransformGeometryCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // Select Element
                //=====================================================
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select a FamilyInstance to read its Transform");
                Element element = doc.GetElement(selRef);

                if (!(element is FamilyInstance familyInstance))
                {
                    TaskDialog.Show("Error", "Selected element is not a FamilyInstance.");
                    return Result.Cancelled;
                }

                //=====================================================
                // Get Transform
                //=====================================================
                Autodesk.Revit.DB.Transform t = familyInstance.GetTransform();

                string resultStr = "Transform Info:\n========================================\n";
                resultStr += $"Origin: {XYZToString(t.Origin)}\n";
                resultStr += $"BasisX: {XYZToString(t.BasisX)}\n";
                resultStr += $"BasisY: {XYZToString(t.BasisY)}\n";
                resultStr += $"BasisZ: {XYZToString(t.BasisZ)}\n";
                resultStr += $"IsIdentity: {t.IsIdentity}\n";
                resultStr += $"Scale: {t.Scale}\n\n";

                //=====================================================
                // Apply Transform
                //=====================================================
                XYZ localPoint = new XYZ(1, 0, 0);
                XYZ worldPoint = t.OfPoint(localPoint);

                resultStr += "Coordinate Transformation:\n========================================\n";
                resultStr += $"Local Point: {XYZToString(localPoint)}\n";
                resultStr += $"World Point: {XYZToString(worldPoint)}\n";

                TaskDialog.Show("Transform Result", resultStr);

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

        private string XYZToString(XYZ point)
        {
            return $"({point.X:F2}, {point.Y:F2}, {point.Z:F2})";
        }
    }
}

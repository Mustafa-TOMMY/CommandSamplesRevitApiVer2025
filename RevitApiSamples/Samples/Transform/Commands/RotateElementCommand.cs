using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Note: Rotation angle is in RADIANS. 
    // Axis is a Line through the element's own location, pointing up (Z).
    // ============================================================================

    // Command 04
    [Transaction(TransactionMode.Manual)]
    public class RotateElementCommand : IExternalCommand
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
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select a FamilyInstance to rotate");
                Element element = doc.GetElement(selRef);

                if (!(element.Location is LocationPoint locPoint))
                {
                    TaskDialog.Show("Error", "Selected element does not have a LocationPoint.");
                    return Result.Cancelled;
                }

                //=====================================================
                // Define Rotation Axis and Angle
                //=====================================================
                XYZ location = locPoint.Point;
                Line axisLine = Line.CreateBound(location, location + XYZ.BasisZ);
                double angleInRadians = Math.PI / 4.0; // 45 degrees

                //=====================================================
                // Rotate Element
                //=====================================================
                using (Transaction t = new Transaction(doc, "Rotate Element"))
                {
                    t.Start();
                    ElementTransformUtils.RotateElement(doc, element.Id, axisLine, angleInRadians);
                    t.Commit();
                }

                TaskDialog.Show("Rotate Result", $"Element {element.Id} rotated by 45 degrees.");

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

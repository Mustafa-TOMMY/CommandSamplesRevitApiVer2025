using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 08
    public class PickingFaceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //==============================

                // pick an element from the face
                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Face,
                    "Select a face");
                Element element = doc.GetElement(reference);

                TaskDialog.Show(
                    "Face Selection",
                    $"Element Id : {element.Id}\n" +
                    $"Reference Type : Face");

                // get the face from the element
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                Face face = geometryObject as Face;

                TaskDialog.Show(
                "Face",
                $"Area : {face.Area}");

                //==============================

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

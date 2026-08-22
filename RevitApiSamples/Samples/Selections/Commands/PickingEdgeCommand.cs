using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 09
    public class PickingEdgeCommand : IExternalCommand
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

                // pick an element from the edge
                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Edge,
                        "Select an edge");
                Element element = doc.GetElement(reference);

                TaskDialog.Show(
                    "Edge Selection",
                    $"Element Id : {element.Id}\n" +
                    $"Reference Type : Edge");

                // get the edge from the element
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                Edge edge = geometryObject as Edge;

                TaskDialog.Show(
                    "Edge",
                    $"Length : {edge.ApproximateLength:F3}");

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

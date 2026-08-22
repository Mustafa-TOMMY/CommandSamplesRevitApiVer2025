using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Selections.Filters;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 07
    public class PickingPointCommand : IExternalCommand
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

                // without snap points
                XYZ point = uiDoc.Selection.PickPoint("Pick a point");

                TaskDialog.Show(
                    "Selection",
                    $"X : {point.X:F3}\n" +
                    $"Y : {point.Y:F3}\n" +
                    $"Z : {point.Z:F3}");

                // with snap points
                XYZ snappedPoint = uiDoc.Selection.PickPoint(
                    ObjectSnapTypes.Endpoints | ObjectSnapTypes.Intersections,
                    "Pick an endpoint or intersection");

                TaskDialog.Show(
                    "Snapped Point",
                    $"X : {snappedPoint.X:F3}\n" +
                    $"Y : {snappedPoint.Y:F3}\n" +
                    $"Z : {snappedPoint.Z:F3}");

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

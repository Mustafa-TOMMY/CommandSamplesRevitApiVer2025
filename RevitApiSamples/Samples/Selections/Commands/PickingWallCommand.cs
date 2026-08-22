using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Selections.Filters;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 04
    public class PickingWallCommand : IExternalCommand
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

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    new WallSelectionFilter(),
                    "Select a wall");

                Element wall = doc.GetElement(reference);

                TaskDialog.Show(
                    "Selection",
                    $"Wall Id : {wall.Id}");

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

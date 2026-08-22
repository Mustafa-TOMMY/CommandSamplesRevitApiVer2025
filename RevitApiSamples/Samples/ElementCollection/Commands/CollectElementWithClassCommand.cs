using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class CollectElementWithClassCommand : IExternalCommand
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

                FilteredElementCollector collector = new FilteredElementCollector(doc);

                List<Wall> walls = collector
                            .OfClass(typeof(Wall))
                            .Cast<Wall>()
                            .ToList();

                TaskDialog.Show(
                    "OfClass",
                    $"Walls Count : {walls.Count}");


                /*
                 * .OfClass(typeof(Wall))
                 * .OfClass(typeof(Level))
                 * .OfClass(typeof(View))
                 * .OfClass(typeof(Grid))
                 * .OfClass(typeof(Family))
                 * .OfClass(typeof(RevitLinkInstance))
                 */

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

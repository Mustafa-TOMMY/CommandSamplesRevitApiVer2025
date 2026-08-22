using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class CollectAllElementsCommand : IExternalCommand
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

                // get all DataBase of the document
                FilteredElementCollector collector = new FilteredElementCollector(doc);

                List<Element> allElements = collector.ToElements().ToList();

                TaskDialog.Show(
                    "Collector",
                    $"Elements Count : {allElements.Count}");

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

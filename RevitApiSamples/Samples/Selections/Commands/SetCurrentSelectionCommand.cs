using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 13
    public class SetCurrentSelectionCommand : IExternalCommand
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

                // Get the first 5 walls only.
                ICollection<ElementId> wallIds = new FilteredElementCollector(doc)
                        .OfClass(typeof(Wall))
                        .WhereElementIsNotElementType()
                        .Take(5)
                        .Select(wall => wall.Id)
                        .ToList();

                uiDoc.Selection.SetElementIds(wallIds);

                if (!wallIds.Any())
                {
                    TaskDialog.Show(
                        "Selection",
                        "No walls found.");

                    return Result.Succeeded;
                }
                TaskDialog.Show(
                    "Selection",
                    $"{wallIds.Count} walls have been selected.");

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

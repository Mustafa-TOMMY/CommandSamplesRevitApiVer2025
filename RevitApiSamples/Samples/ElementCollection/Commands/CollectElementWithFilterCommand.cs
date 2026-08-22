using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class CollectElementWithFilterCommand : IExternalCommand
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

                ElementCategoryFilter wallFilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                List<Element> wallsWithCategoryFilter = new FilteredElementCollector(doc)
                    .WherePasses(wallFilter) // FilteredElementCollector.WherePasses() method is used to filter elements based on a specified filter.
                    .WhereElementIsNotElementType()
                    .ToList();

                TaskDialog.Show(
                    "WherePasses",
                    $"Walls Count : {wallsWithCategoryFilter.Count}");

                // ========================================================================

                ElementClassFilter classFilter = new ElementClassFilter(typeof(Wall));
                List<Element> wallsWithClassFilter = new FilteredElementCollector(doc)
                    .WherePasses(classFilter)
                    .WhereElementIsNotElementType()
                    .ToList();

                TaskDialog.Show(
                    "WherePasses",
                    $"Walls Count : {wallsWithClassFilter.Count}");

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

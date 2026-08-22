using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 04
    public class CollectElementTypeOrInstanceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message,ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;
                //==============================

                // ElementType : Represents a type of element in the Revit database.
                FilteredElementCollector TypeCollector = new FilteredElementCollector(doc);
                List<ElementType> wallTypes = TypeCollector
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsElementType() // get only element types
                    .Cast<ElementType>()
                    .ToList();
                TaskDialog.Show(
                    "Element Types",
                    $"Wall Types : {wallTypes.Count}");

                // ElementInstance : Represents an instance of an element in the Revit database.
                FilteredElementCollector instanceCollector = new FilteredElementCollector(doc);
                List<Wall> walls = instanceCollector
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType() // get only element instances
                    .Cast<Wall>()
                    .ToList();
                TaskDialog.Show(
                    "Element Instances",
                    $"Walls : {walls.Count}");

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

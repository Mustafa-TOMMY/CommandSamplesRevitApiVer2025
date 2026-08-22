using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class CollectElementWithCategoryCommand : IExternalCommand
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

                List<Element> walls = collector
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .ToList();

                TaskDialog.Show(
                    "OfCategory",
                    $"Walls Count : {walls.Count}");


                /* return list of elements based on category
                 * .OfCategory(BuiltInCategory.OST_Walls)
                 * .OfCategory(BuiltInCategory.OST_Doors)
                 * .OfCategory(BuiltInCategory.OST_Furniture)
                 * .OfCategory(BuiltInCategory.OST_GenericModel)
                 * .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Selections.Filters;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class PickingElementByCategoryCommand
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
                    new ElementCategorySelectionFilter(BuiltInCategory.OST_Doors),
                    "Select a door");

                Element element = doc.GetElement(reference);

                TaskDialog.Show(
                    "Selection",
                    $"Door Id : {element.Id}");

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

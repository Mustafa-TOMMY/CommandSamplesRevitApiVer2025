using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Selections.Filters;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class PickingMultipleElementsByCategoryCommand : IExternalCommand
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

                IList<Reference> references = uiDoc.Selection.PickObjects(
                    ObjectType.Element,
                    new ElementCategorySelectionFilter(BuiltInCategory.OST_Doors),
                    "Select multiple doors");

                IList<Element> doors = references
                    .Select(doc.GetElement)
                    .ToList();

                TaskDialog.Show(
                    "Selection",
                    $"Selected Doors : {doors.Count}");

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

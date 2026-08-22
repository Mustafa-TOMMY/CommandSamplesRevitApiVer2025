using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class PickingElementCommand : IExternalCommand
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
                                    "Select any element");

                Element element = doc.GetElement(reference);

                TaskDialog.Show(
                    "Selection",
                    $"Id: {element.Id}\n" +
                    $"Name: {element.Name}\n" +
                    $"Category: {element.Category?.Name}");

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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class CollectElementsInViewCommand
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

                // get all elements in the active view (for example level 01)
                View activeView = doc.ActiveView;

                List<Element> elementsInView = new FilteredElementCollector(doc, activeView.Id)
                    .WhereElementIsNotElementType()
                    .ToList();

                TaskDialog.Show(
                    "Collector",
                    $"Elements in View : {elementsInView.Count}");

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

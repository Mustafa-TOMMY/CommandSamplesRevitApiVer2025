using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApiSamples.Samples.Selections.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 10
    public class PickLinkedElement : IExternalCommand
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
                            ObjectType.LinkedElement,
                            "Select an element from a Revit Link");

                RevitLinkInstance linkInstance = doc.GetElement(reference) as RevitLinkInstance;
                Document linkedDocument = linkInstance.GetLinkDocument();
                Element linkedElement = linkedDocument.GetElement(reference.LinkedElementId);

                TaskDialog.Show(
                    "Linked Element",
                    $"Link Instance : {linkInstance.Name}\n" +
                    $"Element Id : {linkedElement.Id}\n" +
                    $"Category : {linkedElement.Category?.Name}");

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

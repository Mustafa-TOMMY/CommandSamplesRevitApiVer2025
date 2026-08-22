using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ElementCollection.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CollectFamilySymbolsByPlacementTypeCommand : IExternalCommand
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

                FamilySymbol familySymbol = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(symbol =>
                                symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased);

                TaskDialog.Show(
                    "Face-Based Family",
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Type: {familySymbol.Name}\n" +
                    $"Element Id: {familySymbol.Id}");

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

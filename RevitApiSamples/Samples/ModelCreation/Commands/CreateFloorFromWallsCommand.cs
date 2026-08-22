using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitApiSamples.Samples.ModelCreation.Helpers;
using RevitApiSamples.Samples.Selections.Filters;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    [Transaction(TransactionMode.Manual)]
    // Command 07
    public class CreateFloorFromWallsCommand : IExternalCommand
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

                FloorType floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault()!;

                Level level = doc.ActiveView.GenLevel;

                if (floorType == null || level == null)
                {
                    TaskDialog.Show(
                        "Create Profile",
                        "Floor type or Level not found.");

                    return Result.Failed;
                }

                List<Wall> walls = uiDoc.Selection.PickElementsByRectangle(
                    new WallSelectionFilter())
                    .Cast<Wall>()
                    .ToList();

                List<Curve> curves = walls
                    .Select(w => w.Location)
                    .OfType<LocationCurve>()
                    .Select(lc => lc.Curve)
                    .ToList();

                List<CurveLoop> loops = CurveLoopBuilder.Build(curves);

                using (Transaction tx = new Transaction(doc, "Create Floors"))
                {
                    tx.Start();

                    foreach (CurveLoop loop in loops)
                    {
                        Floor.Create(
                            doc,
                            new List<CurveLoop> { loop },
                            floorType.Id,
                            level.Id);
                    }

                    tx.Commit();
                }

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

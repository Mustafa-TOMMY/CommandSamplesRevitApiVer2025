using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    [Transaction(TransactionMode.Manual)]
    // Command 01
    public class CreateWallCommand : IExternalCommand
    {
        // ============================================================================
        // Curve-Based Element Creation
        //
        // This command demonstrates how to create elements that are defined by a Curve
        // (typically a Line). The workflow is:
        // Start Point -> End Point -> Curve -> Create(...)
        //
        // Common Curve-Based Elements:
        // - Wall
        // - Beam
        // - Brace
        // - Pipe
        // - Duct
        // - Conduit
        // - Cable Tray
        //
        // Once you understand this pattern, creating the elements above is mainly a
        // matter of calling a different Create() method with the required parameters.
        // ============================================================================
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //==============================

                // get first wall type in the document
                WallType wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault();

                // get the active level in the document
                Level level = doc.ActiveView.GenLevel;

                if (wallType == null || level == null)
                {
                    TaskDialog.Show(
                        "Create Wall",
                        "WallType or Level not found.");

                    return Result.Failed;
                }

                // user pick two points to create the wall
                XYZ startPoint = uiDoc.Selection.PickPoint("Pick first point");
                XYZ endPoint = uiDoc.Selection.PickPoint("Pick second point");

                Line wallLine = Line.CreateBound(startPoint, endPoint);

                //=====================================================

                using (Transaction transaction = new Transaction(doc, "Create Wall"))
                {
                    transaction.Start();

                    Wall.Create(
                        doc,
                        wallLine,
                        wallType.Id,
                        level.Id,
                        10,
                        0,
                        false,
                        false);

                    transaction.Commit();
                }

                TaskDialog.Show(
                    "Create Wall",
                    "Wall created successfully.");

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
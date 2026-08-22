using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    [Transaction(TransactionMode.Manual)]
    // Command 06
    public class CreateFloorByRectangleCommand : IExternalCommand
    {
        // ============================================================================
        // Sketch-Based Element Creation (Rectangle)
        //
        // This command demonstrates how to create a rectangular sketch profile from
        // two user-picked corner points.
        //
        // Workflow:
        // Pick Point 1 -> Pick Point 2 -> Rectangle -> CurveLoop -> Create(...)
        //
        //
        // Common Use Cases:
        //
        // - Rectangular Floor
        // - Rectangular Roof
        // - Rectangular Ceiling
        // - Rectangular Opening
        // - Rectangular Filled Region
        //
        // Unlike the previous sample, the user does not need to pick every corner.
        // The remaining two corners are calculated automatically.
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

                FloorType floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault();

                Level level = doc.ActiveView.GenLevel;

                if (floorType == null || level == null)
                {
                    TaskDialog.Show(
                        "Create Profile",
                        "Floor type or Level not found.");

                    return Result.Failed;
                }

                XYZ p1 = uiDoc.Selection.PickPoint("Pick first corner");
                XYZ p3 = uiDoc.Selection.PickPoint("Pick opposite corner");
                XYZ p2 = new XYZ(p3.X, p1.Y, p1.Z);
                XYZ p4 = new XYZ(p1.X, p3.Y, p1.Z);

                CurveLoop profile = new CurveLoop();

                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p4));
                profile.Append(Line.CreateBound(p4, p1));

                using (Transaction transaction = new Transaction(doc, "Create Floor"))
                {
                    transaction.Start();

                    Floor.Create(
                        doc,
                        new List<CurveLoop> { profile },
                        floorType.Id,
                        level.Id);

                    transaction.Commit();
                }

                TaskDialog.Show(
                    "Success",
                    "Floor created successfully.");


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

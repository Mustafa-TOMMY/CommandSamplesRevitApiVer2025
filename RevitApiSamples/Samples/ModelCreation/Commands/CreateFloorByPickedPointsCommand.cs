using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    // ============================================================================
    // Sketch-Based Element Creation
    //
    // This command demonstrates how to create elements that require a closed sketch
    // profile (CurveLoop).
    //
    // Workflow:
    // Points -> Curves -> Closed CurveLoop -> Create(...)
    //
    // Common Sketch-Based Elements:
    //
    // - Floor
    // - Roof
    // - Ceiling
    // - Filled Region (2D)
    // - Openings (some overloads)
    //
    // Unlike Curve-Based elements, Sketch-Based elements require a closed boundary.
    // ============================================================================
    [Transaction(TransactionMode.Manual)]
    // Command 05
    public class CreateFloorByPickedPointsCommand : IExternalCommand
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
                    .FirstOrDefault();

                Level level = doc.ActiveView.GenLevel;

                if (floorType == null || level == null)
                {
                    TaskDialog.Show(
                        "Create Profile",
                        "Floor type or Level not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Pick Points

                List<XYZ> points = new List<XYZ>();
                while (true)
                {
                    try
                    {
                        XYZ point = uiDoc.Selection.PickPoint("Pick profile points - Press ESC when finished");

                        points.Add(point);
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break;
                    }
                }

                // Validation
                if (points.Count < 3)
                {
                    TaskDialog.Show(
                        "Validation",
                        "A closed profile requires at least 3 points.");

                    return Result.Cancelled;
                }

                //=====================================================
                // Build CurveLoop

                CurveLoop profile = new CurveLoop();

                // 5 % 2 = 1
                // 5 % 3 = 2
                // 2 % 5 = 2
                // 3 % 5 = 3
                // 5 % 5 = 0
                for (int i = 0; i < points.Count; i++)
                {
                    XYZ start = points[i];
                    XYZ end = points[(i + 1) % points.Count];

                    profile.Append(Line.CreateBound(start, end));
                }

                // Validation

                if (!profile.HasPlane())
                {
                    TaskDialog.Show(
                        "Validation",
                        "The selected profile is not planar.");

                    return Result.Cancelled;
                }

                // IsOpen() ==> alway return false for a closed profile, but we can check it anyway
                if (profile.IsOpen())
                {
                    TaskDialog.Show(
                        "Validation",
                        "The profile is not closed.");

                    return Result.Cancelled;
                }

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

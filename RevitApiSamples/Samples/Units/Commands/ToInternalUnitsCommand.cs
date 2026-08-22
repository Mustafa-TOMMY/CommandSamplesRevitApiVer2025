using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Units.Commands
{
    // ============================================================================
    // Convert To Internal Units
    //
    // This command demonstrates how to convert a value from a user-facing unit
    // into Revit's internal unit before using it with the API.
    //
    // Workflow:
    //
    // User Value in Meters
    //      ↓
    // UnitUtils.ConvertToInternalUnits()
    //      ↓
    // Internal Value
    //      ↓
    // Parameter.Set()
    //
    // Example:
    //
    // 3 meters
    //      ↓
    // Revit Internal Units
    //      ↓
    // Set Wall Height
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 02
    public class ToInternalUnitsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;
                var app = uiApp.Application;

                //=====================================================
                // Select Wall

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall");

                Wall wall = doc.GetElement(reference) as Wall;

                if (wall == null)
                {
                    TaskDialog.Show(
                        "To Internal Units",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Wall Height Parameter

                Parameter heightParameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (heightParameter == null)
                {
                    TaskDialog.Show(
                        "To Internal Units",
                        "Wall height parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Check if Parameter can be modified

                if (heightParameter.IsReadOnly)
                {
                    TaskDialog.Show(
                        "To Internal Units",
                        "The wall height parameter is read-only.");

                    return Result.Failed;
                }

                //=====================================================
                // Check Storage Type

                if (heightParameter.StorageType != StorageType.Double)
                {
                    TaskDialog.Show(
                        "To Internal Units",
                        "The wall height parameter is not a Double.");

                    return Result.Failed;
                }

                //=====================================================
                // User Value
                //
                // The user thinks in meters.
                //
                // Example:
                // 3 meters

                double heightMeters = 3.0;

                //=====================================================
                // Convert Meters → Revit Internal Units

                double heightInternal = UnitUtils.ConvertToInternalUnits(heightMeters, UnitTypeId.Meters);

                //=====================================================
                // Set Parameter

                using (Transaction transaction = new Transaction(doc, "Set Wall Height"))
                {
                    transaction.Start();

                    heightParameter.Set(heightInternal);

                    transaction.Commit();
                }

                //=====================================================
                // Read Result From Revit

                double resultInternal = heightParameter.AsDouble();

                double resultMeters = UnitUtils.ConvertFromInternalUnits(resultInternal, UnitTypeId.Meters);

                //=====================================================

                TaskDialog.Show(
                    "To Internal Units",
                    $"Wall: {wall.Name}\n\n" +
                    $"Input:\n" +
                    $"{heightMeters:F3} m\n\n" +
                    $"Internal Value:\n" +
                    $"{heightInternal:F6} ft\n\n" +
                    $"Result:\n" +
                    $"{resultMeters:F3} m");

                //=====================================================

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
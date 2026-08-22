using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Set Parameter
    //
    // This command demonstrates how to modify a parameter value.
    //
    // Workflow:
    //
    // Select Wall
    //      ↓
    // Get BuiltInParameter
    //      ↓
    // Check Parameter
    //      ↓
    // Check IsReadOnly
    //      ↓
    // Check StorageType
    //      ↓
    // Start Transaction
    //      ↓
    // Set(...)
    //      ↓
    // Commit
    //      ↓
    // Read Updated Value
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 04
    public class SetParameterCommand : IExternalCommand
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
                        "Set Parameter",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Parameter

                Parameter heightParameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (heightParameter == null)
                {
                    TaskDialog.Show(
                        "Set Parameter",
                        "Wall height parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Check ReadOnly

                if (heightParameter.IsReadOnly)
                {
                    TaskDialog.Show(
                        "Set Parameter",
                        "The parameter is read-only.");

                    return Result.Failed;
                }

                //=====================================================
                // Check StorageType

                if (heightParameter.StorageType != StorageType.Double)
                {
                    TaskDialog.Show(
                        "Set Parameter",
                        "The parameter does not use Double storage.");

                    return Result.Failed;
                }

                //=====================================================
                // New Value
                //
                // Revit internal length unit = feet.
                //
                // Example:
                // 10 feet

                double newHeight = 3.0;
                double hightInMeters = UnitUtils.ConvertFromInternalUnits(newHeight, UnitTypeId.Meters);

                //=====================================================
                // Set Parameter

                using (Transaction transaction = new Transaction(doc, "Set Wall Height"))
                {
                    transaction.Start();

                    heightParameter.Set(hightInMeters);

                    transaction.Commit();
                }

                //=====================================================
                // Read Updated Value

                double updatedHeight = heightParameter.AsDouble();

                TaskDialog.Show(
                    "Set Parameter",
                    $"Wall: {wall.Name}\n" +
                    $"Parameter: {heightParameter.Definition.Name}\n" +
                    $"New Height: {updatedHeight:F3} ft"); // expected output: 9.843 ft (3 m)

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
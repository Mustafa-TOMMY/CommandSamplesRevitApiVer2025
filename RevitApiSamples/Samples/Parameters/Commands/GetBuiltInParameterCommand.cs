using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Parameters.Helper;
using System;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Get Parameter by BuiltInParameter
    //
    // This command demonstrates how to retrieve a built-in parameter using
    // Revit's predefined BuiltInParameter identifier.
    //
    // Workflow:
    //
    // Select Wall
    //      ↓
    // BuiltInParameter
    //      ↓
    // get_Parameter()
    //      ↓
    // Parameter
    //
    // This is different from LookupParameter(), which searches by parameter name.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class GetBuiltInParameterCommand : IExternalCommand
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
                        "BuiltInParameter",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Built-In Parameter

                Parameter parameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (parameter == null)
                {
                    TaskDialog.Show(
                        "BuiltInParameter",
                        "The parameter was not found.");

                    return Result.Succeeded;
                }

                //=====================================================
                // Read Parameter

                string value = ParameterValueHelper.GetParameterValue(parameter);

                TaskDialog.Show(
                    "BuiltInParameter",
                    $"Parameter: {parameter.Definition.Name}\n" +
                    $"BuiltInParameter: WALL_USER_HEIGHT_PARAM\n" +
                    $"Storage Type: {parameter.StorageType}\n" +
                    $"Read Only: {parameter.IsReadOnly}\n" +
                    $"Value: {value}");

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
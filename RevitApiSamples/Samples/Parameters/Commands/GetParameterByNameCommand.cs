using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Parameters.Helper;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Get Parameter By Name
    //
    // This command demonstrates how to retrieve a specific parameter
    // using its visible parameter name.
    //
    // Workflow:
    //
    // Select Element
    //      ↓
    // Parameter Name
    //      ↓
    // LookupParameter()
    //      ↓
    // Parameter
    //
    // Important:
    // LookupParameter() searches using the parameter's name.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class GetParameterByNameCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;
                var app = uiApp.Application;

                //=====================================================
                // Select Element

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element");

                Element element = doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Parameter",
                        "Element not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Parameter Name

                string parameterName = "Width";

                //=====================================================
                // Get Parameter By Name

                Parameter parameter = element.LookupParameter(parameterName);

                if (parameter == null)
                {
                    TaskDialog.Show(
                        "Parameter",
                        $"Parameter '{parameterName}' was not found.");

                    return Result.Succeeded;
                }

                //=====================================================
                // Display Parameter Information

                string value = ParameterValueHelper.GetParameterValue(parameter);

                TaskDialog.Show(
                    "Parameter",
                    $"Element: {element.Name}\n\n" +
                    $"Parameter: {parameter.Definition.Name}\n" +
                    $"Storage Type: {parameter.StorageType}\n" +
                    $"Read Only: {parameter.IsReadOnly}\n" +
                    $"Has Value: {parameter.HasValue}\n" +
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
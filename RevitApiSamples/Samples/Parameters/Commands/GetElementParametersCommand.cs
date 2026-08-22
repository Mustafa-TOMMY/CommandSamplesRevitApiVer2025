using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Parameters.Helper;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Get Element Parameters
    //
    // This command demonstrates how to access all parameters belonging to
    // a selected Element.
    //
    // Workflow:
    //
    // Select Element
    //      ↓
    // Element.Parameters
    //      ↓
    // ParameterSet
    //      ↓
    // Parameter
    //
    // For each Parameter we inspect:
    //
    // - Definition.Name
    // - StorageType
    // - IsReadOnly
    // - HasValue
    // - Value
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class GetElementParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //=====================================================
                // Select Element

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element");

                Element element = doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Parameters",
                        "Element not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Parameters

                ParameterSet parameters = element.Parameters;

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Element: {element.Name}");
                sb.AppendLine($"Element Id: {element.Id}");
                sb.AppendLine($"Parameters Count: {parameters.Size}");
                sb.AppendLine();
                sb.AppendLine("========================================");

                foreach (Parameter parameter in parameters)
                {
                    string name = parameter.Definition?.Name ?? "Unnamed";

                    string value = ParameterValueHelper.GetParameterValue(parameter);

                    sb.AppendLine(
                        $"Name        : {name}\n" +
                        $"StorageType : {parameter.StorageType}\n" +
                        $"ReadOnly    : {parameter.IsReadOnly}\n" +
                        $"HasValue    : {parameter.HasValue}\n" +
                        $"Value       : {value}");

                    sb.AppendLine(
                        "----------------------------------------");
                }

                TaskDialog.Show(
                    "Element Parameters",
                    sb.ToString());

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
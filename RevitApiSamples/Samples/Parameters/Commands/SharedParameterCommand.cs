using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitApiSamples.Samples.Parameters.Helper;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Shared Parameters
    //
    // This command demonstrates how to identify Shared Parameters.
    //
    // A Shared Parameter:
    //
    // - Is defined independently from a specific Family.
    // - Has a unique GUID.
    // - Can be reused across different Families and Projects.
    //
    // Workflow:
    //
    // Select Element
    //      ↓
    // Get Parameters
    //      ↓
    // Check Parameter.IsShared
    //      ↓
    // Get Parameter GUID
    //      ↓
    // Display Shared Parameter Information
    //
    // Important:
    //
    // Built-in Parameter
    //      → Defined by Revit
    //      → Identified through BuiltInParameter
    //
    // Shared Parameter
    //      → User/organization defined
    //      → Identified by GUID
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class SharedParameterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
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
                        "Shared Parameter",
                        "Element was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Inspect Parameters

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Element: {element.Name}");
                sb.AppendLine();
                sb.AppendLine("Shared Parameters:");
                sb.AppendLine("========================================");

                int sharedParameterCount = 0;

                foreach (Parameter parameter in element.Parameters)
                {
                    //=================================================
                    // Check if Parameter is Shared

                    if (!parameter.IsShared)
                        continue;

                    sharedParameterCount++;

                    // Get Shared Parameter GUID
                    Guid guid = parameter.GUID;

                    sb.AppendLine($"Name:\n" + $"{parameter.Definition.Name}");
                    sb.AppendLine($"GUID:\n" + $"{guid}");
                    sb.AppendLine($"Storage Type:\n" + $"{parameter.StorageType}");
                    sb.AppendLine($"Value:\n" + $"{ParameterValueHelper.GetParameterValue(parameter)}");
                    sb.AppendLine("----------------------------------------");
                }

                //=====================================================

                if (sharedParameterCount == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("No Shared Parameters found.");
                }
                else
                {
                    sb.Insert(
                        sb.ToString().IndexOf("Shared Parameters:") + "Shared Parameters:".Length,
                        $"\nCount: {sharedParameterCount}\n");
                }

                TaskDialog.Show("Shared Parameter", sb.ToString());

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
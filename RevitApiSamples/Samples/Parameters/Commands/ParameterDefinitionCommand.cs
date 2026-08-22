using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Parameter Definition
    //
    // This command demonstrates the relationship between:
    //
    // Parameter
    //      ↓
    // Definition
    //      ↓
    // Name
    // Data Type
    // ForgeTypeId
    //
    // A Parameter represents a value attached to an Element.
    //
    // The Definition describes what that Parameter represents.
    //
    // Workflow:
    //
    // Select Element
    //      ↓
    // Select / Find Parameter
    //      ↓
    // Parameter.Definition
    //      ↓
    // Inspect Definition
    //      ↓
    // GetDataType()
    //
    // This command connects the Parameters Module with the Units Module.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 09
    public class ParameterDefinitionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
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
                        "Parameter Definition",
                        "Element was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Select Parameter
                // For this educational sample we use the first
                // parameter that has a Definition.

                Parameter parameter = null;

                foreach (Parameter candidate in element.Parameters)
                {
                    if (candidate?.Definition != null)
                    {
                        parameter = candidate;
                        break;
                    }
                }

                if (parameter == null)
                {
                    TaskDialog.Show(
                        "Parameter Definition",
                        "No parameter with a valid Definition was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Definition

                Definition definition = parameter.Definition;

                //=====================================================
                // Get Data Type

                ForgeTypeId dataType = definition.GetDataType();

                //=====================================================
                // Check Measurable Spec

                bool isMeasurable = UnitUtils.IsMeasurableSpec(dataType);

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Element:\n{element.Name}");
                sb.AppendLine();
                sb.AppendLine("================ PARAMETER ================");
                sb.AppendLine($"Name:\n{definition.Name}");
                sb.AppendLine();
                sb.AppendLine($"Storage Type:\n{parameter.StorageType}");
                sb.AppendLine();
                sb.AppendLine($"Has Value:\n{parameter.HasValue}");
                sb.AppendLine();
                sb.AppendLine("================ DEFINITION ================");
                sb.AppendLine($"Definition Name:\n{definition.Name}");
                sb.AppendLine();
                sb.AppendLine($"Data Type TypeId:\n{dataType.TypeId}");
                sb.AppendLine();
                sb.AppendLine($"Is Measurable Spec:\n{isMeasurable}");
                sb.AppendLine();
                sb.AppendLine($"Is Shared:\n{parameter.IsShared}");

                //=====================================================
                // If measurable, show valid units

                if (isMeasurable)
                {
                    IList<ForgeTypeId> validUnits = UnitUtils.GetValidUnits(dataType);

                    sb.AppendLine();

                    sb.AppendLine($"Valid Units Count:\n{validUnits.Count}");
                }

                //=====================================================

                TaskDialog.Show("Parameter Definition",sb.ToString());

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
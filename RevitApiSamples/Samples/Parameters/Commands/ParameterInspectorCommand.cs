using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Parameter Inspector
    //
    // This command provides a complete inspection of the Parameters
    // available on a selected Element.
    //
    // The goal is to combine the concepts learned throughout the
    // Parameters Module:
    //
    // Element
    //      ↓
    // Parameters
    //      ↓
    // Parameter
    //      ├── Definition
    //      ├── StorageType
    //      ├── Value
    //      ├── IsReadOnly
    //      ├── IsShared
    //      └── DataType
    //
    // This command is intentionally read-only.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 10
    public class ParameterInspectorCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
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
                    "Select an element to inspect its parameters");

                Element element =
                    doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Parameter Inspector",
                        "Element was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Build Result

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "PARAMETER INSPECTOR");

                sb.AppendLine(
                    "========================================");

                sb.AppendLine(
                    $"Element: {element.Name}");

                sb.AppendLine(
                    $"Category: {element.Category?.Name ?? "None"}");

                sb.AppendLine(
                    $"Element Id: {element.Id.IntegerValue}");

                sb.AppendLine();

                int parameterCount = 0;

                //=====================================================
                // Inspect Parameters

                foreach (Parameter parameter in element.Parameters)
                {
                    if (parameter == null)
                        continue;

                    parameterCount++;

                    Definition definition = parameter.Definition;

                    //=================================================
                    // Basic Information

                    sb.AppendLine(
                        $"[{parameterCount}] " +
                        $"{definition?.Name ?? "<No Definition>"}");

                    sb.AppendLine(
                        $"  Storage Type : {parameter.StorageType}");

                    sb.AppendLine(
                        $"  Has Value    : {parameter.HasValue}");

                    sb.AppendLine(
                        $"  Is ReadOnly  : {parameter.IsReadOnly}");

                    sb.AppendLine(
                        $"  Is Shared    : {parameter.IsShared}");

                    //=================================================
                    // Data Type

                    if (definition != null)
                    {
                        ForgeTypeId dataType =
                            definition.GetDataType();

                        sb.AppendLine(
                            $"  Data Type    : {dataType.TypeId}");

                        bool isMeasurable =
                            UnitUtils.IsMeasurableSpec(dataType);

                        sb.AppendLine(
                            $"  Measurable   : {isMeasurable}");
                    }

                    //=================================================
                    // Shared Parameter GUID

                    if (parameter.IsShared)
                    {
                        sb.AppendLine(
                            $"  GUID         : {parameter.GUID}");
                    }

                    //=================================================
                    // Parameter Value

                    sb.AppendLine(
                        $"  Value        : " +
                        $"{GetParameterValue(parameter)}");

                    sb.AppendLine(
                        "----------------------------------------");
                }

                //=====================================================

                sb.AppendLine();

                sb.AppendLine(
                    $"Total Parameters: {parameterCount}");

                //=====================================================

                TaskDialog.Show(
                    "Parameter Inspector",
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

        // =========================================================================
        // Get Parameter Value
        // =========================================================================

        private static string GetParameterValue(
            Parameter parameter)
        {
            if (!parameter.HasValue)
                return "<No Value>";

            switch (parameter.StorageType)
            {
                case StorageType.Double:

                    return parameter.AsValueString()
                           ?? parameter.AsDouble().ToString();

                case StorageType.Integer:

                    return parameter.AsInteger().ToString();

                case StorageType.String:

                    return parameter.AsString()
                           ?? "<null>";

                case StorageType.ElementId:

                    ElementId id =
                        parameter.AsElementId();

                    return id.IntegerValue.ToString();

                default:

                    return "<Unknown>";
            }
        }
    }
}
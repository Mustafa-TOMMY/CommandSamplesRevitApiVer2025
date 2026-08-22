using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.Units.Commands
{
    // ============================================================================
    // Parameter Data Type And Valid Units
    //
    // This command demonstrates how to discover what a Parameter represents
    // and, when applicable, which units can be used with that data type.
    //
    // Workflow:
    //
    // Select Element
    //      ↓
    // Get Parameter
    //      ↓
    // Parameter.Definition.GetDataType()
    //      ↓
    // ForgeTypeId
    //      ↓
    // IsMeasurableSpec() ?
    //      │
    //      ├── No
    //      │
    //      └── Yes
    //            ↓
    //       GetValidUnits()
    //
    // Example:
    //
    // Wall
    //   ↓
    // Unconnected Height
    //   ↓
    // Data Type = Length
    //   ↓
    // Valid Units:
    // Feet / Meters / Millimeters / ...
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 04
    public class ParameterDataTypeAndValidUnitsCommand : IExternalCommand
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
                // Select Element

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall");

                Wall wall = doc.GetElement(reference) as Wall;

                if (wall == null)
                {
                    TaskDialog.Show(
                        "Parameter Data Type",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Parameter
                //
                // We use Wall Height as an example because its data type
                // is Length.

                Parameter parameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (parameter == null)
                {
                    TaskDialog.Show(
                        "Parameter Data Type",
                        "Wall height parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Parameter Data Type

                ForgeTypeId dataType = parameter.Definition.GetDataType();

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Parameter: {parameter.Definition.Name}");

                sb.AppendLine();

                sb.AppendLine($"Data Type TypeId:\n{dataType.TypeId}");

                sb.AppendLine();

                //=====================================================
                // Check if the Data Type is a Measurable Spec

                if (UnitUtils.IsMeasurableSpec(dataType))
                {
                    sb.AppendLine("Data Type: Measurable Spec");

                    sb.AppendLine();

                    //=================================================
                    // Get Valid Units

                    IList<ForgeTypeId> validUnits = UnitUtils.GetValidUnits(dataType);

                    sb.AppendLine($"Valid Units Count: {validUnits.Count}");

                    sb.AppendLine();

                    sb.AppendLine("========================================");

                    foreach (ForgeTypeId unit in validUnits)
                    {
                        string unitName;

                        try
                        {
                            unitName = LabelUtils.GetLabelForUnit(unit);
                        }
                        catch
                        {
                            unitName = "Unknown";
                        }

                        sb.AppendLine(
                            $"Name   : {unitName}\n" +
                            $"TypeId : {unit.TypeId}");

                        sb.AppendLine("----------------------------------------");
                    }
                }
                else
                {
                    sb.AppendLine("Data Type: Not a measurable Spec.");

                    sb.AppendLine();

                    sb.AppendLine("GetValidUnits() is not applicable.");
                }

                //=====================================================

                TaskDialog.Show(
                    "Parameter Data Type",
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
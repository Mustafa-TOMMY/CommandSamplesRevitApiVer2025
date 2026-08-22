using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitApiSamples.Samples.Units.Commands
{
    // ============================================================================
    // Format Parameter Value
    //
    // This command demonstrates the difference between:
    //
    // Conversion vs Formatting
    //
    // Conversion:
    //
    // 3 meters
    //      ↓
    // Internal Units
    //
    // The numerical value changes.
    //
    // Formatting:
    //
    // Internal Value
    //      ↓
    // Project Units + FormatOptions
    //      ↓
    // "3000 mm"
    //
    // The stored value does NOT change.
    // Only the displayed text changes.
    //
    // Workflow:
    //
    // Select Wall
    //      ↓
    // Get Parameter
    //      ↓
    // Get Internal Value
    //      ↓
    // Format Value
    //      ↓
    // Display Formatted String
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class FormatParameterValueCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Select Wall

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall");

                Wall wall = doc.GetElement(reference) as Wall;

                if (wall == null)
                {
                    TaskDialog.Show(
                        "Format Parameter",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Wall Height Parameter

                Parameter heightParameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (heightParameter == null)
                {
                    TaskDialog.Show(
                        "Format Parameter",
                        "Wall height parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Internal Value

                double internalValue = heightParameter.AsDouble();

                //=====================================================
                // Format Parameter Value
                //
                // Revit uses the Parameter's Definition and the
                // Document's Units/FormatOptions to produce the
                // user-facing string.

                string formattedValue = heightParameter.AsValueString();

                //=====================================================
                // Get Project Units Information

                Autodesk.Revit.DB.Units projectUnits = doc.GetUnits();

                FormatOptions lengthFormat = projectUnits.GetFormatOptions(SpecTypeId.Length);

                ForgeTypeId displayUnit = lengthFormat.GetUnitTypeId();

                string unitName;

                try
                {
                    unitName = LabelUtils.GetLabelForUnit(displayUnit);
                }
                catch
                {
                    unitName = "Unknown";
                }

                //=====================================================

                TaskDialog.Show(
                    "Format Parameter Value",

                    $"Parameter:\n" +
                    $"{heightParameter.Definition.Name}\n\n" +

                    $"Internal Value:\n" +
                    $"{internalValue:F6} ft\n\n" +

                    $"Project Display Unit:\n" +
                    $"{unitName}\n\n" +

                    $"Formatted Value:\n" +
                    $"{formattedValue}");

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
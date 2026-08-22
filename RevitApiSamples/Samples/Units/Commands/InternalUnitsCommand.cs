using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitApiSamples.Samples.Units.Commands
{
    // ============================================================================
    // Internal Units
    //
    // This command demonstrates the difference between:
    //
    // - Revit API Internal Units
    // - Display Units
    //
    // For Length:
    //
    // Revit Internal Unit = Feet
    //
    // Workflow:
    //
    // Select Wall
    //      ↓
    // Get Height Parameter
    //      ↓
    // AsDouble()
    //      ↓
    // Internal Value (Feet)
    //      ↓
    // ConvertFromInternalUnits()
    //      ↓
    // Meters / Millimeters
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class InternalUnitsCommand : IExternalCommand
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
                        "Internal Units",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Wall Height

                Parameter heightParameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (heightParameter == null)
                {
                    TaskDialog.Show(
                        "Internal Units",
                        "Wall height parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Internal Value (AsDouble() return Revit internal value)

                double internalHeight = heightParameter.AsDouble();

                //=====================================================
                // Convert Internal Units → Meters

                double heightMeters = UnitUtils.ConvertFromInternalUnits(internalHeight,UnitTypeId.Meters);

                //=====================================================
                // Convert Internal Units → Millimeters

                double heightMillimeters =UnitUtils.ConvertFromInternalUnits(internalHeight,UnitTypeId.Millimeters);

                //=====================================================

                TaskDialog.Show(
                    "Internal Units",
                    $"Wall: {wall.Name}\n\n" +

                    $"API Value:\n" +
                    $"{internalHeight:F6} ft\n\n" +

                    $"Meters:\n" +
                    $"{heightMeters:F3} m\n\n" +

                    $"Millimeters:\n" +
                    $"{heightMillimeters:F1} mm");

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
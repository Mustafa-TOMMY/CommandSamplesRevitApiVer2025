using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Instance Vs Type Parameters
    // This command demonstrates the difference between:
    // Instance Parameters vs Type Parameters
    // Example:
    //
    // Wall Instance
    //      │
    //      ├── Unconnected Height
    //      │       ↓
    //      │   Instance Parameter
    //      │
    //      └── WallType
    //              │
    //              └── Width
    //                      ↓
    //                  Type Parameter
    //
    // Important:
    // Instance Parameter → affects one specific element
    //
    // Type Parameter
    //      → belongs to the ElementType
    //      → affects all instances using that type
    //
    // Workflow:
    //
    // Select Wall
    //      ↓
    // Get Wall Instance Parameter
    //      ↓
    // Get Wall Type
    //      ↓
    // Get Type Parameter
    //      ↓
    // Compare
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class InstanceVsTypeParameterCommand : IExternalCommand
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
                        "Instance Vs Type",
                        "Please select a Wall.");

                    return Result.Failed;
                }

                //=====================================================
                // INSTANCE PARAMETER
                // Unconnected Height belongs to the Wall instance.

                Parameter instanceParameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);

                if (instanceParameter == null)
                {
                    TaskDialog.Show(
                        "Instance Vs Type",
                        "Instance parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // TYPE
                // Every Element has a TypeId.
                // The TypeId points to the ElementType that defines
                // the type of this specific instance.

                ElementId typeId = wall.GetTypeId();

                WallType wallType = doc.GetElement(typeId) as WallType;

                if (wallType == null)
                {
                    TaskDialog.Show(
                        "Instance Vs Type",
                        "Wall Type was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // TYPE PARAMETER
                // Width belongs to the WallType.

                Parameter typeParameter = wallType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);

                if (typeParameter == null)
                {
                    TaskDialog.Show(
                        "Instance Vs Type",
                        "Type parameter was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Read Values
                // We use AsValueString() here because the purpose of
                // this command is inspection/display.

                string instanceValue = instanceParameter.AsValueString();

                string typeValue = typeParameter.AsValueString();

                //=====================================================
                // Build Result

                TaskDialog.Show("Instance Vs Type Parameters",

                    $"Selected Wall:\n" + $"{wall.Name}\n\n" +
                    $"---------------- INSTANCE ----------------\n\n" +
                    $"Parameter:\n" + $"{instanceParameter.Definition.Name}\n\n" +
                    $"Value:\n" + $"{instanceValue}\n\n" +
                    $"Owner:\n" + $"Wall Instance\n\n" +
                    $"---------------- TYPE ----------------\n\n" +
                    $"Type:\n" + $"{wallType.Name}\n\n" +
                    $"Parameter:\n" + $"{typeParameter.Definition.Name}\n\n" +
                    $"Value:\n" + $"{typeValue}\n\n" +
                    $"Owner:\n" + $"Wall Type");

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
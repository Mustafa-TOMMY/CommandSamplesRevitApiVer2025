using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.Units.Commands
{
    // ============================================================================
    // Project Units
    //
    // This command demonstrates how to inspect the project's unit settings.
    //
    // Important:
    //
    // Internal Units
    //      ≠
    // Project Display Units
    //
    // Revit stores measurable values internally using its internal representation.
    //
    // Project Units determine how those values are displayed/formatted
    // in the Revit project.
    //
    // Workflow:
    //
    // Document
    //      ↓
    // GetUnits()
    //      ↓
    // Units
    //      ↓
    // GetFormatOptions(SpecTypeId.Length)
    //      ↓
    // FormatOptions
    //      ↓
    // UnitTypeId
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class ProjectUnitsCommand : IExternalCommand
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
                // Get Project Units
                Autodesk.Revit.DB.Units units = doc.GetUnits();

                //=====================================================
                // Get Length Format Options
                FormatOptions lengthFormat = units.GetFormatOptions(SpecTypeId.Length);

                //=====================================================
                // Get Display Unit
                ForgeTypeId lengthUnit = lengthFormat.GetUnitTypeId();

                //=====================================================
                // Get User-Visible Unit Name
                string unitName = LabelUtils.GetLabelForUnit(lengthUnit);

                //=====================================================
                // Display Information

                TaskDialog.Show(
                    "Project Units",
                    $"Project: {doc.Title}\n\n" +
                    $"Spec: Length\n\n" +
                    $"Unit TypeId:\n" +
                    $"{lengthUnit.TypeId}\n\n" +
                    $"Display Unit:\n" +
                    $"{unitName}");

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
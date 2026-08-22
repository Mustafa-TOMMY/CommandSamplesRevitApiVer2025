using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // Collect Views By Type
    //
    // This command demonstrates how to collect specific View types
    // from the Revit Document.
    //
    // Workflow:
    //
    // Document
    //      ↓
    // FilteredElementCollector
    //      ↓
    // View
    //      ↓
    // Exclude Templates
    //      ↓
    // Filter by ViewType
    //      ↓
    // Result
    //
    // Example:
    //
    // FloorPlan Views
    //
    // View
    //   ├── ViewType == FloorPlan
    //   └── IsTemplate == false
    //
    // Important:
    //
    // ViewType answers:
    // "What kind of View is this?"
    //
    // IsTemplate answers:
    // "Is this View a View Template?"
    //
    // Both may be required when searching for usable project Views.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 04
    public class CollectViewsByTypeCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Collect all Views

                List<View> allViews = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .ToList();

                /*
                 * List<View> floorPlans = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .Where(view =>
                            view.ViewType == ViewType.FloorPlan &&
                            !view.IsTemplate)
                        .ToList();
                 */

                //=====================================================
                // Filter Views
                // For this sample:
                // View Type = FloorPlan
                // IsTemplate = false

                // get only the floor plan views that user can use (not templates)
                List<View> floorPlans = allViews.Where(view =>
                                view.ViewType == ViewType.FloorPlan && !view.IsTemplate)
                        .ToList();

                //=====================================================
                // Validation

                if (floorPlans.Count == 0)
                {
                    TaskDialog.Show("Collect Views By Type", "No Floor Plan Views were found.");
                    return Result.Succeeded;
                }

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("FLOOR PLAN VIEWS");

                sb.AppendLine("========================================");

                sb.AppendLine($"Total Floor Plans: {floorPlans.Count}");

                sb.AppendLine();

                //=====================================================
                // Display Results

                foreach (View view in floorPlans)
                {
                    sb.AppendLine($"Name:\n{view.Name}");

                    sb.AppendLine($"View Type:\n{view.ViewType}");

                    sb.AppendLine($"Is Template:\n{view.IsTemplate}");

                    sb.AppendLine($"Element Id:\n{view.Id.IntegerValue}");

                    sb.AppendLine("----------------------------------------");
                }

                //=====================================================

                TaskDialog.Show("Collect Views By Type", sb.ToString());

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
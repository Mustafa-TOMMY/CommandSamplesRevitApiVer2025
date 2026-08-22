using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // View Types
    //
    // This command demonstrates how to identify the type of a Revit View.
    //
    // Workflow:
    //
    // Active View
    //      ↓
    // View
    //      ↓
    // ViewType
    //
    // Examples:
    //
    // Floor Plan
    // Ceiling Plan
    // Section
    // Elevation
    // 3D View
    // Drafting View
    // Schedule
    // Legend
    //
    // Important:
    //
    // ViewType tells us WHAT KIND of view we are dealing with.
    //
    // It does NOT mean that every ViewType has a separate class that
    // we must cast to.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class ViewTypesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Get Active View

                View activeView = doc.ActiveView;

                if (activeView == null)
                {
                    TaskDialog.Show("View Types", "No active view was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get View Type

                ViewType viewType = activeView.ViewType;

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("VIEW TYPE");

                sb.AppendLine("========================================");

                sb.AppendLine($"View Name:\n{activeView.Name}");

                sb.AppendLine();
                // this is the view type according to c# classes
                sb.AppendLine($"View Class:\n{activeView.GetType().Name}");

                sb.AppendLine();
                // this is the view type according to RevitAPI ViewType enum
                sb.AppendLine($"View Type:\n{viewType}");

                sb.AppendLine();

                sb.AppendLine("========================================");

                sb.AppendLine("Common View Types:");

                sb.AppendLine("FloorPlan");

                sb.AppendLine("CeilingPlan");

                sb.AppendLine("Section");

                sb.AppendLine("Elevation");

                sb.AppendLine("3D");

                sb.AppendLine("DraftingView");

                sb.AppendLine("Schedule");

                sb.AppendLine("Legend");

                //=====================================================

                TaskDialog.Show("View Types", sb.ToString());

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
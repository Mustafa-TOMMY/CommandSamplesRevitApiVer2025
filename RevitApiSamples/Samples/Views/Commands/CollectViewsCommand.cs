using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // Collect Views
    //
    // This command demonstrates how to collect Views from the Revit Document.
    //
    // Workflow:
    //
    // Document
    //      ↓
    // FilteredElementCollector
    //      ↓
    // OfClass(typeof(View))
    //      ↓
    // Views
    //
    // Important:
    //
    // View is an Element in Revit.
    //
    // Therefore, it can be collected using:
    //
    // FilteredElementCollector
    //
    // This command also demonstrates how to inspect:
    //
    // - View Name
    // - ViewType
    // - IsTemplate
    //
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class CollectViewsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Collect Views

                List<View> views = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .ToList();

                //=====================================================
                // Validation

                if (views.Count == 0)
                {
                    TaskDialog.Show("Collect Views", "No views were found.");

                    return Result.Succeeded;
                }

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("PROJECT VIEWS");

                sb.AppendLine("========================================");

                sb.AppendLine($"Total Views: {views.Count}");

                sb.AppendLine();

                //=====================================================
                // Inspect Views

                foreach (View view in views)
                {
                    sb.AppendLine($"Name:\n{view.Name}");

                    sb.AppendLine($"View Type:\n{view.ViewType}");

                    sb.AppendLine($"Is Template:\n{view.IsTemplate}");

                    sb.AppendLine($"Element Id:\n{view.Id.IntegerValue}");

                    sb.AppendLine("----------------------------------------");
                }

                //=====================================================

                TaskDialog.Show("Collect Views", sb.ToString());

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
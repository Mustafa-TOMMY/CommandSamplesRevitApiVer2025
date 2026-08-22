using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // Get Active View
    //
    // This command demonstrates how to access the currently active Revit View.
    //
    // Workflow:
    //
    // UIApplication
    //      ↓
    // UIDocument
    //      ↓
    // Document
    //      ↓
    // ActiveView
    //      ↓
    // View
    //
    // Important:
    //
    // A View is also an Element in the Revit database.
    //
    // Therefore:
    //
    // View
    //   ↓
    // Element.Id
    // Element.Name
    // Element.Category
    //
    // In addition, View provides view-specific properties such as:
    //
    // ViewType
    // Scale
    // IsTemplate
    // CropBox
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class GetActiveViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
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
                    TaskDialog.Show("Active View","No active view was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Build Result

                StringBuilder sb =new StringBuilder();

                sb.AppendLine("ACTIVE VIEW");

                sb.AppendLine("========================================");

                sb.AppendLine($"Name:\n{activeView.Name}");

                sb.AppendLine();

                sb.AppendLine($"Id:\n{activeView.Id.IntegerValue}");

                sb.AppendLine();

                sb.AppendLine($"View Type:\n{activeView.ViewType}");

                sb.AppendLine();

                sb.AppendLine($"Is Template:\n{activeView.IsTemplate}");

                sb.AppendLine();

                sb.AppendLine($"Scale:\n1:{activeView.Scale}");

                sb.AppendLine();

                sb.AppendLine($"Detail Level:\n{activeView.DetailLevel}");

                sb.AppendLine();

                sb.AppendLine($"Display Style:\n{activeView.DisplayStyle}");

                //=====================================================

                TaskDialog.Show("Active View",sb.ToString());

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
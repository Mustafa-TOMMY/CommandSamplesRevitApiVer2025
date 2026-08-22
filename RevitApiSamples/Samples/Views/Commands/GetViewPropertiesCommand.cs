using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // Get View Properties
    //
    // This command demonstrates how to inspect common properties of a View.
    //
    // Workflow:
    //
    // Active View
    //      ↓
    // View
    //      ↓
    // Common View Properties
    //
    // Examples:
    //
    // - Name
    // - Id
    // - ViewType
    // - IsTemplate
    // - Scale
    // - DetailLevel
    // - DisplayStyle
    // - ViewTemplateId
    //
    // Important:
    //
    // Not every View type supports every property in the same way.
    //
    // Therefore:
    //
    // View
    //   ↓
    // Common Properties
    //   +
    // View-Type-Specific Properties
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class GetViewPropertiesCommand : IExternalCommand
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

                View view = doc.ActiveView;

                if (view == null)
                {
                    TaskDialog.Show("View Properties", "No active view was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("VIEW PROPERTIES");

                sb.AppendLine("========================================");

                //=====================================================
                // Basic Properties

                sb.AppendLine($"Name:\n{view.Name}");

                sb.AppendLine();

                sb.AppendLine($"Element Id:\n{view.Id.IntegerValue}");

                sb.AppendLine();

                sb.AppendLine($"View Type:\n{view.ViewType}");

                sb.AppendLine();

                sb.AppendLine($"Is Template:\n{view.IsTemplate}");

                sb.AppendLine();

                //=====================================================
                // Scale

                sb.AppendLine($"Scale:\n1:{view.Scale}");

                sb.AppendLine();

                //=====================================================
                // Detail Level

                sb.AppendLine($"Detail Level:\n{view.DetailLevel}");

                sb.AppendLine();

                //=====================================================
                // Display Style

                sb.AppendLine($"Display Style:\n{view.DisplayStyle}");

                sb.AppendLine();

                //=====================================================
                // View Template

                ElementId templateId = view.ViewTemplateId;

                if (templateId != ElementId.InvalidElementId)
                {
                    View template = doc.GetElement(templateId) as View;
                    sb.AppendLine($"View Template:\n" + $"{template?.Name ?? "Unknown"}");
                    sb.AppendLine($"Template Id:\n" + $"{templateId.IntegerValue}");
                }
                else
                {
                    sb.AppendLine("View Template:\nNone");
                }

                sb.AppendLine();

                //=====================================================
                // Associated Level
                // Not every View has a meaningful associated level.
                // GenLevel can therefore be null.
                // GenLevel is for the plan views, ceiling plans, but for sections and 3D views, GenLevel is null.

                Level level = view.GenLevel;

                sb.AppendLine($"Associated Level:\n" + $"{level?.Name ?? "None"}");

                //=====================================================

                TaskDialog.Show("View Properties", sb.ToString());

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
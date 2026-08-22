using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Project Document vs Family Document
    //
    // This command demonstrates the difference between:
    //
    // Project Document
    // Family Document
    //
    // A Project Document represents the Revit project/model.
    //
    // A Family Document represents a family being edited independently.
    //
    // Workflow:
    //
    // Current Document
    //       ↓
    // IsFamilyDocument
    //       ↓
    // ┌───────────────────────┐
    // │                       │
    // No                      Yes
    // │                       │
    // ▼                       ▼
    // Project Document     Family Document
    //
    // Important:
    //
    // Both are represented by the Revit API using the Document class.
    //
    // The difference is the CONTEXT of the Document.
    //
    // Project Document:
    //     Contains project elements, views, levels, walls, families, etc.
    //
    // Family Document:
    //     Represents the family being edited and provides access to
    //     family-specific functionality such as FamilyManager.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class ProjectVsFamilyDocumentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //=====================================================
                // Get Current Document

                UIApplication uiApp = commandData.Application;

                UIDocument uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("Document", "No active UIDocument was found.");

                    return Result.Failed;
                }

                Document doc = uiDoc.Document;

                //=====================================================
                // Determine Document Type

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("DOCUMENT CONTEXT");

                sb.AppendLine("========================================");

                if (doc.IsFamilyDocument)
                {
                    //=================================================
                    // Family Document

                    sb.AppendLine("Type:\nFamily Document");

                    sb.AppendLine();

                    sb.AppendLine("This Document represents a Family.");

                    sb.AppendLine();

                    sb.AppendLine("Family Parameters can be accessed " + "through FamilyManager.");

                    sb.AppendLine();

                    sb.AppendLine($"Family Name:\n" + $"{doc.OwnerFamily?.Name ?? "Unknown"}");
                }
                else
                {
                    //=================================================
                    // Project Document

                    sb.AppendLine("Type:\nProject Document");

                    sb.AppendLine();

                    sb.AppendLine("This Document represents a Revit Project.");

                    sb.AppendLine();

                    sb.AppendLine("Project elements, Views, Levels, " + "Families and Project Parameters " + 
                        "belong to this Document context.");
                }

                //=====================================================

                TaskDialog.Show(
                    "Project vs Family Document",
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
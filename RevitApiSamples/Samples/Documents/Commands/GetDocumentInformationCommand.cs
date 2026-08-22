using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Document Information
    //
    // This command demonstrates how to inspect the current Revit Document.
    //
    // Workflow:
    //
    // UIApplication
    //      ↓
    // UIDocument
    //      ↓
    // Document
    //      ↓
    // Document Information
    //
    // Important:
    //
    // Document represents the Revit database currently being accessed.
    //
    // The Document also contains information that describes its context:
    //
    // - Project or Family Document
    // - Workshared or non-workshared
    // - File path
    // - Title
    // - Revit Application
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class GetDocumentInformationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //=====================================================
                // Get UIApplication

                UIApplication uiApp = commandData.Application;

                //=====================================================
                // Get UIDocument

                UIDocument uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show(
                        "Document Information",
                        "No active UIDocument was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Document

                Document doc = uiDoc.Document;

                //=====================================================
                // Document Information

                string documentType = doc.IsFamilyDocument
                        ? "Family Document"
                        : "Project Document";

                string path = string.IsNullOrEmpty(doc.PathName)
                        ? "Not saved / No path"
                        : doc.PathName;

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("DOCUMENT INFORMATION");

                sb.AppendLine("========================================");

                sb.AppendLine($"Title:\n{doc.Title}");

                sb.AppendLine();

                sb.AppendLine($"Path:\n{path}");

                sb.AppendLine();

                sb.AppendLine($"Document Type:\n{documentType}");

                sb.AppendLine();

                sb.AppendLine($"Is Family Document:\n{doc.IsFamilyDocument}");

                sb.AppendLine();

                sb.AppendLine($"Is Workshared:\n{doc.IsWorkshared}");

                sb.AppendLine();

                sb.AppendLine($"Is Linked:\n{doc.IsLinked}");

                //=====================================================
                // Application

                sb.AppendLine();

                sb.AppendLine($"Application Version:\n" + $"{doc.Application.VersionName}");

                sb.AppendLine();

                sb.AppendLine($"Application Version Number:\n" + $"{doc.Application.VersionNumber}");

                //=====================================================

                TaskDialog.Show("Document Information", sb.ToString());

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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Get Current Document
    //
    // This command demonstrates how to access the current Revit Document.
    //
    // Workflow:
    //
    // UIApplication
    //      ↓
    // UIDocument
    //      ↓
    // Document
    //
    // Important:
    //
    // UIApplication represents the Revit application/session.
    //
    // UIDocument represents the document currently opened in the Revit UI.
    //
    // Document represents the actual Revit database/document that the API
    // uses to read and modify model data.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class GetCurrentDocumentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //=====================================================
                // UIApplication
                UIApplication uiApp = commandData.Application;
                //=====================================================
                // UIDocument
                UIDocument uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show("Document", "No active UIDocument was found.");
                    return Result.Failed;
                }

                //=====================================================
                // Document
                Document doc = uiDoc.Document;

                if (doc == null)
                {
                    TaskDialog.Show("Document", "No active Document was found.");
                    return Result.Failed;
                }

                //=====================================================
                // Display Information

                string documentType;

                if (doc.IsFamilyDocument)
                {
                    documentType = "Family Document";
                }
                else
                {
                    documentType = "Project Document";
                }

                bool isWorkshared = doc.IsWorkshared;
                string path = doc.PathName;

                TaskDialog.Show(
                    "Document Information",
                    $"Title: {doc.Title}\n\n" +
                    $"Is Workshared: {doc.IsWorkshared}\n\n" +
                    $"Path: {doc.PathName}");

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
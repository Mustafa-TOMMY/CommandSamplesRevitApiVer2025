using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Get Linked Document
    //
    // This command demonstrates how to access the Document of a Revit Link.
    //
    // Workflow:
    //
    // Host Project Document
    //          ↓
    // RevitLinkInstance
    //          ↓
    // GetLinkDocument()
    //          ↓
    // Linked Document
    //          ↓
    // Linked Elements
    //
    // Important:
    //
    // RevitLinkInstance belongs to the Host Document.
    //
    // The elements inside the linked model belong to the Linked Document.
    //
    // Therefore:
    //
    // Host Document
    //      ↓
    // RevitLinkInstance
    //      ↓
    // Linked Document
    //
    // The Linked Document is a different Document context.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 06
    public class GetLinkedDocumentCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                //=====================================================
                // Get Host Document

                UIApplication uiApp =
                    commandData.Application;

                UIDocument uiDoc =
                    uiApp.ActiveUIDocument;

                Document doc =
                    uiDoc.Document;

                //=====================================================
                // Select Revit Link

                Reference reference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Revit Link");

                Element element =
                    doc.GetElement(reference);

                //=====================================================
                // Validate Revit Link

                RevitLinkInstance linkInstance =
                    element as RevitLinkInstance;

                if (linkInstance == null)
                {
                    TaskDialog.Show(
                        "Linked Document",
                        "The selected element is not a Revit Link.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Linked Document

                Document linkedDoc =
                    linkInstance.GetLinkDocument();

                if (linkedDoc == null)
                {
                    TaskDialog.Show(
                        "Linked Document",
                        "The Linked Document could not be accessed.");

                    return Result.Failed;
                }

                //=====================================================
                // Build Result

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "LINKED DOCUMENT");

                sb.AppendLine(
                    "========================================");

                sb.AppendLine(
                    $"Host Document:\n{doc.Title}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Link Instance:\n" +
                    $"{linkInstance.Name}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Linked Document:\n" +
                    $"{linkedDoc.Title}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Is Linked:\n" +
                    $"{linkedDoc.IsLinked}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Path:\n" +
                    $"{linkedDoc.PathName}");

                sb.AppendLine();

                sb.AppendLine(
                    "The selected Revit Link belongs to the " +
                    "Host Document, while the linked model " +
                    "has its own Document context.");

                //=====================================================

                TaskDialog.Show(
                    "Linked Document",
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
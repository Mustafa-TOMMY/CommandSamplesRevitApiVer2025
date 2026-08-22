using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Documents.Commands
{
    // ============================================================================
    // Access Family Manager
    //
    // This command demonstrates how to access the FamilyManager of a Family
    // through a Family Document.
    //
    // Workflow:
    //
    // Project Document
    //      ↓
    // Select FamilyInstance
    //      ↓
    // Family
    //      ↓
    // EditFamily()
    //      ↓
    // Family Document
    //      ↓
    // FamilyManager
    //      ↓
    // Family Parameters
    //
    // Important:
    //
    // Family parameters belong to the Family Document context.
    //
    // They are managed through:
    //
    // FamilyDocument.FamilyManager
    //
    // This is different from Project Parameters, which are accessed through
    // the Project Document and ParameterBindings.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 05
    public class AccessFamilyManagerCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp =
                    commandData.Application;

                UIDocument uiDoc =
                    uiApp.ActiveUIDocument;

                Document projectDoc =
                    uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Family Instance");

                Element element =
                    projectDoc.GetElement(reference);

                //=====================================================
                // Validate Family Instance

                FamilyInstance familyInstance =
                    element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Family Manager",
                        "Please select a Family Instance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family =
                    familyInstance.Symbol.Family;

                if (family == null)
                {
                    TaskDialog.Show(
                        "Family Manager",
                        "The selected instance has no Family.");

                    return Result.Failed;
                }

                //=====================================================
                // Open Family Document

                Document familyDoc =
                    projectDoc.EditFamily(family);

                if (familyDoc == null)
                {
                    TaskDialog.Show(
                        "Family Manager",
                        "The Family Document could not be opened.");

                    return Result.Failed;
                }

                //=====================================================
                // Access FamilyManager

                FamilyManager familyManager =
                    familyDoc.FamilyManager;

                if (familyManager == null)
                {
                    TaskDialog.Show(
                        "Family Manager",
                        "FamilyManager could not be accessed.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family Parameters

                IList<FamilyParameter> parameters =
                    familyManager.Parameters
                        .Cast<FamilyParameter>()
                        .ToList();

                //=====================================================
                // Build Result

                StringBuilder sb =
                    new StringBuilder();

                sb.AppendLine(
                    "FAMILY MANAGER");

                sb.AppendLine(
                    "========================================");

                sb.AppendLine(
                    $"Family:\n{family.Name}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Family Document:\n{familyDoc.Title}");

                sb.AppendLine();

                sb.AppendLine(
                    $"Parameter Count:\n{parameters.Count}");

                sb.AppendLine();

                sb.AppendLine(
                    "Family Parameters:");

                sb.AppendLine(
                    "----------------------------------------");

                foreach (FamilyParameter parameter in parameters)
                {
                    sb.AppendLine(
                        $"Name:\n{parameter.Definition.Name}");

                    sb.AppendLine(
                        $"Is Instance:\n{parameter.IsInstance}");

                    sb.AppendLine(
                        $"Is Shared:\n{parameter.IsShared}");

                    sb.AppendLine(
                        "----------------------------------------");
                }

                //=====================================================

                TaskDialog.Show(
                    "Family Manager",
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
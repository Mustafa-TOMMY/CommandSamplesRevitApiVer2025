using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Family Parameters
    //
    // This command demonstrates how to inspect parameters defined inside
    // a Revit Family.
    //
    // Project Parameters:
    //
    // Document
    //     ↓
    // ParameterBindings
    //
    // Family Parameters:
    //
    // Family
    //     ↓
    // Family Document
    //     ↓
    // FamilyManager
    //
    // Workflow:
    //
    // Select Family Instance
    //      ↓
    // Get Family
    //      ↓
    // EditFamily()
    //      ↓
    // FamilyManager
    //      ↓
    // Family Parameters
    //
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 08
    public class FamilyParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Family Instance");

                FamilyInstance familyInstance = doc.GetElement(reference) as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Family Parameters",
                        "Please select a Family Instance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family = familyInstance.Symbol.Family;

                if (family == null)
                {
                    TaskDialog.Show(
                        "Family Parameters",
                        "Family was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Open Family Document

                Document familyDocument = doc.EditFamily(family);

                if (familyDocument == null)
                {
                    TaskDialog.Show(
                        "Family Parameters",
                        "Could not open the Family document.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family Manager

                FamilyManager familyManager = familyDocument.FamilyManager;

                //=====================================================
                // Read Family Parameters

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Family: {family.Name}");

                sb.AppendLine();

                sb.AppendLine($"Parameters Count: " + $"{familyManager.Parameters.Size}");

                sb.AppendLine();

                sb.AppendLine("========================================");

                foreach (FamilyParameter parameter in familyManager.Parameters)
                {
                    sb.AppendLine($"Name:\n" + $"{parameter.Definition.Name}");

                    sb.AppendLine( $"Is Instance:\n" + $"{parameter.IsInstance}");

                    sb.AppendLine($"Storage Type:\n" + $"{parameter.StorageType}");

                    sb.AppendLine($"Is Shared:\n" + $"{parameter.IsShared}");

                    sb.AppendLine("----------------------------------------");
                }

                //=====================================================

                TaskDialog.Show(
                    "Family Parameters",
                    sb.ToString());

                //=====================================================
                // Close Family Document

                familyDocument.Close(false);

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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Load Family
    //
    // This command demonstrates how to load an external RFA Family
    // into the current Project Document.
    //
    // Workflow:
    //
    // RFA File
    //    ↓
    // Document.LoadFamily()
    //    ↓
    // Family
    //
    // Important:
    //
    // Loading a Family is different from activating a FamilySymbol.
    //
    // LoadFamily:
    //     Brings the Family definition into the Project.
    //
    // Activate:
    //     Activates a FamilySymbol that already exists in the Project.
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 05
    public class LoadFamilyCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;

                UIDocument uiDoc = uiApp.ActiveUIDocument;

                Document doc = uiDoc.Document;

                //=====================================================
                // Ask for RFA File

                string familyPath = GetFamilyPath();

                if (string.IsNullOrWhiteSpace(familyPath))
                {
                    return Result.Cancelled;
                }

                //=====================================================
                // Validate File

                if (!File.Exists(familyPath))
                {
                    TaskDialog.Show(
                        "Load Family",
                        "The selected family file does not exist.");

                    return Result.Failed;
                }

                //=====================================================
                // Check Extension

                if (!string.Equals(
                        Path.GetExtension(familyPath),
                        ".rfa",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TaskDialog.Show(
                        "Load Family",
                        "Please select a valid Revit Family (.rfa) file.");

                    return Result.Failed;
                }

                //=====================================================
                // Load Family

                Family loadedFamily = null;

                using (Transaction transaction = new Transaction(doc, "Load Family"))
                {
                    transaction.Start();

                    bool loaded = doc.LoadFamily(familyPath, out loadedFamily);

                    if (!loaded || loadedFamily == null)
                    {
                        transaction.RollBack();

                        TaskDialog.Show("Load Family", "The family could not be loaded.");

                        return Result.Failed;
                    }

                    transaction.Commit();
                }

                //=====================================================
                // Get Family Symbols

                ISet<ElementId> symbolIds = loadedFamily.GetFamilySymbolIds();

                //=====================================================
                // Result

                string result = "FAMILY LOADED SUCCESSFULLY\n" +
                    "========================================\n\n" +

                    $"Family:\n" +
                    $"{loadedFamily.Name}\n\n" +

                    $"Family Id:\n" +
                    $"{loadedFamily.Id}\n\n" +

                    $"Family Symbols:\n" +
                    $"{symbolIds.Count}";

                TaskDialog.Show(
                    "Load Family",
                    result);

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

        //=============================================================
        // Simple File Selection
        //
        // This helper keeps the command focused on the Revit API
        // Family loading workflow.
        //
        // A production application could replace this with a
        // dedicated file-selection service.
        //=============================================================

        private string? GetFamilyPath()
        {
            FileOpenDialog dialog = new FileOpenDialog("Revit Family Files (*.rfa)|*.rfa");
            dialog.Title = "Select Revit Family";

            ItemSelectionDialogResult result = dialog.Show();
            if (result != ItemSelectionDialogResult.Confirmed)
                return null;

            ModelPath modelPath = dialog.GetSelectedModelPath();
            if (modelPath == null)
                return null;

            return ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
        }
    }
}
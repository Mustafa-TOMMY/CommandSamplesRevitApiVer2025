using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Family Symbol Activation
    //
    // This command demonstrates:
    //
    // FamilySymbol
    //     ↓
    // IsActive
    //     ↓
    // Activate()
    //     ↓
    // Document.Regenerate()
    //
    // Important:
    //
    // A FamilySymbol can exist in the Document without being active.
    //
    // Activation is especially important before certain operations that require
    // an active symbol, such as creating FamilyInstances.
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 04
    public class FamilySymbolActivationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Family Instance");

                //=====================================================
                // Get Element

                Element element = doc.GetElement(reference);

                //=====================================================
                // Get Family Instance

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Families", "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Symbol

                FamilySymbol symbol = familyInstance.Symbol;

                if (symbol == null)
                {
                    TaskDialog.Show("Families", "No FamilySymbol was found.");

                    return Result.Failed;
                }

                //=====================================================
                // Check Current State

                bool wasActive = symbol.IsActive;

                //=====================================================
                // Activate Symbol if Necessary

                if (!symbol.IsActive)
                {
                    using (Transaction transaction = new Transaction(doc, "Activate Family Symbol"))
                    {
                        transaction.Start();

                        symbol.Activate();
                        doc.Regenerate();

                        transaction.Commit();
                    }
                }

                //=====================================================
                // Check Final State

                bool isActive = symbol.IsActive;

                //=====================================================
                // Result

                string result = "FAMILY SYMBOL ACTIVATION\n" +
                    "========================================\n\n" +

                    $"Family:\n" +
                    $"{symbol.Family.Name}\n\n" +

                    $"Type:\n" +
                    $"{symbol.Name}\n\n" +

                    $"Symbol Id:\n" +
                    $"{symbol.Id}\n\n" +

                    $"Is Active Before:\n" +
                    $"{wasActive}\n\n" +

                    $"Is Active After:\n" +
                    $"{isActive}";

                TaskDialog.Show("Family Symbol", result);

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
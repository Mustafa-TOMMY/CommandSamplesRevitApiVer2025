using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Get Family Symbols From Family
    //
    // This command demonstrates how to:
    //
    // User
    //   ↓
    // Select FamilyInstance
    //   ↓
    // FamilySymbol
    //   ↓
    // Family
    //   ↓
    // Get Family's Symbols
    //
    // Important:
    //
    // A Family can contain multiple FamilySymbols (Types).
    //
    // Example:
    //
    // Door Family
    //      ├── 900 x 2100
    //      ├── 1000 x 2100
    //      └── 1200 x 2100
    //
    // The command demonstrates the difference between:
    //
    // Family
    // FamilySymbol
    // FamilyInstance
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 03
    public class GetFamilySymbolsFromFamilyCommand : IExternalCommand
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
                // Get Selected Element

                Element element = doc.GetElement(reference);

                //=====================================================
                // Validate FamilyInstance

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show("Families", "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get FamilySymbol

                FamilySymbol selectedSymbol = familyInstance.Symbol;

                if (selectedSymbol == null)
                {
                    TaskDialog.Show("Families", "The FamilyInstance does not have a valid FamilySymbol.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family = selectedSymbol.Family;

                if (family == null)
                {
                    TaskDialog.Show("Families", "The FamilySymbol does not have a valid Family.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Symbols from Family

                ISet<ElementId> symbolIds = family.GetFamilySymbolIds();

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("FAMILY SYMBOLS");

                sb.AppendLine("========================================");

                sb.AppendLine($"Family Id:\n{family.Id}");

                sb.AppendLine();

                sb.AppendLine($"Family Name:\n{family.Name}");

                sb.AppendLine();

                sb.AppendLine($"Symbol Count:\n{symbolIds.Count}");

                sb.AppendLine();

                sb.AppendLine("TYPES / SYMBOLS");

                sb.AppendLine("========================================");

                //=====================================================
                // Get Each Symbol

                int index = 1;

                foreach (ElementId symbolId in symbolIds)
                {
                    FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;

                    if (symbol == null) continue;

                    sb.AppendLine($"Type #{index}");

                    sb.AppendLine($"Symbol Id : {symbol.Id}");

                    sb.AppendLine($"Type Name : {symbol.Name}");

                    sb.AppendLine($"Is Active : {symbol.IsActive}");

                    sb.AppendLine("----------------------------------------");

                    index++;
                }

                //=====================================================

                TaskDialog.Show(
                    "Family Symbols",
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
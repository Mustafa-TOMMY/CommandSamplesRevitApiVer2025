using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Collect Family Symbols
    //
    // This command demonstrates how to collect FamilySymbol elements
    // from the current project document.
    //
    // Concept:
    //
    // Project Document
    //       ↓
    // FilteredElementCollector
    //       ↓
    // FamilySymbol
    //       ↓
    // Family
    //
    // A FamilySymbol represents a Family Type.
    //
    // Example:
    //
    // Family:
    //     Single-Flush Door
    //
    // Symbols / Types:
    //     900 x 2100
    //     1000 x 2100
    //     1200 x 2100
    //
    // The command demonstrates that:
    //
    // Family ≠ FamilySymbol
    //
    // A Family can contain multiple FamilySymbols.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class CollectFamilySymbolsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // Collect Family Symbols

                IList<FamilySymbol> symbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .ToList();

                //=====================================================

                if (symbols.Count == 0)
                {
                    TaskDialog.Show(
                        "Families",
                        "No FamilySymbols were found.");

                    return Result.Succeeded;
                }

                //=====================================================
                // Build Result

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("FAMILY SYMBOLS");
                sb.AppendLine("========================================");
                sb.AppendLine($"Total Symbols: {symbols.Count}");
                sb.AppendLine();

                //=====================================================
                // Display Symbols

                int index = 1;

                foreach (FamilySymbol symbol in symbols)
                {
                    Family family = symbol.Family;

                    sb.AppendLine($"Symbol #{index}");
                    sb.AppendLine($"Symbol Id    : {symbol.Id}");
                    sb.AppendLine($"Type Name    : {symbol.Name}");
                    sb.AppendLine($"Family Id    : " + $"{family?.Id.ToString() ?? "None"}");
                    sb.AppendLine($"Family Name  : " + $"{family?.Name ?? "None"}");
                    sb.AppendLine("----------------------------------------");

                    index++;
                }

                //=====================================================

                TaskDialog.Show("Family Symbols", sb.ToString());

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
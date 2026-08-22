using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Families.Commands
{
    // ============================================================================
    // Create Family Instance Command
    //
    // This command connects the Families Module with the ModelCreation Module:
    //
    // Conceptual Workflow:
    //
    // Family / RFA
    //       ↓
    // FamilySymbol
    //       ↓
    // IsActive?
    //  /       \
    // No        Yes
    // ↓          ↓
    // Activate   │
    // ↓          │
    // Regenerate │
    //  \        /
    //   ↓      ↓
    // NewFamilyInstance()
    //       ↓
    // FamilyInstance
    //
    // Important Connections:
    // - Families Module teaches FamilySymbol, IsActive, and FamilyPlacementType.
    // - ModelCreation Module teaches Document.Create.NewFamilyInstance().
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 12
    public class CreateFamilyInstanceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Pick an existing FamilyInstance to borrow its Symbol & Level
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an existing FamilyInstance to spawn a new instance of its Type");

                Element element = doc.GetElement(reference);
                FamilyInstance existingInstance = element as FamilyInstance;

                if (existingInstance == null)
                {
                    TaskDialog.Show("Create Instance", "The selected element is not a FamilyInstance.");
                    return Result.Failed;
                }

                FamilySymbol symbol = existingInstance.Symbol;
                if (symbol == null)
                {
                    TaskDialog.Show("Create Instance", "No valid FamilySymbol found.");
                    return Result.Failed;
                }

                //=====================================================
                // 2. Check and Activate FamilySymbol if Inactive
                //=====================================================

                if (!symbol.IsActive)
                {
                    using (Transaction tActivate = new Transaction(doc, "Activate FamilySymbol"))
                    {
                        tActivate.Start();
                        symbol.Activate();
                        doc.Regenerate();
                        tActivate.Commit();
                    }
                }

                //=====================================================
                // 3. Pick Insertion Point in 3D View / Plan View
                //=====================================================

                XYZ insertionPoint = uiDoc.Selection.PickPoint("Pick insertion point for the new FamilyInstance");

                // Get a valid Level
                Level level = doc.GetElement(existingInstance.LevelId) as Level;
                if (level == null)
                {
                    // Fallback to first level in project if instance level is null
                    level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();
                }

                if (level == null)
                {
                    TaskDialog.Show("Create Instance", "No valid Level found in document.");
                    return Result.Failed;
                }

                //=====================================================
                // 4. Create FamilyInstance inside Transaction
                //=====================================================

                FamilyInstance newInstance = null;

                using (Transaction transaction = new Transaction(doc, "Create FamilyInstance"))
                {
                    transaction.Start();

                    newInstance = doc.Create.NewFamilyInstance(
                        insertionPoint,
                        symbol,
                        level,
                        StructuralType.NonStructural);

                    if (newInstance == null)
                    {
                        transaction.RollBack();
                        TaskDialog.Show("Create Instance", "Failed to create FamilyInstance.");
                        return Result.Failed;
                    }

                    transaction.Commit();
                }

                //=====================================================
                // 5. Report Success
                //=====================================================

                string result = "FAMILY INSTANCE CREATED\n" +
                    "========================================\n\n" +
                    $"New Instance Id : {newInstance.Id}\n" +
                    $"Family Name     : {symbol.Family.Name}\n" +
                    $"Type Name       : {symbol.Name}\n" +
                    $"Level           : {level.Name}\n" +
                    $"Location Point  : ({insertionPoint.X:F2}, {insertionPoint.Y:F2}, {insertionPoint.Z:F2})";

                TaskDialog.Show("Create FamilyInstance", result);

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

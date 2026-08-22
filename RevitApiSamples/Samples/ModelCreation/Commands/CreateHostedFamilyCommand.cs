using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    // ============================================================================
    // Hosted Family Creation
    //
    // This command demonstrates how to create families that require a host element.
    //
    // Workflow:
    // Host Element -> Pick Point -> FamilySymbol -> Create(...)
    //
    //
    // Common Hosted Families:
    //
    // - Door
    // - Window
    // - Wall-Based Lighting Fixture
    // - Wall-Based Mechanical Equipment
    // - Wall-Based Plumbing Fixture
    //
    // Unlike Point-Based families, Hosted families cannot exist without
    // a valid host element (such as a Wall, Floor, Ceiling, or Roof).
    // ============================================================================
    [Transaction(TransactionMode.Manual)]
    // Command 04
    public class CreateHostedFamilyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //==============================

                Reference wallReference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a host wall");

                Wall wall = doc.GetElement(wallReference) as Wall;

                if (wall == null)
                {
                    TaskDialog.Show(
                        "Create Hosted Family",
                        "Please select a wall.");

                    return Result.Failed;
                }

                //=====================================================
                // Pick insertion point

                XYZ point = uiDoc.Selection.PickPoint("Pick insertion point");

                //=====================================================
                // Find Door Type

                FamilySymbol doorType =
                    new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault();

                if (doorType == null)
                {
                    TaskDialog.Show(
                        "Create Hosted Family",
                        "No door type found.");

                    return Result.Failed;
                }

                //=====================================================

                using (Transaction transaction = new Transaction(doc, "Create Door"))
                {
                    transaction.Start();

                    if (!doorType.IsActive)
                    {
                        doorType.Activate();
                        doc.Regenerate();
                    }

                    doc.Create.NewFamilyInstance(
                        point,
                        doorType,
                        wall,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    transaction.Commit();
                }

                TaskDialog.Show(
                    "Create Hosted Family",
                    "Door created successfully.");



                //==============================

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

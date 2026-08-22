using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    [Transaction(TransactionMode.Manual)]
    // Command 03
    public class CreateColumnCommand : IExternalCommand
    {
        // ============================================================================
        // Point-Based Element Creation
        //
        // This command demonstrates how to create elements that are placed at a single
        // insertion point.
        //
        // Workflow:
        // Pick Point -> FamilySymbol -> Create(...)
        //
        // Common Point-Based Elements:
        // - Structural Column
        // - Furniture
        // - Generic Model
        // - Mechanical Equipment
        // - Electrical Equipment
        // - Plumbing Fixture
        // - Specialty Equipment
        //
        // The main difference from Curve-Based creation is that these elements require
        // only one XYZ insertion point instead of a Curve.
        // ============================================================================
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //==============================

                FamilySymbol columnType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(BuiltInCategory.OST_StructuralColumns)
                            .Cast<FamilySymbol>()
                            .FirstOrDefault();

                Level level = doc.ActiveView.GenLevel;

                XYZ point = uiDoc.Selection.PickPoint("Pick Column point");



                //=====================================================

                using (Transaction transaction = new Transaction(doc, "Create Column"))
                {
                    transaction.Start();

                    if (!columnType.IsActive)
                    {
                        columnType.Activate();
                        doc.Regenerate();
                    }

                    FamilyInstance column = doc.Create.NewFamilyInstance(
                        point,
                        columnType,
                        level,
                        StructuralType.Column);

                    transaction.Commit();
                }



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

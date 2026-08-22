using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.ModelCreation.Commands
{
    public class CreateFaceBasedFamilyCommand : IExternalCommand
    {
        // ============================================================================
        // Face-Based Family Creation
        //
        // This command demonstrates how to place a Work Plane-Based family directly
        // on a selected Face.
        //
        // Workflow:
        //
        // Select Face
        //      ↓
        // Get selected point
        //      ↓
        // Get Face
        //      ↓
        // Calculate Face Normal
        //      ↓
        // Calculate Reference Direction
        //      ↓
        // Find FamilySymbol
        //      ↓
        // Activate FamilySymbol
        //      ↓
        // NewFamilyInstance()
        //
        // The selected Face can belong to different elements such as:
        //
        // - Wall
        // - Floor
        // - Ceiling
        // - Column
        // - Generic Model
        // - Mechanical Equipment
        // - Other elements with valid faces
        //
        // The important concept is that the family is placed on the selected Face,
        // not on a specific element category.
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

                Reference faceReference = uiDoc.Selection.PickObject(
                    ObjectType.Face,
                    "Select a face to place the family");
                // Get User selected point
                XYZ point = faceReference.GlobalPoint;
                // Get Face
                Element element = doc.GetElement(faceReference);
                Face face = element
                    .GetGeometryObjectFromReference(faceReference) as Face;

                if (face == null)
                {
                    TaskDialog.Show(
                        "Face-Based Family",
                        "The selected reference is not a valid face.");

                    return Result.Failed;
                }
                // Get UV location on the Face
                UV uv = face.Project(point).UVPoint;

                // Get Face Normal
                XYZ normal = face.ComputeNormal(uv);

                //=====================================================
                // Calculate Reference Direction
                // ReferenceDirection must lie on the Face.
                // We use CrossProduct to create a vector perpendicular
                // to the Face Normal.

                XYZ referenceDirection;

                if (Math.Abs(normal.DotProduct(XYZ.BasisZ)) < 0.99)
                {
                    referenceDirection =normal.CrossProduct(XYZ.BasisZ).Normalize();
                }
                else
                {
                    referenceDirection =normal.CrossProduct(XYZ.BasisX).Normalize();
                }
                //=====================================================
                // Find a Work Plane-Based FamilySymbol
                FamilySymbol familySymbol = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(symbol =>
                                symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased);

                if (familySymbol == null)
                {
                    TaskDialog.Show(
                        "Face-Based Family",
                        "No Work Plane-Based Family Type was found.");

                    return Result.Failed;
                }
                //=====================================================
                // Create Family Instance
                using (Transaction transaction = new Transaction(doc, "Create Face-Based Family"))
                {
                    transaction.Start();

                    // Activate FamilySymbol if necessary

                    if (!familySymbol.IsActive)
                    {
                        familySymbol.Activate();
                        doc.Regenerate();
                    }

                    // Place Family on selected Face

                    FamilyInstance familyInstance = doc.Create.NewFamilyInstance(
                                                        faceReference,
                                                        point,
                                                        referenceDirection,
                                                        familySymbol);

                    transaction.Commit();
                }

                TaskDialog.Show(
                    "Face-Based Family",
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Type: {familySymbol.Name}\n" +
                    $"Element Id: {familySymbol.Id}");

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

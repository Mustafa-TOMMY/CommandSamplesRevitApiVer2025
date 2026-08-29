using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Note: MirrorElement mirrors in-place (moves the element). 
    // To create a mirrored copy, use MirrorElements (plural).
    // ============================================================================

    // Command 05
    [Transaction(TransactionMode.Manual)]
    public class MirrorElementCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // Select Element
                //=====================================================
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select a FamilyInstance to mirror");
                Element element = doc.GetElement(selRef);

                if (!(element.Location is LocationPoint locPoint))
                {
                    TaskDialog.Show("Error", "Selected element does not have a LocationPoint.");
                    return Result.Cancelled;
                }

                //=====================================================
                // Define Mirror Plane
                //=====================================================
                XYZ location = locPoint.Point;
                Plane mirrorPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisX, location);

                //=====================================================
                // Mirror Element
                //=====================================================
                using (Transaction t = new Transaction(doc, "Mirror Element"))
                {
                    t.Start();
                    ElementTransformUtils.MirrorElement(doc, element.Id, mirrorPlane);
                    t.Commit();
                }

                TaskDialog.Show("Mirror Result", $"Element {element.Id} mirrored across YZ plane at its location.");

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

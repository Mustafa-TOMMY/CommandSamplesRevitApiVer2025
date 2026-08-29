using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Transform.Commands
{
    // ============================================================================
    // Workflow:
    // Select Element
    //       ↓
    // XYZ Translation Vector
    //       ↓
    // Transaction
    //       ↓
    // MoveElement()
    //       ↓
    // Element moved
    // ============================================================================

    // Command 02
    [Transaction(TransactionMode.Manual)]
    public class MoveElementCommand : IExternalCommand
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
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select an element to move");
                Element element = doc.GetElement(selRef);

                //=====================================================
                // Define Translation Vector
                //=====================================================
                XYZ translationVector = new XYZ(5, 0, 0); // Move +5 feet in X

                //=====================================================
                // Move Element
                //=====================================================
                using (Transaction t = new Transaction(doc, "Move Element"))
                {
                    t.Start();
                    ElementTransformUtils.MoveElement(doc, element.Id, translationVector);
                    t.Commit();
                }

                TaskDialog.Show("Move Result", $"Element {element.Id} moved by {translationVector.X}, {translationVector.Y}, {translationVector.Z}");

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

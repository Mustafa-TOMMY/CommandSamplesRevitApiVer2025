using System;
using System.Collections.Generic;
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
    // CopyOffset XYZ
    //       ↓
    // Transaction
    //       ↓
    // CopyElement()
    //       ↓
    // New ElementId collection
    // ============================================================================

    // Command 03
    [Transaction(TransactionMode.Manual)]
    public class CopyElementCommand : IExternalCommand
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
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select an element to copy");
                Element element = doc.GetElement(selRef);

                //=====================================================
                // Define Copy Offset
                //=====================================================
                XYZ copyOffset = new XYZ(10, 0, 0);

                //=====================================================
                // Copy Element
                //=====================================================
                ICollection<ElementId> newIds;
                using (Transaction t = new Transaction(doc, "Copy Element"))
                {
                    t.Start();
                    newIds = ElementTransformUtils.CopyElement(doc, element.Id, copyOffset);
                    t.Commit();
                }

                string resultStr = "New Element IDs:\n========================================\n";
                foreach (ElementId id in newIds)
                {
                    resultStr += $"{id}\n";
                }

                TaskDialog.Show("Copy Result", resultStr);

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

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
    // Get Location
    //       ↓
    // Cast to LocationPoint OR LocationCurve
    // ============================================================================

    // Command 01
    [Transaction(TransactionMode.ReadOnly)]
    public class InspectLocationCommand : IExternalCommand
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
                Reference selRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select an element to inspect its location");
                Element element = doc.GetElement(selRef);

                //=====================================================
                // Get Location
                //=====================================================
                Location location = element.Location;
                string resultString = $"Element: {element.Name} (ID: {element.Id})\n";
                resultString += "========================================\n";

                if (location is LocationPoint locPoint)
                {
                    resultString += $"Location Type: LocationPoint\n";
                    resultString += $"Point: {XYZToString(locPoint.Point)}\n";
                }
                else if (location is LocationCurve locCurve)
                {
                    resultString += $"Location Type: LocationCurve\n";
                    resultString += $"Start: {XYZToString(locCurve.Curve.GetEndPoint(0))}\n";
                    resultString += $"End: {XYZToString(locCurve.Curve.GetEndPoint(1))}\n";
                }
                else
                {
                    resultString += "Location Type: Other / Not Point or Curve\n";
                }

                TaskDialog.Show("Location Info", resultString);

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

        private string XYZToString(XYZ point)
        {
            return $"({point.X:F2}, {point.Y:F2}, {point.Z:F2})";
        }
    }
}

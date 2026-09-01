using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands
{
    // ============================================================================
    // Transform Inspection Command
    //
    // Fundamental Transform Concept
    //
    // This command demonstrates the basic structure of a Revit Transform:
    //
    // Transform
    //    ├── Origin
    //    ├── BasisX
    //    ├── BasisY
    //    └── BasisZ
    //
    // A Transform describes a coordinate system:
    //
    // Origin
    //    → Where the coordinate system is located.
    //
    // BasisX
    //    → Direction of the local X-axis.
    //
    // BasisY
    //    → Direction of the local Y-axis.
    //
    // BasisZ
    //    → Direction of the local Z-axis.
    //
    // IMPORTANT:
    //
    // This command does NOT modify the selected element.
    // It only inspects and displays its Transform.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class TransformInspectionCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp =
                    commandData.Application;

                UIDocument uiDoc =
                    uiApp.ActiveUIDocument;

                Document doc =
                    uiDoc.Document;

                //=====================================================
                // 1. Select an Element
                //=====================================================

                Reference reference =
                    uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select an element to inspect its Transform");

                Element element =
                    doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Transform",
                        "Could not find the selected element.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Try to obtain the Element Transform
                //
                // Not every Revit Element exposes a direct Transform
                // through the same API.
                //
                // For this fundamental sample, FamilyInstance is used
                // because it provides a clear instance transform.
                //=====================================================

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Transform",
                        "Please select a FamilyInstance.\n\n" +
                        "This fundamental sample uses FamilyInstance " +
                        "because it provides a clear instance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Get Transform
                //=====================================================

                Transform transform = familyInstance.GetTransform();

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Transform",
                        "Could not obtain the Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 4. Read Transform Components
                //=====================================================

                XYZ origin = transform.Origin;

                XYZ basisX = transform.BasisX;

                XYZ basisY = transform.BasisY;

                XYZ basisZ = transform.BasisZ;

                //=====================================================
                // 5. Build Result
                //=====================================================

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("TRANSFORM INSPECTION");

                sb.AppendLine("========================================");

                sb.AppendLine($"Element Id : {element.Id}");

                sb.AppendLine($"Family     : {familyInstance.Symbol.Family.Name}");

                sb.AppendLine($"Type       : {familyInstance.Symbol.Name}");

                sb.AppendLine();

                sb.AppendLine("ORIGIN");

                sb.AppendLine($"X : {origin.X:F4}");

                sb.AppendLine($"Y : {origin.Y:F4}");

                sb.AppendLine($"Z : {origin.Z:F4}");

                sb.AppendLine();

                sb.AppendLine("BASIS X — Local X Axis");

                sb.AppendLine($"X : {basisX.X:F4}");

                sb.AppendLine($"Y : {basisX.Y:F4}");

                sb.AppendLine($"Z : {basisX.Z:F4}");

                sb.AppendLine();

                sb.AppendLine("BASIS Y — Local Y Axis");

                sb.AppendLine($"X : {basisY.X:F4}");

                sb.AppendLine($"Y : {basisY.Y:F4}");

                sb.AppendLine($"Z : {basisY.Z:F4}");

                sb.AppendLine();

                sb.AppendLine("BASIS Z — Local Z Axis");

                sb.AppendLine($"X : {basisZ.X:F4}");

                sb.AppendLine($"Y : {basisZ.Y:F4}");

                sb.AppendLine($"Z : {basisZ.Z:F4}");

                //=====================================================
                // 6. Display Result
                //=====================================================

                TaskDialog.Show(
                    "Transform Inspection",
                    sb.ToString());

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
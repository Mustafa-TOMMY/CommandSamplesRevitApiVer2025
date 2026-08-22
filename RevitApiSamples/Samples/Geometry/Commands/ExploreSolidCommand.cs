using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.Geometry.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 02
    public class ExploreSolidCommand : IExternalCommand
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

                Reference reference = uiDoc.Selection.PickObject(
                                ObjectType.Element,
                                "Select an element");

                Element element = doc.GetElement(reference);

                Options options = new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = false,
                    DetailLevel = ViewDetailLevel.Fine
                };

                GeometryElement geometryElement = element.get_Geometry(options);

                //=====================================================
                int solidCount = geometryElement.OfType<Solid>().Count(s => s.Volume > 0);
                TaskDialog.Show(
                    "Geometry",
                    $"Valid Solids : {solidCount}");

                foreach (GeometryObject geometryObject in geometryElement)
                {
                    if (geometryObject is Solid solid && solid.Volume > 0)
                    {
                        TaskDialog.Show(
                            "Solid",
                            $"Volume : {solid.Volume:F3}\n" +
                            $"Faces : {solid.Faces.Size}\n" +
                            $"Edges : {solid.Edges.Size}");

                        break;
                    }
                }

                // Get faces from the solid
                foreach (GeometryObject geometryObject in geometryElement)
                {
                    if (geometryObject is Solid solid && solid.Volume > 0)
                    {
                        FaceArray faces = solid.Faces;

                        TaskDialog.Show(
                            "Faces",
                            $"Faces Count : {faces.Size}");

                        break;
                    }
                }

                // Get edges from the solid
                foreach (GeometryObject geometryObject in geometryElement)
                {
                    if (geometryObject is Solid solid && solid.Volume > 0)
                    {
                        EdgeArray edges = solid.Edges;

                        TaskDialog.Show(
                            "Edges",
                            $"Edges Count : {edges.Size}");

                        break;
                    }
                }

                // Convert edges to curves
                foreach (GeometryObject geometryObject in geometryElement)
                {
                    if (geometryObject is Solid solid && solid.Volume > 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (Edge edge in solid.Edges)
                        {
                            Curve curve = edge.AsCurve();

                            sb.AppendLine(curve.GetType().Name);
                        }

                        TaskDialog.Show(
                            "Edge Curves",
                            sb.ToString());

                        break;
                    }
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

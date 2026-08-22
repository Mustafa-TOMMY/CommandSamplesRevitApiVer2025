using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitApiSamples.Samples.Geometry.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    // Command 01
    public class ExploreGeometryCommand : IExternalCommand
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

                //=====================================================

                Options options = new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = false,
                    DetailLevel = ViewDetailLevel.Fine
                };

                GeometryElement geometryElement = element.get_Geometry(options);

                StringBuilder sb = new StringBuilder();

                foreach (GeometryObject geometryObject in geometryElement)
                {
                    sb.AppendLine(geometryObject.GetType().Name);
                }

                TaskDialog.Show(
                    "Geometry Objects",
                    sb.ToString());

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

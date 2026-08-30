using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.FiltersAndAdvancedCollection.Commands
{
    // ============================================================================
    // Command 05 — Bounding Box Spatial Filtering (Quick Filters)
    //
    // Demonstrates fast Axis-Aligned Bounding Box (AABB) spatial filtering using:
    //   1. BoundingBoxIntersectsFilter:   Elements whose bounding box touches/overlaps an Outline.
    //   2. BoundingBoxIsInsideFilter:     Elements whose bounding box is strictly inside an Outline.
    //   3. BoundingBoxContainsPointFilter: Elements whose bounding box contains a given 3D point (XYZ).
    //
    // Quick vs. Slow Spatial Filters:
    //   - BoundingBox filters are QUICK filters (they evaluate only min/max coordinates in memory).
    //   - They run in microseconds and should ALWAYS be used as a pre-filter before expensive
    //     3D Solid boolean checks (ElementIntersectsElementFilter / ElementIntersectsSolidFilter).
    //
    // Bounding Box ≠ Exact Geometry:
    //   Two elements can have overlapping bounding boxes without their solids physically touching
    //   (e.g., diagonal pipes, curved walls, L-shaped rooms).
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class BoundingBoxSpatialFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Pick a reference element to define our spatial test region
                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element (e.g., Room, Slab, Wall) to define the spatial search region");

                Element hostElement = doc.GetElement(selRef);
                BoundingBoxXYZ? bbox = hostElement.get_BoundingBox(null);

                if (bbox == null)
                {
                    TaskDialog.Show("Bounding Box Spatial Filter", "The selected element has no bounding box.");
                    return Result.Failed;
                }

                // Calculate center point of the bounding box
                XYZ centerPoint = (bbox.Min + bbox.Max) * 0.5;

                // 2. Create an expanded search zone Outline (+2.0 ft in all directions)
                double expansionFeet = 2.0;
                XYZ searchMin = new XYZ(bbox.Min.X - expansionFeet, bbox.Min.Y - expansionFeet, bbox.Min.Z - expansionFeet);
                XYZ searchMax = new XYZ(bbox.Max.X + expansionFeet, bbox.Max.Y + expansionFeet, bbox.Max.Z + expansionFeet);

                Outline searchOutline = new Outline(searchMin, searchMax);

                // --------------------------------------------------------------------
                // Filter 1: BoundingBoxIntersectsFilter (Touches or overlaps search box)
                // --------------------------------------------------------------------
                BoundingBoxIntersectsFilter intersectsFilter = new BoundingBoxIntersectsFilter(searchOutline);

                IList<Element> intersectingElements = new FilteredElementCollector(doc)
                    .WherePasses(intersectsFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // --------------------------------------------------------------------
                // Filter 2: BoundingBoxIsInsideFilter (Completely enclosed inside search box)
                // --------------------------------------------------------------------
                BoundingBoxIsInsideFilter insideFilter = new BoundingBoxIsInsideFilter(searchOutline);

                IList<Element> strictlyInsideElements = new FilteredElementCollector(doc)
                    .WherePasses(insideFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // --------------------------------------------------------------------
                // Filter 3: BoundingBoxContainsPointFilter (Elements whose bbox covers the center point)
                // --------------------------------------------------------------------
                BoundingBoxContainsPointFilter containsPointFilter = new BoundingBoxContainsPointFilter(centerPoint);

                IList<Element> elementsContainingPoint = new FilteredElementCollector(doc)
                    .WherePasses(containsPointFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 3. Build summary report
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Host Element: [{hostElement.Id.Value}] {hostElement.Name} ({hostElement.Category?.Name})");
                sb.AppendLine($"Search Outline Size: {searchMax.X - searchMin.X:F1} ft × {searchMax.Y - searchMin.Y:F1} ft × {searchMax.Z - searchMin.Z:F1} ft\n");
                sb.AppendLine($"--- 1. BoundingBoxIntersectsFilter ---");
                sb.AppendLine($"Elements touching/overlapping region: {intersectingElements.Count}");
                sb.AppendLine($"\n--- 2. BoundingBoxIsInsideFilter ---");
                sb.AppendLine($"Elements strictly enclosed in region: {strictlyInsideElements.Count}");
                sb.AppendLine($"\n--- 3. BoundingBoxContainsPointFilter ---");
                sb.AppendLine($"Elements whose bounding box contains center point ({centerPoint.X:F1}, {centerPoint.Y:F1}, {centerPoint.Z:F1}): {elementsContainingPoint.Count}");

                TaskDialog.Show("Bounding Box Spatial Filters (Quick)", sb.ToString());

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

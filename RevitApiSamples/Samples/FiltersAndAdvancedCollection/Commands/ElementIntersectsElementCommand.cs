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
    // Command 06 — Element-to-Element 3D Intersection (ElementIntersectsElementFilter)
    //
    // Demonstrates true 3D solid geometry collision detection against a live Revit element.
    //
    // How It Works:
    //   1. Revit internally extracts the 3D solid geometry of the selected target element.
    //   2. ElementIntersectsElementFilter checks each candidate element for volumetric intersection.
    //
    // Best Practice Pattern:
    //   Always pair this SLOW filter with QUICK filters:
    //   - ExclusionFilter: Exclude the target element itself (so it doesn't clash with itself).
    //   - BoundingBoxIntersectsFilter: Discard elements whose bounding boxes don't touch.
    //   - ElementIntersectsElementFilter: Precise 3D boolean collision check.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class ElementIntersectsElementCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Pick a Target Element to test clashes against
                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element (e.g., Pipe, Duct, Beam, Wall) to check 3D solid collisions against");

                Element targetElement = doc.GetElement(selRef);
                BoundingBoxXYZ? bbox = targetElement.get_BoundingBox(null);

                if (bbox == null)
                {
                    TaskDialog.Show("Element Intersection", "The selected element has no 3D geometry.");
                    return Result.Failed;
                }

                // 2. Step 1 of Best Practice: Quick Bounding Box Pre-Filter
                Outline outline = new Outline(bbox.Min, bbox.Max);
                BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(outline);

                // 3. Step 2 of Best Practice: Exclude the target element itself
                ExclusionFilter selfExclusion = new ExclusionFilter(new List<ElementId> { targetElement.Id });

                // 4. Step 3: The True 3D Solid Collision Filter
                ElementIntersectsElementFilter solidCollisionFilter = new ElementIntersectsElementFilter(targetElement);

                // 5. Execute Collector chaining: Quick -> Exclusion -> 3D Slow Filter
                IList<Element> clashingElements = new FilteredElementCollector(doc)
                    .WherePasses(bboxFilter)            // 1. Quick AABB filter (Fast C++)
                    .WherePasses(selfExclusion)         // 2. Exclude self (Fast C++)
                    .WherePasses(solidCollisionFilter)  // 3. Precise 3D Boolean test (C++)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 6. Report results
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Target Element: [{targetElement.Id.Value}] {targetElement.Name} ({targetElement.Category?.Name})");
                sb.AppendLine($"Total 3D Intersecting Clashes: {clashingElements.Count}\n");

                if (clashingElements.Count > 0)
                {
                    sb.AppendLine("Clashing Elements Breakdown:");
                    var grouped = clashingElements.GroupBy(e => e.Category?.Name ?? "Other");
                    foreach (var group in grouped)
                    {
                        sb.AppendLine($" • {group.Key} ({group.Count()}):");
                        foreach (var elem in group.Take(5))
                        {
                            sb.AppendLine($"     - [{elem.Id.Value}] {elem.Name}");
                        }
                        if (group.Count() > 5)
                        {
                            sb.AppendLine($"     - ... and {group.Count() - 5} more");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("✅ No 3D physical collisions detected with other model elements.");
                }

                TaskDialog.Show("Element-to-Element 3D Intersection", sb.ToString());

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

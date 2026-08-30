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
    // Command 04 — Exclusion Filtering (ExclusionFilter)
    //
    // Demonstrates how to exclude a specific set of elements (by ElementId) from
    // a collector query at the native Revit database level.
    //
    // Why use ExclusionFilter?
    //   - Instead of collecting everything and doing C# LINQ `.Where(e => !selectedIds.Contains(e.Id))`,
    //     ExclusionFilter skips those elements during the internal database scan.
    //   - Eliminates unnecessary .NET proxy allocations for excluded elements.
    //
    // Real-World Scenario:
    //   Select elements to exclude (e.g. approved clashes, temporary elements), and
    //   retrieve all remaining model elements of a category in one native operation.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class ExclusionFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Get pre-selected element IDs or prompt user to pick elements to exclude
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                if (selectedIds.Count == 0)
                {
                    IList<Reference> pickedRefs = uiDoc.Selection.PickObjects(
                        ObjectType.Element,
                        "Select one or more elements to EXCLUDE from the query (Click Finish when done)");

                    selectedIds = pickedRefs.Select(r => r.ElementId).ToList();
                }

                if (selectedIds.Count == 0)
                {
                    TaskDialog.Show("Exclusion Filter", "No elements were selected for exclusion.");
                    return Result.Cancelled;
                }

                // 2. Base query: All physical Wall instances in the model
                FilteredElementCollector totalWallCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType();

                int totalWallsCount = totalWallCollector.GetElementCount();

                // 3. Apply ExclusionFilter
                ExclusionFilter exclusionFilter = new ExclusionFilter(selectedIds);

                IList<Element> remainingWalls = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .WherePasses(exclusionFilter)
                    .ToElements();

                // 4. Report results
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Total Model Walls: {totalWallsCount}");
                sb.AppendLine($"Excluded Element IDs Count: {selectedIds.Count}");
                sb.AppendLine($"Remaining Walls After Exclusion: {remainingWalls.Count}\n");

                sb.AppendLine("Excluded IDs:");
                foreach (ElementId id in selectedIds.Take(5))
                {
                    sb.AppendLine($" • Element ID: {id.Value}");
                }
                if (selectedIds.Count > 5)
                {
                    sb.AppendLine($" • ... and {selectedIds.Count - 5} more");
                }

                TaskDialog.Show("Exclusion Filter", sb.ToString());

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitApiSamples.Samples.FiltersAndAdvancedCollection.Commands
{
    // ============================================================================
    // Command 01 — Logical Filters (LogicalAndFilter & LogicalOrFilter)
    //
    // Demonstrates how to combine multiple element filters using Boolean logic
    // (AND / OR) directly inside Revit's native C++ query engine.
    //
    // Key Concept:
    //   - LogicalAndFilter: Passes elements satisfying ALL contained filters.
    //   - LogicalOrFilter:  Passes elements satisfying ANY contained filter.
    //
    // Real-World Scenario:
    //   Find all Columns OR Framing elements (Structural Beams) that reside
    //   on a specific Level in the active project.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class LogicalFiltersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Get the active view's level or first available level
                Level? targetLevel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .FirstOrDefault();

                if (targetLevel == null)
                {
                    TaskDialog.Show("Logical Filters", "No Levels found in the current project.");
                    return Result.Failed;
                }

                // 2. Build Category Filters for Columns and Structural Framing
                ElementCategoryFilter columnCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
                ElementCategoryFilter structuralColumnCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
                ElementCategoryFilter framingCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);

                // 3. Combine with LogicalOrFilter (Columns OR Structural Columns OR Framing)
                IList<ElementFilter> structuralFilters = new List<ElementFilter>
                {
                    columnCategoryFilter,
                    structuralColumnCategoryFilter,
                    framingCategoryFilter
                };
                LogicalOrFilter structuralElementsFilter = new LogicalOrFilter(structuralFilters);

                // 4. Build Level Filter (Elements whose level parameter matches targetLevel.Id)
                ElementLevelFilter levelFilter = new ElementLevelFilter(targetLevel.Id);

                // 5. Combine with LogicalAndFilter: (Columns OR Framing) AND (On Target Level)
                LogicalAndFilter combinedFilter = new LogicalAndFilter(structuralElementsFilter, levelFilter);

                // 6. Execute Native Collector with the composite filter
                IList<Element> results = new FilteredElementCollector(doc)
                    .WherePasses(combinedFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 7. Display Results Breakdown
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Target Level: {targetLevel.Name} (ID: {targetLevel.Id.Value})");
                sb.AppendLine($"Total Matched Elements: {results.Count}\n");

                var groupedByCategory = results.GroupBy(e => e.Category?.Name ?? "Uncategorized");
                foreach (var group in groupedByCategory)
                {
                    sb.AppendLine($" • {group.Key}: {group.Count()} element(s)");
                }

                TaskDialog.Show("Logical Filters (AND / OR)", sb.ToString());

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

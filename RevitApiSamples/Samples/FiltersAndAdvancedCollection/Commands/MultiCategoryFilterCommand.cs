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
    // Command 03 — Multi-Category Filtering (ElementMulticategoryFilter)
    //
    // Demonstrates how to query multiple categories in a single native collector pass.
    //
    // Performance Advantage:
    //   Instead of instantiating 4 separate collectors for Pipes, Ducts, Cable Trays,
    //   and Conduit, ElementMulticategoryFilter checks against the category list
    //   internally in C++ in one single scan of the database.
    //
    // Real-World Scenario:
    //   Collect all MEP distribution elements (Ducts, Pipes, Cable Trays, Conduits)
    //   in one operation for clash checking or quantification.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class MultiCategoryFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Define the collection of target MEP distribution categories
                ICollection<BuiltInCategory> mepCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_DuctCurves,
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_CableTray,
                    BuiltInCategory.OST_Conduit,
                    BuiltInCategory.OST_DuctFitting,
                    BuiltInCategory.OST_PipeFitting
                };

                // 2. Instantiate ElementMulticategoryFilter
                ElementMulticategoryFilter multiCategoryFilter = new ElementMulticategoryFilter(mepCategories);

                // 3. Execute Native FilteredElementCollector
                IList<Element> mepElements = new FilteredElementCollector(doc)
                    .WherePasses(multiCategoryFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 4. Summarize results
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Total MEP Distribution Elements: {mepElements.Count}\n");

                var categoryCounts = mepElements
                    .GroupBy(e => e.Category?.Name ?? "Unknown Category")
                    .OrderByDescending(g => g.Count());

                foreach (var group in categoryCounts)
                {
                    sb.AppendLine($" • {group.Key}: {group.Count()} element(s)");
                }

                TaskDialog.Show("Multi-Category Filter", sb.ToString());

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

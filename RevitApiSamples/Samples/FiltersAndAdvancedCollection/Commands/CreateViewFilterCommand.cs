using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RevitApiSamples.Samples.FiltersAndAdvancedCollection.Commands
{
    // ============================================================================
    // Create and Apply View Filter (ParameterFilterElement)
    //
    // This command demonstrates how to programmatically create a persistent View
    // Filter (ParameterFilterElement) in the Revit database and apply it to the
    // active view's Visibility/Graphic Overrides (VV / VG).
    //
    // Workflow:
    //
    // 1. Define Target Categories (e.g., Walls, Sections, or Ducts)
    // 2. Build Rule-Based Criteria using ElementParameterFilter & ParameterFilterRuleFactory
    // 3. Open a Transaction (Creating ParameterFilterElement modifies the database)
    // 4. Create or retrieve ParameterFilterElement via ParameterFilterElement.Create()
    // 5. Apply the filter to the Active View:
    //    - view.AddFilter(filterElement.Id)
    //    - view.SetFilterOverrides(filterElement.Id, overrideGraphicSettings)
    //    - view.SetFilterVisibility(filterElement.Id, true/false)
    // 6. Commit Transaction & display result
    //
    // Distinction:
    //
    // - ElementParameterFilter: An IN-MEMORY query filter used with FilteredElementCollector.
    // - ParameterFilterElement: A PERSISTENT database element stored in the .rvt file,
    //                           visible in the Visibility/Graphics dialog (VV / VG -> Filters).
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 09
    public class CreateViewFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;
                var activeView = doc.ActiveView;

                // ====================================================================
                // Validation: Check if Active View supports Graphic Overrides & Filters
                // ====================================================================
                if (activeView == null)
                {
                    TaskDialog.Show("Create View Filter", "Error: No active view was found.");
                    return Result.Failed;
                }

                if (!activeView.AreGraphicsOverridesAllowed())
                {
                    TaskDialog.Show("Create View Filter", 
                        $"The current active view '{activeView.Name}' ({activeView.ViewType}) does not support graphic overrides or view filters.");
                    return Result.Cancelled;
                }

                // ====================================================================
                // Step 1: Define Target Categories
                // ====================================================================
                // Specify which element categories this View Filter will apply to.
                // For this sample, we target Walls.
                // ====================================================================
                ICollection<ElementId> targetCategories = new List<ElementId>
                {
                    new ElementId(BuiltInCategory.OST_Walls)
                };

                // ====================================================================
                // Step 2: Build the Rule-Based Criteria (ElementParameterFilter)
                // ====================================================================
                // Example Rule: Walls whose "Comments" contains the keyword "Fire"
                // (Case-insensitive check)
                // ====================================================================
                ElementId commentsParamId = new ElementId(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                FilterRule containsRule = ParameterFilterRuleFactory.CreateContainsRule(
                    commentsParamId, 
                    "Fire", 
                    caseSensitive: false);

                ElementParameterFilter parameterCriteria = new ElementParameterFilter(containsRule);

                // ====================================================================
                // Step 3: Open Transaction to Create and Apply the Filter
                // ====================================================================
                string filterName = "Walls - Fire Safety Check";
                ParameterFilterElement filterElement = null;

                using (Transaction t = new Transaction(doc, "Create and Apply View Filter"))
                {
                    t.Start();

                    // Check if a ParameterFilterElement with this name already exists
                    filterElement = new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>()
                        .FirstOrDefault(pfe => pfe.Name.Equals(filterName, StringComparison.OrdinalIgnoreCase));

                    if (filterElement == null)
                    {
                        // Create the new persistent View Filter in the Revit database
                        filterElement = ParameterFilterElement.Create(
                            doc, 
                            filterName, 
                            targetCategories, 
                            parameterCriteria);
                    }
                    else
                    {
                        // Update existing filter with new criteria and categories
                        filterElement.SetCategories(targetCategories);
                        filterElement.SetElementFilter(parameterCriteria);
                    }

                    // ================================================================
                    // Step 4: Add Filter to the Active View (if not already added)
                    // ================================================================
                    if (!activeView.IsFilterApplied(filterElement.Id))
                    {
                        activeView.AddFilter(filterElement.Id);
                    }

                    // ================================================================
                    // Step 5: Configure Graphic Overrides for the Filter in this View
                    // ================================================================
                    // Set Red projection line color and heavier line weight (e.g. 5)
                    // ================================================================
                    OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
                    
                    // Highlight matching walls in RED
                    overrideSettings.SetProjectionLineColor(new Color(255, 0, 0));
                    overrideSettings.SetProjectionLineWeight(5);

                    // Apply the graphic override settings to the filter in the active view
                    activeView.SetFilterOverrides(filterElement.Id, overrideSettings);

                    // Ensure the filter is set to visible (true = display with overrides, false = hide)
                    activeView.SetFilterVisibility(filterElement.Id, true);

                    t.Commit();
                }

                // ====================================================================
                // Step 6: Display Success Summary Dialog
                // ====================================================================
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("VIEW FILTER CREATED & APPLIED SUCCESSFULLY!");
                sb.AppendLine("========================================");
                sb.AppendLine($"Filter Name:       {filterElement.Name}");
                sb.AppendLine($"Filter Element Id: {filterElement.Id.Value}");
                sb.AppendLine($"Applied to View:   {activeView.Name} ({activeView.ViewType})");
                sb.AppendLine($"Target Category:   Walls (OST_Walls)");
                sb.AppendLine($"Rule Criteria:     Comments contains 'Fire'");
                sb.AppendLine($"Graphic Override:  Projection Lines -> RED, Weight 5");
                sb.AppendLine($"Filter Visibility: Visible (true)");
                sb.AppendLine("========================================");
                sb.AppendLine("Check the 'Visibility/Graphic Overrides' dialog (VV / VG) -> 'Filters' tab in Revit to view the newly added filter.");

                TaskDialog.Show("Create View Filter", sb.ToString());

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

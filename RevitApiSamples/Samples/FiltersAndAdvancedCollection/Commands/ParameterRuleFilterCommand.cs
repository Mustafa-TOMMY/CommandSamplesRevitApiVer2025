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
    // Command 02 — Parameter Rule Filtering (ElementParameterFilter)
    //
    // Demonstrates how to evaluate element parameter values natively at the C++
    // database level using ParameterFilterRuleFactory and ElementParameterFilter.
    //
    // Why use ElementParameterFilter instead of C# LINQ?
    //   - LINQ (.Where(x => x.LookupParameter("...").AsString() == "...")) forces
    //     Revit to instantiate every managed .NET Element proxy in memory.
    //   - ElementParameterFilter evaluates properties directly in native memory,
    //     making queries 10x - 50x faster on large models.
    //
    // Examples Shown:
    //   1. String Rule: Elements with "Comments" containing a specific keyword.
    //   2. Numeric Rule: Walls with Length >= 10.0 feet.
    //   3. Inverted Rule: Finding elements where a parameter does NOT equal a value.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class ParameterRuleFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                StringBuilder sb = new StringBuilder();

                // --------------------------------------------------------------------
                // 1. Numeric Rule: Find Walls with Length >= 10 feet
                // --------------------------------------------------------------------
                ElementId lengthParamId = new ElementId(BuiltInParameter.CURVE_ELEM_LENGTH);
                double minLengthFeet = 10.0;
                double tolerance = 0.001;

                FilterRule lengthRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(
                    lengthParamId,
                    minLengthFeet,
                    tolerance);

                ElementParameterFilter wallLengthFilter = new ElementParameterFilter(lengthRule);

                IList<Element> longWalls = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .WherePasses(wallLengthFilter)
                    .ToElements();

                sb.AppendLine($"--- 1. Numeric Rule (Walls Length >= {minLengthFeet} ft) ---");
                sb.AppendLine($"Matched: {longWalls.Count} wall(s)\n");

                // --------------------------------------------------------------------
                // 2. String Rule: Find Doors with Mark starting with "D"
                // --------------------------------------------------------------------
                ElementId markParamId = new ElementId(BuiltInParameter.DOOR_NUMBER);

                FilterRule markBeginsWithRule = ParameterFilterRuleFactory.CreateBeginsWithRule(
                    markParamId,
                    "D",
                    caseSensitive: false);

                ElementParameterFilter doorMarkFilter = new ElementParameterFilter(markBeginsWithRule);

                IList<Element> matchedDoors = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .WherePasses(doorMarkFilter)
                    .ToElements();

                sb.AppendLine($"--- 2. String Rule (Doors Mark begins with 'D') ---");
                sb.AppendLine($"Matched: {matchedDoors.Count} door(s)\n");

                // --------------------------------------------------------------------
                // 3. Inverted Rule: Find Elements where Comments is NOT empty
                // --------------------------------------------------------------------
                ElementId commentsParamId = new ElementId(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                FilterRule emptyCommentsRule = ParameterFilterRuleFactory.CreateEqualsRule(
                    commentsParamId,
                    string.Empty,
                    caseSensitive: false);

                // Setting inverted = true finds all elements where Comments != ""
                ElementParameterFilter hasCommentsFilter = new ElementParameterFilter(
                    emptyCommentsRule,
                    inverted: true);

                IList<Element> elementsWithComments = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(hasCommentsFilter)
                    .ToElements();

                sb.AppendLine($"--- 3. Inverted Rule (Elements with Non-Empty Comments) ---");
                sb.AppendLine($"Matched: {elementsWithComments.Count} element(s)");

                TaskDialog.Show("Parameter Rule Filtering", sb.ToString());

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

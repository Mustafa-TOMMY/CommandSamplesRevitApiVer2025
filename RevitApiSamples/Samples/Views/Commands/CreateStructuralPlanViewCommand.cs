using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using System.Text;

namespace RevitApiSamples.Samples.Views.Commands
{
    // ============================================================================
    // Create Structural Plan View
    //
    // This command demonstrates how to programmatically create a new Structural
    // Plan View in Revit using the Revit API.
    //
    // Workflow:
    //
    // 1. Find ViewFamilyType for Structural Plans (ViewFamily.StructuralPlan)
    // 2. Find target Level (e.g. Level 1)
    // 3. Open a Transaction (View creation modifies the database)
    // 4. Call ViewPlan.Create(doc, viewFamilyTypeId, levelId)
    // 5. Configure View properties:
    //    - Name (with collision handling)
    //    - Scale (e.g. 1:50)
    //    - DetailLevel (Fine)
    //    - DisplayStyle (HiddenLine)
    // 6. Commit Transaction
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 06
    public class CreateStructuralPlanViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                // ====================================================================
                // Step 1: Find the ViewFamilyType for Structural Plan
                // ====================================================================
                // A ViewFamilyType is the Type definition required by Revit to know
                // how to construct a specific view family (like WallType for Walls).
                // ====================================================================
                ViewFamilyType structuralPlanType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.StructuralPlan);

                // Fallback: If template has no StructuralPlan type, try FloorPlan
                if (structuralPlanType == null)
                {
                    structuralPlanType = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
                }

                if (structuralPlanType == null)
                {
                    TaskDialog.Show("Create View", "Error: No suitable ViewFamilyType was found in the project.");
                    return Result.Failed;
                }

                // ====================================================================
                // Step 2: Find a Target Level
                // ====================================================================
                // Plan views (Floor Plans, Ceiling Plans, Structural Plans) require
                // a reference Level (GenLevel) to establish their horizontal cut plane.
                // ====================================================================
                Level targetLevel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault();

                if (targetLevel == null)
                {
                    TaskDialog.Show("Create View", "Error: No Levels found in the project. Cannot create plan view.");
                    return Result.Failed;
                }

                // ====================================================================
                // Step 3: Open Transaction & Create the View
                // ====================================================================
                ViewPlan newStructuralPlan = null;
                string requestedName = $"Structural Plan - {targetLevel.Name} - Automated";

                using (Transaction t = new Transaction(doc, "Create Structural Plan View"))
                {
                    t.Start();

                    // Create the plan view instance
                    newStructuralPlan = ViewPlan.Create(doc, structuralPlanType.Id, targetLevel.Id);

                    if (newStructuralPlan == null)
                    {
                        t.RollBack();
                        TaskDialog.Show("Create View", "Failed to create the structural plan view.");
                        return Result.Failed;
                    }

                    // ================================================================
                    // Step 4: Configure View Name (Handle Name Uniqueness)
                    // ================================================================
                    // Revit requires view names to be globally unique in the project.
                    // If a view with this name already exists, append a timestamp.
                    // ================================================================
                    string finalName = requestedName;
                    bool nameExists = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .Any(v => v.Name.Equals(finalName, StringComparison.OrdinalIgnoreCase));

                    if (nameExists)
                    {
                        finalName = $"{requestedName} ({DateTime.Now:HHmmss})";
                    }

                    newStructuralPlan.Name = finalName;

                    // ================================================================
                    // Step 5: Configure View Graphic & Scale Properties
                    // ================================================================
                    // Scale: 50 represents 1:50 drawing scale
                    newStructuralPlan.Scale = 50;

                    // Detail Level: Coarse, Medium, or Fine
                    newStructuralPlan.DetailLevel = ViewDetailLevel.Fine;

                    // Display Style: Wireframe, HLR (Hidden Line Removal), Shading, etc.
                    newStructuralPlan.DisplayStyle = DisplayStyle.HLR;

                    // Commit changes to database
                    t.Commit();
                }

                // ====================================================================
                // Step 6: Build User Report Dialog
                // ====================================================================
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("NEW VIEW CREATED SUCCESSFULLY!");
                sb.AppendLine("========================================");
                sb.AppendLine($"View Name:     {newStructuralPlan.Name}");
                sb.AppendLine($"Element Id:    {newStructuralPlan.Id.Value}");
                sb.AppendLine($"View Type:     {newStructuralPlan.ViewType}");
                sb.AppendLine($"View Family:   {structuralPlanType.ViewFamily}");
                sb.AppendLine($"Family Type:   {structuralPlanType.Name}");
                sb.AppendLine($"Assoc. Level:  {targetLevel.Name}");
                sb.AppendLine($"Scale:         1:{newStructuralPlan.Scale}");
                sb.AppendLine($"Detail Level:  {newStructuralPlan.DetailLevel}");
                sb.AppendLine($"Display Style: {newStructuralPlan.DisplayStyle}");
                sb.AppendLine("========================================");

                TaskDialog.Show("Create Structural Plan", sb.ToString());

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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Create Family Parameter
    //
    // This command demonstrates how to create a Parameter inside a Family.
    //
    // Workflow:
    //
    // Select Family Instance
    //      ↓
    // Get Family
    //      ↓
    // Open Family Document
    //      ↓
    // FamilyManager
    //      ↓
    // AddParameter()
    //      ↓
    // Instance / Type
    //
    // Important:
    //
    // This is different from Project Parameters.
    //
    // Project Parameter:
    //
    // Project Document
    //      ↓
    // ParameterBindings
    //
    // Family Parameter:
    //
    // Family Document
    //      ↓
    // FamilyManager
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 12
    public class CreateFamilyParameterCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                //=====================================================
                // Select Family Instance

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a Family Instance");

                FamilyInstance familyInstance = doc.GetElement(reference) as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Create Family Parameter",
                        "Please select a Family Instance.");

                    return Result.Failed;
                }

                //=====================================================
                // Get Family

                Family family = familyInstance.Symbol.Family;

                if (family == null)
                {
                    TaskDialog.Show(
                        "Create Family Parameter",
                        "Family was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Open Family Document

                Document familyDocument = doc.EditFamily(family);

                if (familyDocument == null)
                {
                    TaskDialog.Show(
                        "Create Family Parameter",
                        "Could not open the Family document.");

                    return Result.Failed;
                }

                try
                {
                    //=================================================
                    // Get Family Manager
                    FamilyManager familyManager = familyDocument.FamilyManager;

                    //=================================================
                    // Parameter Information
                    string parameterName = "Company_Code";

                    //=================================================
                    // Check if Parameter Already Exists
                    FamilyParameter existingParameter = familyManager.get_Parameter(parameterName);

                    if (existingParameter != null)
                    {
                        TaskDialog.Show(
                            "Create Family Parameter",
                            $"Parameter '{parameterName}' already exists.");

                        return Result.Cancelled;
                    }

                    //=================================================
                    // Create Family Parameter
                    // This sample creates a Text parameter.
                    // Make the parameter an Instance Parameter.

                    using (Transaction transaction = new Transaction(familyDocument, "Create Family Parameter"))
                    {
                        transaction.Start();

                        FamilyParameter familyParameter =
                            familyManager.AddParameter(
                                parameterName,
                                GroupTypeId.Data,
                                SpecTypeId.String.Text,
                                true);

                        transaction.Commit();
                    }

                    //=================================================

                    TaskDialog.Show(
                        "Create Family Parameter",
                        $"Family Parameter created successfully.\n\n" +
                        $"Family:\n{family.Name}\n\n" +
                        $"Parameter:\n{parameterName}\n\n" +
                        $"Type:\nInstance");

                    //=================================================
                    // Close Family Document
                    // Do not save changes back to the original family
                    // in this educational sample.

                    familyDocument.Close(false);
                }
                catch
                {
                    familyDocument.Close(false);
                    throw;
                }

                //=====================================================

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
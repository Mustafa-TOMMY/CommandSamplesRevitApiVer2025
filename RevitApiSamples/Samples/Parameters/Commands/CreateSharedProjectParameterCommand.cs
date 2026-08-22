using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Create Shared Project Parameter
    //
    // This command demonstrates how to create a Shared Parameter and bind it
    // to a Project Category.
    //
    // Workflow:
    //
    // Shared Parameters File
    //          ↓
    // DefinitionGroup
    //          ↓
    // ExternalDefinition
    //          ↓
    // CategorySet
    //          ↓
    // InstanceBinding / TypeBinding
    //          ↓
    // Document.ParameterBindings
    //
    // Example:
    //
    // Parameter:
    //     Company_Code
    //
    // Category:
    //     Walls
    //
    // Binding:
    //     Instance
    //
    // Result:
    //
    // Walls
    //     ↓
    // Company_Code
    //     ↓
    // Instance Parameter
    // ============================================================================

    [Transaction(TransactionMode.Manual)]
    // Command 11
    public class CreateSharedProjectParameterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;
                var doc = uiDoc.Document;

                //=====================================================
                // Shared Parameters File
                // Revit must already have a Shared Parameters file
                // configured.

                string sharedParameterFilePath = app.SharedParametersFilename;

                if (string.IsNullOrWhiteSpace(sharedParameterFilePath))
                {
                    TaskDialog.Show(
                        "Create Shared Parameter",
                        "No Shared Parameters file is configured in Revit.");

                    return Result.Failed;
                }

                //=====================================================
                // Open Shared Parameters File

                DefinitionFile definitionFile = app.OpenSharedParameterFile();

                if (definitionFile == null)
                {
                    TaskDialog.Show(
                        "Create Shared Parameter",
                        "Could not open the Shared Parameters file.");

                    return Result.Failed;
                }

                //=====================================================
                // Parameter Information
                //
                // For this educational sample we create:
                //
                // Company_Code
                //
                // as a Text parameter.

                string parameterName = "Company_Code";

                //=====================================================
                // Get or Create Definition Group

                string groupName = "Company Parameters";

                DefinitionGroup definitionGroup = definitionFile.Groups.get_Item(groupName);

                if (definitionGroup == null)
                {
                    definitionGroup = definitionFile.Groups.Create(groupName);
                }

                //=====================================================
                // Get Existing Definition

                ExternalDefinition externalDefinition =
                    definitionGroup.Definitions.get_Item(parameterName) as ExternalDefinition;

                //=====================================================
                // Create Definition if it does not exist

                if (externalDefinition == null)
                {
                    ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(
                            parameterName,
                            SpecTypeId.String.Text);

                    options.Description = "Company project identification code.";

                    options.UserModifiable = true;

                    externalDefinition =
                        definitionGroup.Definitions.Create(options) as ExternalDefinition;
                }

                if (externalDefinition == null)
                {
                    TaskDialog.Show(
                        "Create Shared Parameter",
                        "Could not create the Shared Parameter definition.");

                    return Result.Failed;
                }

                //=====================================================
                // Category
                //
                // For this sample we bind the parameter to Walls.

                Category wallCategory = Category.GetCategory(
                        doc,
                        BuiltInCategory.OST_Walls);

                if (wallCategory == null)
                {
                    TaskDialog.Show(
                        "Create Shared Parameter",
                        "Walls category was not found.");

                    return Result.Failed;
                }

                //=====================================================
                // Create CategorySet

                CategorySet categorySet = app.Create.NewCategorySet();

                categorySet.Insert(wallCategory);

                //=====================================================
                // Choose Binding
                //
                // Change this line to:
                //
                // NewTypeBinding(categorySet)
                //
                // if you want a Type Parameter.

                ElementBinding binding = app.Create.NewInstanceBinding(categorySet);
                // in case create type binding, use the following line instead
                //ElementBinding binding =app.Create.NewTypeBinding(categorySet);

                //=====================================================
                // Create / Update Project Binding

                using (Transaction transaction = new Transaction(doc, "Create Shared Project Parameter"))
                {
                    transaction.Start();

                    bool success =
                        doc.ParameterBindings.Insert(
                            externalDefinition,
                            binding,
                            GroupTypeId.Data);

                    transaction.Commit();

                    if (!success)
                    {
                        TaskDialog.Show(
                            "Create Shared Parameter",
                            "The parameter could not be bound to the project.");

                        return Result.Failed;
                    }
                }

                //=====================================================
                // Result

                string bindingType = binding is InstanceBinding ? "Instance" : binding is TypeBinding
                            ? "Type"
                            : "Unknown";

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Shared Project Parameter Created");

                sb.AppendLine();

                sb.AppendLine($"Name:\n{externalDefinition.Name}");

                sb.AppendLine();

                sb.AppendLine($"GUID:\n{externalDefinition.GUID}");

                sb.AppendLine();

                sb.AppendLine($"Category:\n{wallCategory.Name}");

                sb.AppendLine();

                sb.AppendLine($"Binding:\n{bindingType}");

                sb.AppendLine();

                sb.AppendLine($"Group:\nCompany Parameters");

                TaskDialog.Show(
                    "Create Shared Parameter",
                    sb.ToString());

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
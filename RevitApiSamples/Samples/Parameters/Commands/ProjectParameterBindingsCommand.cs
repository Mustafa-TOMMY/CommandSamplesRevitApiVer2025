using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Text;

namespace RevitApiSamples.Samples.Parameters.Commands
{
    // ============================================================================
    // Project Parameter Bindings
    //
    // This command demonstrates how Project Parameters are bound to Categories.
    //
    // A Project Parameter does not simply exist by itself.
    //
    // It is associated with:
    //
    //     Parameter Definition
    //            ↓
    //     Binding
    //            ↓
    //     Categories
    //
    // The binding can be:
    //
    //     Instance Binding
    //            OR
    //
    //     Type Binding
    //
    // Workflow:
    //
    // Document
    //     ↓
    // ParameterBindings
    //     ↓
    // Definition
    //     ↓
    // Binding
    //     ↓
    // Categories
    //
    // Important:
    //
    // ParameterBindings represents the relationship between
    // Project Parameter definitions and Categories in the project.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    // Command 07
    public class ProjectParameterBindingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;
                var app = uiApp.Application;

                //=====================================================
                // Get Project Parameter Bindings

                BindingMap bindingMap = doc.ParameterBindings;
                //=====================================================

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Project: {doc.Title}");
                sb.AppendLine();
                sb.AppendLine("Project Parameter Bindings:");
                sb.AppendLine("========================================");
                int bindingCount = 0;

                //=====================================================
                // Iterate through all bindings

                DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();

                while (iterator.MoveNext())
                {
                    Definition definition = iterator.Key;

                    ElementBinding binding = iterator.Current as ElementBinding;

                    if (definition == null || binding == null)
                        continue;

                    bindingCount++;

                    //=================================================
                    // Parameter Definition

                    sb.AppendLine(
                        $"Parameter:\n" +
                        $"{definition.Name}");

                    //=================================================
                    // Binding Type

                    string bindingType;

                    if (binding is InstanceBinding)
                    {
                        bindingType = "Instance";
                    }
                    else if (binding is TypeBinding)
                    {
                        bindingType = "Type";
                    }
                    else
                    {
                        bindingType = "Unknown";
                    }

                    sb.AppendLine(
                        $"Binding Type:\n" +
                        $"{bindingType}");

                    //=================================================
                    // Categories

                    sb.AppendLine("Categories:");

                    foreach (Category category in binding.Categories)
                    {
                        sb.AppendLine($"  - {category.Name}");
                    }

                    sb.AppendLine("----------------------------------------");
                }

                //=====================================================

                if (bindingCount == 0)
                {
                    sb.AppendLine("No project parameter bindings found.");
                }
                else
                {
                    sb.Insert(
                        sb.ToString().IndexOf(
                            "Project Parameter Bindings:") +
                            "Project Parameter Bindings:".Length,
                        $"\nCount: {bindingCount}\n");
                }

                //=====================================================

                TaskDialog.Show(
                    "Project Parameter Bindings",
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
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
    // Command 08 — Linked Model Clash Intersection (ElementIntersectsSolidFilter)
    //
    // Demonstrates cross-document collision detection between Linked Model elements
    // and Host Document elements.
    //
    // The Core Challenge:
    //   - ElementIntersectsElementFilter cannot be used across documents because
    //     linked elements live in their own local coordinate space.
    //
    // The Solution Architecture:
    //   1. User picks an element from a Linked Model (ObjectType.LinkedElement).
    //   2. Get the RevitLinkInstance to obtain its Total Transform (Translation/Rotation).
    //   3. Extract the raw 3D Solid from the linked element's geometry.
    //   4. Apply SolidUtils.CreateTransformed(solid, transform) to map the solid into
    //      the Host Document's world coordinates.
    //   5. Pass the transformed Solid into ElementIntersectsSolidFilter(hostDoc).
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class LinkedModelIntersectionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document hostDoc = uiDoc.Document;

                // 1. Prompt user to select an element inside a Linked Model
                Reference linkedRef = uiDoc.Selection.PickObject(
                    ObjectType.LinkedElement,
                    "Select an element inside a Linked Model (e.g., Structural Column or Wall in Link)");

                // 2. Retrieve the RevitLinkInstance and its document
                Element linkElement = hostDoc.GetElement(linkedRef);
                if (linkElement is not RevitLinkInstance linkInstance)
                {
                    TaskDialog.Show("Linked Intersection", "The selected element is not part of a Revit Link.");
                    return Result.Failed;
                }

                Document linkDoc = linkInstance.GetLinkDocument();
                if (linkDoc == null)
                {
                    TaskDialog.Show("Linked Intersection", "The linked document is not currently loaded.");
                    return Result.Failed;
                }

                // 3. Get the linked element using LinkedElementId
                Element linkedTargetElement = linkDoc.GetElement(linkedRef.LinkedElementId);
                Autodesk.Revit.DB.Transform linkTransform = linkInstance.GetTotalTransform();

                // 4. Extract 3D Solid from the linked element
                Solid? linkedSolid = ExtractSolid(linkedTargetElement);
                if (linkedSolid == null || linkedSolid.Volume <= 0.001)
                {
                    TaskDialog.Show("Linked Intersection", "Could not extract valid 3D solid geometry from the linked element.");
                    return Result.Failed;
                }

                // 5. Transform the Solid into Host Model World Coordinates
                Solid transformedSolid = SolidUtils.CreateTransformed(linkedSolid, linkTransform);

                // 6. Test Host Model Elements intersecting the transformed solid
                ElementIntersectsSolidFilter linkSolidFilter = new ElementIntersectsSolidFilter(transformedSolid);

                IList<Element> hostClashes = new FilteredElementCollector(hostDoc)
                    .WherePasses(linkSolidFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 7. Format results
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Linked Document: {linkDoc.Title}");
                sb.AppendLine($"Linked Element: [{linkedTargetElement.Id.Value}] {linkedTargetElement.Name} ({linkedTargetElement.Category?.Name})");
                sb.AppendLine($"Host Clashes Found: {hostClashes.Count}\n");

                if (hostClashes.Count > 0)
                {
                    var grouped = hostClashes.GroupBy(e => e.Category?.Name ?? "Other");
                    foreach (var group in grouped)
                    {
                        sb.AppendLine($" • Host {group.Key}: {group.Count()} element(s)");
                        foreach (var elem in group.Take(3))
                        {
                            sb.AppendLine($"     - [{elem.Id.Value}] {elem.Name}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("✅ No clashing elements in the host document intersect this linked element.");
                }

                TaskDialog.Show("Cross-Model Clash Detection", sb.ToString());

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

        /// <summary>
        /// Recursively extracts the first non-empty 3D Solid from an Element's geometry tree.
        /// </summary>
        private Solid? ExtractSolid(Element element)
        {
            Options options = new Options
            {
                ComputeReferences = false,
                DetailLevel = ViewDetailLevel.Fine
            };

            GeometryElement geomElem = element.get_Geometry(options);
            if (geomElem == null) return null;

            return FindSolidInGeometry(geomElem);
        }

        private Solid? FindSolidInGeometry(IEnumerable<GeometryObject> geomObjects)
        {
            foreach (GeometryObject obj in geomObjects)
            {
                if (obj is Solid solid && solid.Volume > 0.0001)
                {
                    return solid;
                }
                if (obj is GeometryInstance inst)
                {
                    GeometryElement symbolGeom = inst.GetSymbolGeometry();
                    Solid? s = FindSolidInGeometry(symbolGeom);
                    if (s != null) return s;
                }
            }
            return null;
        }
    }
}

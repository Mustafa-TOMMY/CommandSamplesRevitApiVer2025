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
    // Command 07 — Custom Solid & Clearance Intersection (ElementIntersectsSolidFilter)
    //
    // Demonstrates 3D intersection testing against an explicit, in-memory Solid.
    //
    // When to use ElementIntersectsSolidFilter instead of ElementIntersectsElementFilter:
    //   1. Clearance Zones: When checking a safety envelope (e.g., +2 inches around duct).
    //   2. Virtual Volumes: Room solids, egress pathways, or construction zones.
    //   3. Transformed Solids: Solids extracted from Linked Models.
    //
    // This command generates an in-memory clearance box solid around a picked element,
    // verifies solid validity and volume, and queries all elements intersecting this clearance zone.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class ElementIntersectsSolidCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // 1. Pick an Element (e.g., Duct, Pipe, Equipment)
                Reference selRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select an element (e.g., Duct, Pipe, Equipment) to test Clearance Zone intersections");

                Element hostElement = doc.GetElement(selRef);
                BoundingBoxXYZ? bbox = hostElement.get_BoundingBox(null);

                if (bbox == null)
                {
                    TaskDialog.Show("Solid Intersection", "Selected element has no bounding box.");
                    return Result.Failed;
                }

                // 2. Define a Clearance Offset (e.g. 50mm = ~0.164 ft)
                double clearanceOffsetFeet = 50.0 / 304.8; // Convert 50mm to internal feet

                XYZ min = new XYZ(bbox.Min.X - clearanceOffsetFeet, bbox.Min.Y - clearanceOffsetFeet, bbox.Min.Z - clearanceOffsetFeet);
                XYZ max = new XYZ(bbox.Max.X + clearanceOffsetFeet, bbox.Max.Y + clearanceOffsetFeet, bbox.Max.Z + clearanceOffsetFeet);
                double height = max.Z - min.Z;

                if (height <= 0.001 || (max.X - min.X) <= 0.001 || (max.Y - min.Y) <= 0.001)
                {
                    TaskDialog.Show("Solid Intersection", "Invalid dimensions for creating clearance solid.");
                    return Result.Failed;
                }

                // 3. Construct an in-memory 3D Solid representing the Clearance Envelope
                CurveLoop profile = new CurveLoop();
                XYZ p0 = new XYZ(min.X, min.Y, min.Z);
                XYZ p1 = new XYZ(max.X, min.Y, min.Z);
                XYZ p2 = new XYZ(max.X, max.Y, min.Z);
                XYZ p3 = new XYZ(min.X, max.Y, min.Z);

                profile.Append(Line.CreateBound(p0, p1));
                profile.Append(Line.CreateBound(p1, p2));
                profile.Append(Line.CreateBound(p2, p3));
                profile.Append(Line.CreateBound(p3, p0));

                Solid clearanceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(
                    new List<CurveLoop> { profile },
                    XYZ.BasisZ,
                    height);

                // Validate generated solid before passing to filter
                if (clearanceSolid == null || clearanceSolid.Volume <= 0.0001)
                {
                    TaskDialog.Show("Solid Intersection", "Failed to construct a valid 3D clearance solid.");
                    return Result.Failed;
                }

                // 4. Create the ElementIntersectsSolidFilter with the clearance solid
                ElementIntersectsSolidFilter solidFilter = new ElementIntersectsSolidFilter(clearanceSolid);

                // 5. Exclude the picked host element itself
                ExclusionFilter selfExclusion = new ExclusionFilter(new List<ElementId> { hostElement.Id });

                // 6. Query model elements intersecting this 3D clearance solid
                IList<Element> clearanceViolations = new FilteredElementCollector(doc)
                    .WherePasses(selfExclusion)
                    .WherePasses(solidFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                // 7. Report Findings
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Host Element: [{hostElement.Id.Value}] {hostElement.Name}");
                sb.AppendLine($"Clearance Envelope Buffer: 50 mm ({clearanceOffsetFeet:F3} ft)");
                sb.AppendLine($"Clearance Zone Volume: {clearanceSolid.Volume:F2} cu.ft\n");
                sb.AppendLine($"Elements Intersecting Clearance Zone: {clearanceViolations.Count}\n");

                if (clearanceViolations.Count > 0)
                {
                    var grouped = clearanceViolations.GroupBy(e => e.Category?.Name ?? "Other");
                    foreach (var group in grouped)
                    {
                        sb.AppendLine($" • {group.Key}: {group.Count()} element(s)");
                        foreach (var elem in group.Take(3))
                        {
                            sb.AppendLine($"     - [{elem.Id.Value}] {elem.Name}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("✅ Clearance zone is clear! No surrounding elements penetrate the buffer.");
                }

                TaskDialog.Show("Clearance Zone (ElementIntersectsSolidFilter)", sb.ToString());

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

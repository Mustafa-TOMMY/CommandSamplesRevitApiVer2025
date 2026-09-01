using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.Fundamentals
{
    // ============================================================================
    // Derived Geometry Command
    //
    // Command 04
    //
    // Purpose:
    //
    // Demonstrate how geometric information can be DERIVED from data
    // provided by Revit.
    //
    // LocationPoint:
    //     Point       → Provided by Revit
    //     Rotation    → Provided by Revit
    //     Direction   → Derived from the instance coordinate system
    //     Length      → Not applicable to the LocationPoint itself
    //
    // LocationCurve:
    //     Start Point → Provided by Revit
    //     End Point   → Provided by Revit
    //     Length      → Provided by Revit
    //     Direction   → Derived from End - Start
    //     Angle       → Derived from Direction
    //
    // Important:
    //
    // "Derived" means the value is not being read as one direct property.
    // We calculate it from other geometric information.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class DerivedGeometryCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select Element
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select an element to analyze its derived geometry");

                Element element = doc.GetElement(reference);

                if (element == null)
                {
                    TaskDialog.Show(
                        "Derived Geometry",
                        "Could not find the selected element.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Location
                //=====================================================

                Location location = element.Location;
                if (location == null)
                {
                    TaskDialog.Show("Derived Geometry",
                        "The selected element does not have a Location.");

                    return Result.Failed;
                }

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("DERIVED GEOMETRY ANALYSIS");

                sb.AppendLine("========================================");

                sb.AppendLine($"Element Id : {element.Id}");

                sb.AppendLine($"Category   : " + $"{element.Category?.Name ?? "None"}");

                sb.AppendLine($"Location   : {location.GetType().Name}");

                sb.AppendLine();

                //=====================================================
                // 3. LocationPoint
                //=====================================================

                LocationPoint locationPoint = location as LocationPoint;

                if (locationPoint != null)
                {
                    XYZ point = locationPoint.Point;

                    double rotation = locationPoint.Rotation;

                    sb.AppendLine("LOCATION POINT");

                    sb.AppendLine("----------------------------------------");

                    //=================================================
                    // Point
                    //=================================================

                    sb.AppendLine("Point:");

                    sb.AppendLine($"  ({point.X:F4}, " +
                        $"{point.Y:F4}, " +
                        $"{point.Z:F4})");

                    sb.AppendLine("  Source: Revit LocationPoint.Point");

                    sb.AppendLine();

                    //=================================================
                    // Rotation
                    //=================================================

                    sb.AppendLine("Rotation:");

                    sb.AppendLine($"  Radians : {rotation:F6}");

                    sb.AppendLine($"  Degrees : " + $"{rotation * 180.0 / Math.PI:F2}");

                    sb.AppendLine("  Source: Revit LocationPoint.Rotation");

                    sb.AppendLine();

                    //=================================================
                    // Transform
                    //=================================================

                    FamilyInstance familyInstance = element as FamilyInstance;

                    if (familyInstance != null)
                    {
                        Transform transform = familyInstance.GetTransform();

                        if (transform != null)
                        {
                            sb.AppendLine("INSTANCE TRANSFORM");
                            sb.AppendLine("----------------------------------------");

                            XYZ basisX = transform.BasisX;
                            XYZ basisY = transform.BasisY;
                            XYZ basisZ = transform.BasisZ;

                            sb.AppendLine($"BasisX : " +
                                $"({basisX.X:F4}, " +
                                $"{basisX.Y:F4}, " +
                                $"{basisX.Z:F4})");

                            sb.AppendLine($"BasisY : " +
                                $"({basisY.X:F4}, " +
                                $"{basisY.Y:F4}, " +
                                $"{basisY.Z:F4})");

                            sb.AppendLine($"BasisZ : " +
                                $"({basisZ.X:F4}, " +
                                $"{basisZ.Y:F4}, " +
                                $"{basisZ.Z:F4})");

                            sb.AppendLine();

                            //=========================================
                            // Derived Direction
                            // BasisX represents the local X direction
                            // of the instance coordinate system.
                            //=========================================

                            XYZ direction = basisX.Normalize();

                            sb.AppendLine("DERIVED DIRECTION:");

                            sb.AppendLine(
                                $"  ({direction.X:F4}, " +
                                $"{direction.Y:F4}, " +
                                $"{direction.Z:F4})");

                            sb.AppendLine(" Source: Derived from Transform.BasisX");
                            sb.AppendLine();
                            sb.AppendLine("Actual Length:");
                            sb.AppendLine("N/A for LocationPoint.");
                            sb.AppendLine("A LocationPoint represents a position, not a geometric path.");
                        }
                    }
                }

                //=====================================================
                // 4. LocationCurve
                //=====================================================

                LocationCurve locationCurve = location as LocationCurve;

                if (locationCurve != null)
                {
                    Curve curve = locationCurve.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);

                    //=================================================
                    // Vector from Start → End
                    //=================================================
                    XYZ vector = endPoint - startPoint;

                    //=================================================
                    // 3D Direction
                    //=================================================
                    XYZ direction = vector.Normalize();

                    //=================================================
                    // Actual Curve Length
                    //=================================================
                    double actualLength = curve.Length;

                    //=================================================
                    // Horizontal Angle
                    // Projection of direction onto XY plane.
                    //=================================================

                    double horizontalAngle = Math.Atan2(direction.Y,direction.X);
                    double horizontalAngleDegrees = horizontalAngle * 180.0 / Math.PI;

                    sb.AppendLine("LOCATION CURVE");

                    sb.AppendLine("----------------------------------------");

                    //=================================================
                    // Start Point
                    //=================================================

                    sb.AppendLine("Start Point:");

                    sb.AppendLine(
                        $"  ({startPoint.X:F4}, " +
                        $"{startPoint.Y:F4}, " +
                        $"{startPoint.Z:F4})");

                    sb.AppendLine("  Source: Revit Curve.GetEndPoint(0)");

                    sb.AppendLine();

                    //=================================================
                    // End Point
                    //=================================================

                    sb.AppendLine("End Point:");

                    sb.AppendLine(
                        $"  ({endPoint.X:F4}, " +
                        $"{endPoint.Y:F4}, " +
                        $"{endPoint.Z:F4})");

                    sb.AppendLine("  Source: Revit Curve.GetEndPoint(1)");

                    sb.AppendLine();

                    //=================================================
                    // Vector
                    //=================================================

                    sb.AppendLine("Start → End Vector:");

                    sb.AppendLine(
                        $"  ({vector.X:F4}, " +
                        $"{vector.Y:F4}, " +
                        $"{vector.Z:F4})");

                    sb.AppendLine("  Calculation: End - Start");

                    sb.AppendLine();

                    //=================================================
                    // Direction
                    //=================================================

                    sb.AppendLine("3D Direction:");

                    sb.AppendLine(
                        $"  ({direction.X:F4}, " +
                        $"{direction.Y:F4}, " +
                        $"{direction.Z:F4})");

                    sb.AppendLine("  Calculation: " +
                        "(End - Start).Normalize()");

                    sb.AppendLine();

                    //=================================================
                    // Actual Length
                    //=====================================================

                    sb.AppendLine("Actual Length:");
                    sb.AppendLine($" {actualLength:F4} ft");
                    sb.AppendLine(" Source: Revit Curve.Length");
                    sb.AppendLine();

                    //=================================================
                    // Horizontal Angle
                    //=================================================

                    sb.AppendLine("Horizontal Direction Angle:");
                    sb.AppendLine($" {horizontalAngleDegrees:F2} degrees");
                    sb.AppendLine(" Calculation: atan2(Direction.Y, Direction.X)");
                    sb.AppendLine();

                    //=================================================
                    // Reconstruction
                    //=================================================

                    XYZ reconstructedEnd = startPoint + direction * actualLength;
                    double reconstructionError = reconstructedEnd.DistanceTo(endPoint);
                    sb.AppendLine("END POINT RECONSTRUCTION");
                    sb.AppendLine("----------------------------------------");

                    sb.AppendLine($"Calculated End: " +
                        $"({reconstructedEnd.X:F4}, " +
                        $"{reconstructedEnd.Y:F4}, " +
                        $"{reconstructedEnd.Z:F4})");

                    sb.AppendLine($"Error: {reconstructionError:F8} ft");
                    sb.AppendLine();
                    sb.AppendLine("FORMULA:");
                    sb.AppendLine("End = Start + Direction × Length");
                }

                //=====================================================
                // 5. Unknown Location
                //=====================================================

                if (locationPoint == null && locationCurve == null)
                {
                    sb.AppendLine("OTHER LOCATION TYPE");

                    sb.AppendLine("----------------------------------------");
                    sb.AppendLine($"Runtime Type: {location.GetType().FullName}");
                    sb.AppendLine();
                    sb.AppendLine("Derived geometry for this Location " +
                        "type is not implemented.");
                }

                //=====================================================
                // 6. Display
                //=====================================================

                TaskDialog.Show("Derived Geometry", sb.ToString());

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
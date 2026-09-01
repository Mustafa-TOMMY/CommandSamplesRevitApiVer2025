using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // Face-Based Family Geometry Command
    //
    // Part B - Command 04
    //
    // Purpose:
    //
    // Analyze a FamilyInstance that is hosted on a Face / Work Plane.
    //
    // Main geometric sources:
    //
    // FamilyInstance
    //       ↓
    // Host / HostFace
    //       ↓
    // Face
    //       ↓
    // Face Normal
    //
    // AND
    //
    // FamilyInstance
    //       ↓
    // GetTransform()
    //       ↓
    // Origin / BasisX / BasisY / BasisZ
    //
    // Important:
    //
    // We do NOT assume that BasisZ is always identical to the host Face normal.
    // We calculate and report the relationship between them.
    //
    // This command is READ-ONLY.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class FaceBasedFamilyGeometryCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //=====================================================
                // 1. Select FamilyInstance
                //=====================================================

                Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        "Select a Face-Based / WorkPlane-Based FamilyInstance");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Face-Based Family",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Family Information
                //=====================================================

                FamilySymbol symbol = familyInstance.Symbol;

                Family family = symbol?.Family;

                if (symbol == null || family == null)
                {
                    TaskDialog.Show(
                        "Face-Based Family",
                        "Could not obtain Family or FamilySymbol.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Verify Placement Type
                //=====================================================

                FamilyPlacementType placementType = family.FamilyPlacementType;

                //=====================================================
                // 4. Get Host
                //=====================================================

                Element host = familyInstance.Host;

                //=====================================================
                // 5. Get HostFace Reference
                //=====================================================

                Reference hostFaceReference = null;

                try
                {
                    hostFaceReference = familyInstance.HostFace;
                }
                catch
                {
                    hostFaceReference = null;
                }

                //=====================================================
                // 6. Get Face From Reference
                //=====================================================

                Face hostFace = null;

                if (hostFaceReference != null)
                {
                    Element hostElement = doc.GetElement(hostFaceReference.ElementId);

                    if (hostElement != null)
                    {
                        GeometryObject geometryObject = hostElement.GetGeometryObjectFromReference(hostFaceReference);
                        hostFace = geometryObject as Face;
                    }
                }

                //=====================================================
                // 7. Get Family Transform
                //=====================================================

                Transform transform = null;

                try
                {
                    transform = familyInstance.GetTransform();
                }
                catch
                {
                    transform = null;
                }

                if (transform == null)
                {
                    TaskDialog.Show(
                        "Face-Based Family",
                        "Could not obtain FamilyInstance Transform.");

                    return Result.Failed;
                }

                //=====================================================
                // 8. Get Transform Axes
                //=====================================================

                XYZ origin = transform.Origin;
                XYZ basisX = transform.BasisX.Normalize();
                XYZ basisY = transform.BasisY.Normalize();
                XYZ basisZ = transform.BasisZ.Normalize();

                //=====================================================
                // 9. Determine Face Normal
                //
                // A Face normal is evaluated at a UV location.
                //
                // We use the face's bounding box midpoint as a
                // practical inspection point.
                //=====================================================

                XYZ faceNormal = null;
                XYZ faceEvaluationPoint = null;

                if (hostFace != null)
                {
                    BoundingBoxUV boundingBox = hostFace.GetBoundingBox();

                    UV midpoint = new
                        UV(
                            (boundingBox.Min.U + boundingBox.Max.U) / 2.0,
                            (boundingBox.Min.V + boundingBox.Max.V) / 2.0
                        );

                    faceNormal = hostFace.ComputeNormal(midpoint).Normalize();
                    faceEvaluationPoint = hostFace.Evaluate(midpoint);
                }

                //=====================================================
                // 10. Compare BasisZ With Face Normal
                //=====================================================

                double basisZFaceDot = double.NaN;
                double basisZFaceAngle = double.NaN;

                if (faceNormal != null)
                {
                    double dot = basisZ.DotProduct(faceNormal);

                    // Protect against floating-point drift.
                    dot = Math.Max(-1.0, Math.Min(1.0, dot));

                    basisZFaceDot = dot;

                    basisZFaceAngle = Math.Acos(dot) * 180.0 / Math.PI;
                }

                //=====================================================
                // 11. Determine Basic Relationship
                //=====================================================

                string normalRelationship =
                    "Could not determine";

                if (faceNormal != null)
                {
                    if (basisZFaceDot > 0.999999)
                    {
                        normalRelationship = "BasisZ is aligned with Face Normal";
                    }
                    else if (basisZFaceDot < -0.999999)
                    {
                        normalRelationship = "BasisZ is opposite to Face Normal";
                    }
                    else
                    {
                        normalRelationship = "BasisZ is not parallel to Face Normal";
                    }
                }

                //=====================================================
                // 12. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb =
                            new StringBuilder();

                sb.AppendLine(
                    "FACE-BASED FAMILY GEOMETRY");

                sb.AppendLine(
                    "========================================");

                //=====================================================
                // Family Information
                //=====================================================

                sb.AppendLine(
                    "1. FAMILY INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Element Id          : {familyInstance.Id}");

                sb.AppendLine(
                    $"Family Name         : {family.Name}");

                sb.AppendLine(
                    $"Symbol / Type       : {symbol.Name}");

                sb.AppendLine(
                    $"Placement Type      : {placementType}");

                sb.AppendLine();

                //=====================================================
                // Host Information
                //=====================================================

                sb.AppendLine(
                    "2. HOST INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                if (host != null)
                {
                    sb.AppendLine(
                        $"Host Type           : {host.GetType().Name}");

                    sb.AppendLine(
                        $"Host Id             : {host.Id}");

                    sb.AppendLine(
                        $"Host Name           : {host.Name}");
                }
                else
                {
                    sb.AppendLine(
                        "Host                 : None");
                }

                sb.AppendLine();

                sb.AppendLine(
                    $"HostFace Reference  : " +
                    $"{(hostFaceReference != null ? "Available" : "Not Available")}");

                sb.AppendLine();

                //=====================================================
                // Face Information
                //=====================================================

                sb.AppendLine(
                    "3. HOST FACE");

                sb.AppendLine(
                    "----------------------------------------");

                if (hostFace != null)
                {
                    sb.AppendLine(
                        "Face                : Available");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Evaluation Point:");

                    sb.AppendLine(
                        $"  ({faceEvaluationPoint.X:F6}, " +
                        $"{faceEvaluationPoint.Y:F6}, " +
                        $"{faceEvaluationPoint.Z:F6})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "Face Normal:");

                    sb.AppendLine(
                        $"  ({faceNormal.X:F6}, " +
                        $"{faceNormal.Y:F6}, " +
                        $"{faceNormal.Z:F6})");
                }
                else
                {
                    sb.AppendLine(
                        "Face                : Not available.");

                    sb.AppendLine();

                    sb.AppendLine(
                        "The instance may be WorkPlaneBased without " +
                        "a directly retrievable HostFace reference.");
                }

                sb.AppendLine();

                //=====================================================
                // Transform
                //=====================================================

                sb.AppendLine(
                    "4. FAMILY INSTANCE TRANSFORM");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Origin:");

                sb.AppendLine(
                    $"  ({origin.X:F6}, " +
                    $"{origin.Y:F6}, " +
                    $"{origin.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisX:");

                sb.AppendLine(
                    $"  ({basisX.X:F6}, " +
                    $"{basisX.Y:F6}, " +
                    $"{basisX.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisY:");

                sb.AppendLine(
                    $"  ({basisY.X:F6}, " +
                    $"{basisY.Y:F6}, " +
                    $"{basisY.Z:F6})");

                sb.AppendLine();

                sb.AppendLine(
                    "BasisZ:");

                sb.AppendLine(
                    $"  ({basisZ.X:F6}, " +
                    $"{basisZ.Y:F6}, " +
                    $"{basisZ.Z:F6})");

                sb.AppendLine();

                //=====================================================
                // Normal Comparison
                //=====================================================

                sb.AppendLine(
                    "5. FACE NORMAL vs BASIS Z");

                sb.AppendLine(
                    "----------------------------------------");

                if (faceNormal != null)
                {
                    sb.AppendLine(
                        $"Dot Product         : " +
                        $"{basisZFaceDot:F10}");

                    sb.AppendLine();

                    sb.AppendLine(
                        $"Angle Between Axes  : " +
                        $"{basisZFaceAngle:F6}°");

                    sb.AppendLine();

                    sb.AppendLine(
                        $"Relationship         : " +
                        $"{normalRelationship}");
                }
                else
                {
                    sb.AppendLine(
                        "Face normal comparison unavailable.");
                }

                sb.AppendLine();

                //=====================================================
                // Geometry Interpretation
                //=====================================================

                sb.AppendLine(
                    "6. GEOMETRY INTERPRETATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Native information:");

                sb.AppendLine(
                    "• Host / HostFace");

                sb.AppendLine(
                    "• Face normal");

                sb.AppendLine(
                    "• FamilyInstance Transform");

                sb.AppendLine();

                sb.AppendLine(
                    "Transform information:");

                sb.AppendLine(
                    "• Origin");

                sb.AppendLine(
                    "• BasisX");

                sb.AppendLine(
                    "• BasisY");

                sb.AppendLine(
                    "• BasisZ");

                sb.AppendLine();

                sb.AppendLine(
                    "Important:");

                sb.AppendLine(
                    "Do not automatically assume that BasisZ equals " +
                    "the host Face normal.");

                sb.AppendLine();

                sb.AppendLine(
                    "The relationship is explicitly measured above.");

                //=====================================================
                // Four/Five Main Values
                //=====================================================

                sb.AppendLine();

                sb.AppendLine(
                    "7. MAIN GEOMETRIC VALUES");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Start Point:");

                sb.AppendLine(
                    "Not universally defined by the Face itself.");

                sb.AppendLine();

                sb.AppendLine(
                    "End Point:");

                sb.AppendLine(
                    "Not universally defined by the Face itself.");

                sb.AppendLine();

                sb.AppendLine(
                    "3D Direction:");

                sb.AppendLine(
                    "Requires semantic definition of the desired " +
                    "family axis; Transform Basis vectors are available.");

                sb.AppendLine();

                sb.AppendLine(
                    "Rotation:");

                sb.AppendLine(
                    "Can be analyzed from the Transform axes relative " +
                    "to the host face.");

                sb.AppendLine();

                sb.AppendLine(
                    "Actual Length:");

                sb.AppendLine(
                    "Not inherently defined by a Face-Based placement.");

                sb.AppendLine(
                    "Requires family geometry or an appropriate parameter.");

                sb.AppendLine();

                //=====================================================
                // Final Strategy
                //=====================================================

                sb.AppendLine(
                    "8. PART B STRATEGY");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Face-Based Family:");

                sb.AppendLine(
                    "Host Face");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Face Normal");

                sb.AppendLine(
                    "     +");

                sb.AppendLine(
                    "FamilyInstance Transform");

                sb.AppendLine(
                    "     ↓");

                sb.AppendLine(
                    "Analyze Local Coordinate System");

                sb.AppendLine();

                sb.AppendLine(
                    "Do not invent Start / End / Length when the " +
                    "placement architecture does not define them.");

                #endregion

                //=====================================================
                // 13. Display
                //=====================================================

                TaskDialog.Show("Face-Based Family Geometry", sb.ToString());

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
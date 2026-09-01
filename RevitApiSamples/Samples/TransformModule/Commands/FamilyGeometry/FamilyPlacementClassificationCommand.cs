using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Text;

namespace RevitApiSamples.Samples.TransformModule.Commands.FamilyGeometry
{
    // ============================================================================
    // Family Placement Classification Command
    //
    // Part B - Command 01
    //
    // Purpose:
    //
    // Classify a selected FamilyInstance before performing geometric calculations.
    //
    // The command intentionally separates:
    //
    // 1. FamilyPlacementType
    // 2. Actual Location runtime type
    // 3. Host information
    // 4. Transform information
    //
    // This prevents us from assuming that every FamilyPlacementType has one
    // universal Location representation.
    //
    // The command does NOT calculate the final:
    //
    // - Start Point
    // - End Point
    // - Direction
    // - Rotation
    // - Actual Length
    //
    // Those calculations belong to the specialized FamilyGeometry commands.
    // ============================================================================

    [Transaction(TransactionMode.ReadOnly)]
    public class FamilyPlacementClassificationCommand : IExternalCommand
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
                        "Select a FamilyInstance to classify its placement and geometry source");

                Element element = doc.GetElement(reference);

                FamilyInstance familyInstance = element as FamilyInstance;

                if (familyInstance == null)
                {
                    TaskDialog.Show(
                        "Family Placement Classification",
                        "The selected element is not a FamilyInstance.");

                    return Result.Failed;
                }

                //=====================================================
                // 2. Get Family / Symbol
                //=====================================================

                FamilySymbol symbol = familyInstance.Symbol;

                Family family = symbol?.Family;

                if (symbol == null || family == null)
                {
                    TaskDialog.Show(
                        "Family Placement Classification",
                        "Could not obtain the FamilySymbol or Family.");

                    return Result.Failed;
                }

                //=====================================================
                // 3. Get FamilyPlacementType
                //=====================================================

                FamilyPlacementType placementType = family.FamilyPlacementType;

                //=====================================================
                // 4. Inspect Actual Location
                //=====================================================

                Location location = familyInstance.Location;
                string locationType = location == null ? "null" : location.GetType().Name;
                bool hasLocationPoint = location is LocationPoint;
                bool hasLocationCurve = location is LocationCurve;

                //=====================================================
                // 5. Inspect Host
                //=====================================================

                Element host = familyInstance.Host;

                string hostInfo;

                if (host == null)
                {
                    hostInfo = "No Host";
                }
                else
                {
                    hostInfo =
                        $"{host.GetType().Name} | " +
                        $"Id: {host.Id} | " +
                        $"Name: {host.Name}";
                }

                //=====================================================
                // 6. Inspect HostFace
                //=====================================================

                string hostFaceInfo;

                try
                {
                    Reference hostFace = familyInstance.HostFace;
                    hostFaceInfo = hostFace == null ? "No HostFace Reference" : "HostFace Reference Available";
                }
                catch
                {
                    hostFaceInfo = "HostFace could not be evaluated";
                }

                //=====================================================
                // 7. Inspect Transform
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

                bool hasTransform = transform != null;

                //=====================================================
                // 8. Determine Recommended Geometry Source
                //=====================================================

                string geometrySource;
                string geometryStrategy;

                if (hasLocationCurve)
                {
                    geometrySource = "LocationCurve";
                    geometryStrategy = "Use Curve geometry first: " + "Start Point, End Point, Direction and Length.";
                }
                else if (hasLocationPoint)
                {
                    geometrySource = "LocationPoint";

                    geometryStrategy =
                        "Use LocationPoint first: " +
                        "Insertion Point and Rotation. " +
                        "Additional parameters may be required to reconstruct " +
                        "missing 3D geometry.";
                }
                else if (hostFaceInfo == "HostFace Reference Available")
                {
                    geometrySource = "HostFace + Transform";

                    geometryStrategy =
                        "Inspect the host Face, its normal, " +
                        "and the FamilyInstance Transform.";
                }
                else if (placementType == FamilyPlacementType.TwoLevelsBased)
                {
                    geometrySource = "Two-Level Placement";

                    geometryStrategy = "Inspect Base/Top Levels and the actual runtime " +
                        "Location before deriving geometry.";
                }
                else if (hasTransform)
                {
                    geometrySource = "Transform";

                    geometryStrategy =
                        "Use Transform Origin and BasisX/Y/Z, " +
                        "then determine which axis represents the required geometry.";
                }
                else
                {
                    geometrySource = "No Single Native Source Identified";

                    geometryStrategy =
                        "Further inspection of Family geometry, parameters, " +
                        "references, or placement points is required.";
                }

                //=====================================================
                // 9. Build Report
                //=====================================================

                #region Report Structure
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(
                    "FAMILY PLACEMENT CLASSIFICATION");

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
                    $"Element Id      : {familyInstance.Id}");

                sb.AppendLine(
                    $"Family Name     : {family.Name}");

                sb.AppendLine(
                    $"Symbol / Type   : {symbol.Name}");

                sb.AppendLine();

                //=====================================================
                // Placement Information
                //=====================================================

                sb.AppendLine(
                    "2. PLACEMENT INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"FamilyPlacementType : {placementType}");

                sb.AppendLine(
                    $"Actual Location     : {locationType}");

                sb.AppendLine(
                    $"Has LocationPoint   : {hasLocationPoint}");

                sb.AppendLine(
                    $"Has LocationCurve   : {hasLocationCurve}");

                sb.AppendLine();

                //=====================================================
                // Hosting Information
                //=====================================================

                sb.AppendLine(
                    "3. HOST INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Host     : {hostInfo}");

                sb.AppendLine(
                    $"HostFace : {hostFaceInfo}");

                sb.AppendLine();

                //=====================================================
                // Transform Information
                //=====================================================

                sb.AppendLine(
                    "4. TRANSFORM INFORMATION");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Transform Available : {hasTransform}");

                if (transform != null)
                {
                    sb.AppendLine();

                    sb.AppendLine(
                        "Origin:");

                    sb.AppendLine(
                        $"  ({transform.Origin.X:F4}, " +
                        $"{transform.Origin.Y:F4}, " +
                        $"{transform.Origin.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisX:");

                    sb.AppendLine(
                        $"  ({transform.BasisX.X:F4}, " +
                        $"{transform.BasisX.Y:F4}, " +
                        $"{transform.BasisX.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisY:");

                    sb.AppendLine(
                        $"  ({transform.BasisY.X:F4}, " +
                        $"{transform.BasisY.Y:F4}, " +
                        $"{transform.BasisY.Z:F4})");

                    sb.AppendLine();

                    sb.AppendLine(
                        "BasisZ:");

                    sb.AppendLine(
                        $"  ({transform.BasisZ.X:F4}, " +
                        $"{transform.BasisZ.Y:F4}, " +
                        $"{transform.BasisZ.Z:F4})");
                }

                sb.AppendLine();

                //=====================================================
                // Classification Result
                //=====================================================

                sb.AppendLine(
                    "5. CLASSIFICATION RESULT");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    $"Primary Geometry Source:");

                sb.AppendLine(
                    $"  {geometrySource}");

                sb.AppendLine();

                sb.AppendLine(
                    "Recommended Strategy:");

                sb.AppendLine(
                    $"  {geometryStrategy}");

                sb.AppendLine();

                //=====================================================
                // Placement-Specific Guidance
                //=====================================================

                sb.AppendLine(
                    "6. PLACEMENT-SPECIFIC GUIDANCE");

                sb.AppendLine(
                    "----------------------------------------");

                switch (placementType)
                {
                    case FamilyPlacementType.OneLevelBased:

                        sb.AppendLine(
                            "OneLevelBased");

                        sb.AppendLine(
                            "Inspect LocationPoint first.");

                        sb.AppendLine(
                            "If the required 3D geometry is not directly " +
                            "available, inspect Family Parameters.");

                        break;

                    case FamilyPlacementType.TwoLevelsBased:

                        sb.AppendLine(
                            "TwoLevelsBased");

                        sb.AppendLine(
                            "Do not assume LocationPoint or LocationCurve " +
                            "without inspecting the actual Location object.");

                        sb.AppendLine(
                            "Inspect Base Level, Top Level, and actual geometry.");

                        break;

                    case FamilyPlacementType.WorkPlaneBased:

                        sb.AppendLine(
                            "WorkPlaneBased");

                        sb.AppendLine(
                            "Inspect Host / HostFace and FamilyInstance Transform.");

                        sb.AppendLine(
                            "The host face and instance orientation must be " +
                            "considered together.");

                        break;

                    case FamilyPlacementType.CurveBased:

                        sb.AppendLine(
                            "CurveBased");

                        sb.AppendLine(
                            "Inspect LocationCurve.");

                        sb.AppendLine(
                            "Curve endpoints and Curve.Length are primary " +
                            "geometric sources.");

                        break;

                    case FamilyPlacementType.Adaptive:

                        sb.AppendLine(
                            "Adaptive");

                        sb.AppendLine(
                            "Inspect adaptive placement points.");

                        sb.AppendLine(
                            "Do not rely on LocationPoint or LocationCurve.");

                        break;

                    case FamilyPlacementType.ViewBased:

                        sb.AppendLine(
                            "ViewBased");

                        sb.AppendLine(
                            "Inspect the view coordinate system and " +
                            "instance placement.");

                        break;

                    default:

                        sb.AppendLine(
                            "Placement type requires further inspection.");

                        break;
                }

                sb.AppendLine();

                //=====================================================
                // Important Rule
                //=====================================================

                sb.AppendLine(
                    "7. PART B CORE RULE");

                sb.AppendLine(
                    "----------------------------------------");

                sb.AppendLine(
                    "Do not assume geometry from FamilyPlacementType alone.");

                sb.AppendLine();

                sb.AppendLine(
                    "First inspect:");

                sb.AppendLine(
                    "1. FamilyPlacementType");

                sb.AppendLine(
                    "2. Actual Location runtime type");

                sb.AppendLine(
                    "3. Host / HostFace");

                sb.AppendLine(
                    "4. Transform");

                sb.AppendLine(
                    "5. Native geometry");

                sb.AppendLine(
                    "6. Parameters / References");

                sb.AppendLine();

                sb.AppendLine(
                    "Then derive:");

                sb.AppendLine(
                    "Start Point");

                sb.AppendLine(
                    "End Point");

                sb.AppendLine(
                    "Direction");

                sb.AppendLine(
                    "Rotation");

                sb.AppendLine(
                    "Actual Length");

                #endregion

                //=====================================================
                // 10. Display
                //=====================================================

                TaskDialog.Show("Family Placement Classification", sb.ToString());

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
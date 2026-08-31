# Module 10 — Transform

## 1. Transform Mental Model

A Transform in Revit is a mathematical object that encodes three properties in one: Translation (where is the element's origin in world space), Rotation (how is the element's local coordinate system oriented), and Scale. It is represented as a 4×4 matrix. The Revit API exposes this as the `Autodesk.Revit.DB.Transform` class in `Autodesk.Revit.DB`.

```mermaid
graph TD
    T["Transform (4x4 Matrix)"]
    T --> O["Origin<br/>(Translation)"]
    T --> R["BasisX, BasisY, BasisZ<br/>(Rotation)"]
    T --> S["Scale"]
```

---

## 2. Location vs. Transform

| Feature | Location (Point/Curve) | Transform |
| :--- | :--- | :--- |
| **Availability** | All elements | `FamilyInstance` (via `GetTransform()`), `RevitLinkInstance`, `GeometryInstance` |
| **Data Provided** | XYZ point or Curve | Full 4x4 matrix (Origin, BasisX, BasisY, BasisZ) |
| **Use Case** | Simple positioning | Coordinate system conversions, 3D tilt/slope, exact rotation |

```mermaid
flowchart LR
    E["Element"]
    E --> L["Location Property"]
    L --> LP["LocationPoint"]
    L --> LC["LocationCurve"]
    
    FI["FamilyInstance"]
    FI --> T["GetTransform() / GetTotalTransform()"]
    T --> TM["Transform Matrix"]
```

---

## 3. ElementTransformUtils — The Movement API

`ElementTransformUtils` is the primary API for moving, copying, rotating, and mirroring elements in a Revit project. All operations that modify the model require an active `Transaction`.

| Method | Description |
| :--- | :--- |
| `MoveElement` | Moves an element by an XYZ translation vector. |
| `CopyElement` | Copies an element to a new location. |
| `RotateElement` | Rotates an element around a given axis line. |
| `MirrorElement` | Mirrors an element in-place across a plane. |
| `MirrorElements` | Copies and mirrors elements across a plane. |

---

## 4. Learning Progression (Commands 01–10)

| # | Command File | Class Name | Main API | What It Teaches |
| :--- | :--- | :--- | :--- | :--- |
| 01 | [InspectLocationCommand.cs](Commands/InspectLocationCommand.cs) | `InspectLocationCommand` | `LocationPoint`, `LocationCurve` | Reading simple location data from any element. |
| 02 | [MoveElementCommand.cs](Commands/MoveElementCommand.cs) | `MoveElementCommand` | `ElementTransformUtils.MoveElement()` | Moving elements via an XYZ translation vector. |
| 03 | [CopyElementCommand.cs](Commands/CopyElementCommand.cs) | `CopyElementCommand` | `ElementTransformUtils.CopyElement()` | Copying elements and receiving new element IDs. |
| 04 | [RotateElementCommand.cs](Commands/RotateElementCommand.cs) | `RotateElementCommand` | `ElementTransformUtils.RotateElement()` | Rotating elements around an axis in radians. |
| 05 | [MirrorElementCommand.cs](Commands/MirrorElementCommand.cs) | `MirrorElementCommand` | `ElementTransformUtils.MirrorElement()` | Mirroring elements in-place over a plane. |
| 06 | [TransformGeometryCommand.cs](Commands/TransformGeometryCommand.cs) | `TransformGeometryCommand` | `GetTransform()`, `OfPoint()` | Understanding local coordinates and transform matrices. |
| 07 | [GetPointOnCurveCommand.cs](Commands/GetPointOnCurveCommand.cs) | `GetPointOnCurveCommand` | `curve.Evaluate(t, normalized)`, `GetEndPoint()` | Getting any point along a CurveBased element's curve. |
| 08 | [PointFamilyStartEndCommand.cs](Commands/PointFamilyStartEndCommand.cs) | `PointFamilyStartEndCommand` | `LocationPoint` + `GetTransform().BasisZ` + Length | Deriving Start, End, and 3D direction from a PointBased family. |
| 09 | [DivideCurveByDistanceCommand.cs](Commands/DivideCurveByDistanceCommand.cs) | `DivideCurveByDistanceCommand` | `curve.Evaluate(d/L, true)`, `ComputeDerivatives()` | Sampling points along a curve at custom fixed distance intervals (e.g., every 3 ft on a 12 ft curve). |
| 10 | [GetLocationPointEndPointCommand.cs](Commands/GetLocationPointEndPointCommand.cs) | `GetLocationPointEndPointCommand` | `HandOrientation`, `Transform.OfPoint`, `ConnectorManager`, `Z-Elevation` | Deriving true 3D End Point, 3D Direction, Infeed/Outfeed elevations, and slope from LocationPoint families. |

---

## 5. Deep Dive: Calculating End Point & 3D Direction from `LocationPoint` Families

### The Core Problem
When an element uses a **`LocationPoint`** (a single insertion coordinate `XYZ`), Revit does **NOT** expose a built-in `.EndPoint` property. If the element represents directional machinery, equipment, conveyors, or cantilever components, how do we compute its **true 3D End Point (Outfeed)**, **3D Direction Vector**, and **Z-Elevation Slope**?

---

### The 5 Methods Ranked (Generic-First)

```mermaid
flowchart TD
    Target["Target LocationPoint FamilyInstance"] --> M1["1. Generic Vector Ray Projection<br/>EndPoint = Start + (HandOrientation * Length)"]
    Target --> M2["2. 3D Transform Matrix Transformation<br/>EndPoint = Transform.OfPoint(localEndPoint)"]
    Target --> M3["3. 3D Solid Geometry Vertex Projection<br/>Max Dot Product (V · HandOrientation)"]
    Target --> M4["4. Domain-Specific MEP Connectors<br/>Connector.Origin (Inflow / Outflow Ports)"]
    Target --> M5["5. 2D Polar Trigonometry Fallback<br/>XYZ(cos θ, sin θ, 0) — Flat Planar Only"]
```

#### 🥇 Rank 1: Vector Ray Projection (`Start + HandOrientation * Length`) — *Most Universal for ALL Families*
* **Applies to:** **ALL Loadable Families** (Architectural, Structural, MEP, Furniture, Generic Models).
* **Concept:** `familyInstance.HandOrientation` and `FacingOrientation` are true 3D unit vectors in world space.
* **Formulas:**
  * If insertion point is at **Start (Infeed)**:
    $$P_{\text{end}} = P_{\text{start}} + (\text{HandOrientation} \times L)$$
  * If insertion point is **Centered**:
    $$P_{\text{end}} = P_{\text{center}} + \left(\text{HandOrientation} \times \frac{L}{2}\right)$$
    $$P_{\text{start}} = P_{\text{center}} - \left(\text{HandOrientation} \times \frac{L}{2}\right)$$

#### 🥈 Rank 2: 3D Transform Matrix (`Transform.OfPoint`) — *Universal Matrix Transformation*
* **Applies to:** **ALL Loadable Families**.
* **Concept:** Converts a local coordinate defined in Family Editor space $(L, 0, 0)$ into project world space:
  $$P_{\text{world}} = \text{Transform}_{\text{total}} \times P_{\text{local}}$$
* **Code:**
  ```csharp
  XYZ localEndPoint = new XYZ(length, 0, 0);
  XYZ worldEndPoint = familyInstance.GetTotalTransform().OfPoint(localEndPoint);
  ```

#### 🥉 Rank 3: 3D Solid Geometry Vertex Projection — *Universal Geometric Inspection*
* **Applies to:** **ALL 3D Solid Geometry** (parameter-independent).
* **Concept:** Traverses `GeometryInstance.GetInstanceGeometry()` and finds the vertex maximizing $(V - P_{\text{start}}) \cdot \vec{u}$.

#### 4️⃣ Rank 4: MEP Connectors (`ConnectorManager`) — *Domain-Specific for MEP*
* **Applies to:** **MEP Families ONLY** (`MEPModel != null`).
* **Concept:** Reads physical connection ports (`connector.Origin` and flow direction `connector.CoordinateSystem.BasisZ`).

#### 5️⃣ Rank 5: 2D Polar Trigonometry (`LocationPoint.Rotation`) — *Fallback (2D Flat Plane Only)*
* **Concept:** $\vec{u} = (\cos\theta, \sin\theta, 0)$.
* **Limitation:** Hardcodes $Z=0$. Fails completely on 3D slopes, tilted conveyors, or slanted work planes.

---

### Infeed vs. Outfeed Z-Level & Slope Analysis Relative to $(0,0,0)$

Every point in Revit has an absolute elevation $Z$ measured in internal feet relative to the **Revit Internal Origin $(0,0,0)$**:

```
                     Outfeed P2 (X2, Y2, Z2)
                             ▲
                            /|
                           / |
              3D Vector   /  |  ΔZ (Height Difference) = Z2 - Z1
                         /   |
                        /    |
                       /     ▼
 Infeed P1 (X1, Y1, Z1) ───────
                       Horizontal Run
```

1. **Absolute Infeed Elevation:** $Z_1 = P_{\text{infeed}}.Z$
2. **Absolute Outfeed Elevation:** $Z_2 = P_{\text{outfeed}}.Z$
3. **Vertical Rise / Fall:** $\Delta Z = Z_2 - Z_1$
4. **Horizontal Planar Run:** $\text{Run} = \sqrt{(X_2 - X_1)^2 + (Y_2 - Y_1)^2}$
5. **Slope Percentage:** $\text{Slope} = \left(\frac{\Delta Z}{\text{Run}}\right) \times 100\%$

---

### System Families vs. Loadable Families in Location Calculations

| Aspect | System Families (Walls, Floors, Beams, Pipes, Ducts) | Loadable Families (Doors, Columns, Machinery, Equipment) |
| :--- | :--- | :--- |
| **Location Type** | Predominantly `LocationCurve` (Linear elements). | Predominantly `LocationPoint` (Punctual insertion point). |
| **Start / End Point Access** | Direct API: `Curve.GetEndPoint(0)` and `Curve.GetEndPoint(1)`. | Derived via `HandOrientation * Length` or `Transform.OfPoint()`. |
| **3D Direction Vector** | `(endPoint - startPoint).Normalize()` or `Line.Direction`. | `familyInstance.HandOrientation`, `FacingOrientation`, or `Transform.BasisZ`. |
| **3D Transform Matrix** | ❌ Not exposed directly (always in project world coordinates). | ✔ Exposes `GetTransform()` and `GetTotalTransform()`. |

---

## 6. Key Workflows & API Patterns

### 1. Move Element Pattern
```csharp
XYZ translationVector = new XYZ(5, 0, 0);
using (Transaction t = new Transaction(doc, "Move"))
{
    t.Start();
    ElementTransformUtils.MoveElement(doc, element.Id, translationVector);
    t.Commit();
}
```

### 2. Copy Element Pattern
```csharp
XYZ copyOffset = new XYZ(10, 0, 0);
using (Transaction t = new Transaction(doc, "Copy"))
{
    t.Start();
    ICollection<ElementId> newIds = ElementTransformUtils.CopyElement(doc, element.Id, copyOffset);
    t.Commit();
}
```

### 3. Rotate Element Pattern
```csharp
Line axisLine = Line.CreateBound(location, location + XYZ.BasisZ);
double angleInRadians = Math.PI / 4.0;
using (Transaction t = new Transaction(doc, "Rotate"))
{
    t.Start();
    ElementTransformUtils.RotateElement(doc, element.Id, axisLine, angleInRadians);
    t.Commit();
}
```

### 4. Mirror Element Pattern
```csharp
Plane mirrorPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisX, location);
using (Transaction t = new Transaction(doc, "Mirror"))
{
    t.Start();
    ElementTransformUtils.MirrorElement(doc, element.Id, mirrorPlane);
    t.Commit();
}
```

---

## 7. Deep Dive: Curve Evaluation & Sampling

### 1. `Evaluate(t, normalized: true)` vs `Evaluate(rawParam, normalized: false)`

```mermaid
flowchart TD
    Curve["3D Curve (e.g. Line, Arc, Spline)"] --> Mode{"Evaluation Mode?"}
    
    Mode -->|"normalized = true"| Norm["t ∈ [0.0, 1.0]<br/>0.0 = Start, 0.5 = Midpoint, 1.0 = End"]
    Mode -->|"normalized = false"| Raw["t ∈ [t_start, t_end]<br/>Line: Distance in feet<br/>Arc: Angle in radians"]
```

| Aspect | Normalized Parameter (`normalized = true`) | Raw Parameter (`normalized = false`) |
| :--- | :--- | :--- |
| **Parameter Range** | Always strictly $0.0 \dots 1.0$ | Varies $[t_{\text{start}}, t_{\text{end}}]$ |
| **Line Behavior** | $t$ is the percentage of total length | $t$ is arc-length distance in feet from line origin |
| **Arc / Circle Behavior** | $t$ is fraction of total arc span $(0 \dots 1)$ | $t$ is angle $\theta$ in radians $(0 \dots 2\pi)$ |

---

### 2. Changing the Value: Sampling Every $X$ Feet (e.g. Every 3 ft on a 12 ft Curve)

```csharp
double totalLength = curve.Length; // 12.0 ft
double stepDistance = 3.0;        // 3.0 ft interval

for (double dist = 0.0; dist <= totalLength + 1e-6; dist += stepDistance)
{
    double clampedDist = Math.Min(dist, totalLength);
    double tNormalized = Math.Clamp(clampedDist / totalLength, 0.0, 1.0);
    
    XYZ point = curve.Evaluate(tNormalized, normalized: true);
    // Process point (e.g. place hanger, stiffener)
}
```

---

## 8. Command 10 — Get LocationPoint End Point & Direction

**File:** [`GetLocationPointEndPointCommand.cs`](Commands/GetLocationPointEndPointCommand.cs)

```mermaid
flowchart TD
    Pick["Pick Element"] --> CheckType{"Location Type?"}
    CheckType -->|"LocationPoint"| LocPt["Read startPoint = locPoint.Point"]
    LocPt --> Ray["Method 1: startPoint + (HandOrientation * length)"]
    LocPt --> Matrix["Method 2: Transform.OfPoint(localEndPoint)"]
    LocPt --> MEP["Method 3: MEP ConnectorManager Ports"]
    LocPt --> Elev["Infeed vs Outfeed Z-Elevation Analysis (ΔZ, Run, Slope)"]
    
    CheckType -->|"LocationCurve"| LocCrv["Read curve.GetEndPoint(0) and (1)<br/>Direction = (End - Start).Normalize()"]
```

### Complete Code Recipe

```csharp
[Transaction(TransactionMode.ReadOnly)]
public class GetLocationPointEndPointCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select an element to calculate End Point");
        Element element = doc.GetElement(pickedRef);

        if (element is FamilyInstance familyInstance && element.Location is LocationPoint locPoint)
        {
            XYZ startPoint = locPoint.Point;
            double length = familyInstance.LookupParameter("Length")?.AsDouble() ?? 10.0;

            // 1. Universal Vector Ray Projection (HandOrientation)
            XYZ handDir = familyInstance.HandOrientation;
            XYZ endPointHand = startPoint + (handDir * length);

            // 2. 3D Transform Matrix (Transform.OfPoint)
            Autodesk.Revit.DB.Transform transform = familyInstance.GetTotalTransform();
            XYZ endPointTransform = transform.OfPoint(new XYZ(length, 0, 0));

            // 3. Elevation Analysis
            double deltaZ = endPointHand.Z - startPoint.Z;
            double horizontalRun = Math.Sqrt(Math.Pow(endPointHand.X - startPoint.X, 2) + Math.Pow(endPointHand.Y - startPoint.Y, 2));
            double slopePercent = (horizontalRun > 0.0001) ? (deltaZ / horizontalRun) * 100.0 : 0.0;

            TaskDialog.Show("End Point Result", 
                $"Start Point (Infeed) : ({startPoint.X:F2}, {startPoint.Y:F2}, {startPoint.Z:F2})\n" +
                $"End Point (Outfeed)  : ({endPointHand.X:F2}, {endPointHand.Y:F2}, {endPointHand.Z:F2})\n" +
                $"Height Delta (ΔZ)    : {deltaZ:F3} ft\n" +
                $"Calculated Slope     : {slopePercent:F1}%");
        }

        return Result.Succeeded;
    }
}
```

---

## 9. Common Mistakes to Avoid

1. **Modifying Location without a Transaction** — all model changes require a Transaction.
2. **Using degrees instead of radians** for `RotateElement` — always use `Math.PI / 180 * degrees`.
3. **Hardcoding $Z = 0$ in 2D polar formulas** — forces flat calculation and ignores 3D inclination.
4. **Forgetting `GetTransform()` is only on `FamilyInstance`** — not on `Wall`, `Floor`, etc.
5. **Assuming `LocationPoint` gives the full 3D direction** — always use `HandOrientation` or `GetTransform().BasisZ`.
6. **Calling `MEPModel.ConnectorManager` on Non-MEP Families** — causes `NullReferenceException`.

---

## 10. Transform API Cheat Sheet

| API Symbol | Description | Code Example |
| :--- | :--- | :--- |
| `Element.Location` | Gets physical location of element. | `Location loc = elem.Location;` |
| `LocationPoint` | Point-based location (doors, columns, equipment). | `XYZ pt = (loc as LocationPoint).Point;` |
| `LocationCurve` | Curve-based location (walls, beams, ducts, pipes). | `Curve c = (loc as LocationCurve).Curve;` |
| `familyInstance.HandOrientation` | Local X-axis unit vector in 3D world space. | `XYZ dir = inst.HandOrientation;` |
| `familyInstance.FacingOrientation` | Local Y-axis unit vector in 3D world space. | `XYZ dir = inst.FacingOrientation;` |
| `Transform.BasisZ` | Element's local Z-axis = 3D orientation axis in world space. | `XYZ dir = inst.GetTransform().BasisZ;` |
| `Transform.OfPoint(localPt)` | Converts a local coordinate to world coordinates. | `XYZ world = t.OfPoint(localPt);` |
| `direction.Normalize()` | Returns a 3D unit vector from two points. | `XYZ unit = (end - start).Normalize();` |

# Module 10 — Transform

## 1. Transform Mental Model

A Transform in Revit is a mathematical object that encodes three properties in one: Translation (where is the element's origin in world space), Rotation (how is the element's local coordinate system oriented), and Scale. It is represented as a 4×4 matrix. The Revit API exposes this as the `Transform` class in `Autodesk.Revit.DB`.

```mermaid
graph TD
    T["Transform (4x4 Matrix)"]
    T --> O["Origin<br/>(Translation)"]
    T --> R["BasisX, BasisY, BasisZ<br/>(Rotation)"]
    T --> S["Scale"]
```

## 2. Location vs Transform

| Feature | Location (Point/Curve) | Transform |
| :--- | :--- | :--- |
| **Availability** | All elements | `FamilyInstance` (via `GetTransform()`) |
| **Data Provided** | XYZ point or Curve | Full 4x4 matrix |
| **Use Case** | Simple positioning | Coordinate system conversions, exact rotation/scale |

```mermaid
flowchart LR
    E["Element"]
    E --> L["Location Property"]
    L --> LP["LocationPoint"]
    L --> LC["LocationCurve"]
    
    FI["FamilyInstance"]
    FI --> T["GetTransform()"]
    T --> TM["Transform Matrix"]
```

## 3. ElementTransformUtils — The Movement API

`ElementTransformUtils` is the primary API for moving, copying, rotating, and mirroring elements in a Revit project. All operations that modify the model require an active `Transaction`.

| Method | Description |
| :--- | :--- |
| `MoveElement` | Moves an element by an XYZ translation vector. |
| `CopyElement` | Copies an element to a new location. |
| `RotateElement` | Rotates an element around a given axis line. |
| `MirrorElement` | Mirrors an element in-place across a plane. |
| `MirrorElements` | Copies and mirrors elements across a plane. |

## 4. Learning Progression (Commands 01–09)

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

## 5. Key Workflows & API Patterns

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

### 5. Get Point on Curve Pattern
```csharp
LocationCurve locCurve = element.Location as LocationCurve;
Curve curve = locCurve.Curve;

XYZ startPoint = curve.GetEndPoint(0);         // t = 0.0
XYZ midPoint   = curve.Evaluate(0.5, true);    // t = 0.5, normalized
XYZ endPoint   = curve.GetEndPoint(1);         // t = 1.0
XYZ direction  = (endPoint - startPoint).Normalize();

// dZ of direction = elevation/inclination component
// dZ = 0.0  → horizontal element
// dZ = 1.0  → perfectly vertical element
double elevAngleDeg = Math.Asin(direction.Z) * (180.0 / Math.PI);
double deltaZ = endPoint.Z - startPoint.Z; // total elevation rise
```

### 6. PointFamily Start / End / Direction Pattern
```csharp
// For a PointBased family (LocationPoint only):
LocationPoint locPoint = familyInstance.Location as LocationPoint;
XYZ startPoint = locPoint.Point;  // base origin

// GetTransform().BasisZ = element's 3D axis direction in world space
Transform tfm = familyInstance.GetTransform();
XYZ direction = tfm.BasisZ;  // normalized; this IS the 3D inclination direction

// Compute end point if you know the length
double length = familyInstance
    .get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble();
XYZ endPoint = startPoint + direction * length;

// Elevation analysis from direction.Z
// direction.Z == 0  → horizontal, direction.Z == 1 → vertical
double elevAngleDeg = Math.Asin(direction.Z) * (180.0 / Math.PI);
double xyAngleDeg   = Math.Atan2(direction.Y, direction.X) * (180.0 / Math.PI);
```

### 7. Divide Curve by Distance Interval Pattern (e.g. 12 ft Curve Every 3 ft)
```csharp
LocationCurve locCurve = element.Location as LocationCurve;
Curve curve = locCurve.Curve;

double totalLength  = curve.Length; // e.g. 12.0 ft
double stepDistance = 3.0;         // step in feet (e.g. 3.0 ft)

for (double dist = 0.0; dist <= totalLength + 1e-6; dist += stepDistance)
{
    // Clamp to [0, totalLength] to avoid floating point overshoot
    double clampedDist = Math.Min(dist, totalLength);
    
    // Normalized parameter t = distance / totalLength
    double tNormalized = clampedDist / totalLength; // 0.0, 0.25, 0.50, 0.75, 1.0
    tNormalized = Math.Clamp(tNormalized, 0.0, 1.0);
    
    // Evaluate 3D point at normalized parameter
    XYZ pointAtDist = curve.Evaluate(tNormalized, normalized: true);
    
    // Compute local coordinate frame & tangent vector
    Transform der = curve.ComputeDerivatives(tNormalized, normalized: true);
    XYZ tangent = der.BasisX.Normalize();
}
```

### 8. Curve Local Transform & Tangent Vectors Pattern
```csharp
// ComputeDerivatives returns a Transform object representing the local
// coordinate frame at any point along the curve!
Transform localTransform = curve.ComputeDerivatives(0.5, normalized: true);

XYZ pointOnCurve = localTransform.Origin;          // 3D position (same as Evaluate)
XYZ tangentVec   = localTransform.BasisX.Normalize(); // Tangent direction along curve
XYZ normalVec    = localTransform.BasisY.Normalize(); // Normal direction (curvature)
XYZ binormalVec  = localTransform.BasisZ.Normalize(); // Binormal (perpendicular to osculating plane)
```


## 6. Per-Command Reasoning (Commands 01–06)

### Command 01: Inspect Location
```mermaid
flowchart LR
    S["Select Element"] --> L["Get Location Property"]
    L --> LP["Cast to LocationPoint"]
    L --> LC["Cast to LocationCurve"]
```
This command teaches how to extract basic positioning information from an element. It shows how `Location` is the base class and must be cast to a point or curve depending on the element type.

### Command 02: Move Element
```mermaid
flowchart LR
    S["Select Element"] --> TV["Define XYZ Translation"]
    TV --> T["Start Transaction"]
    T --> ME["MoveElement()"]
```
This demonstrates basic translation. It enforces the concept that moving an element requires a vector, not absolute coordinates.

### Command 03: Copy Element
```mermaid
flowchart LR
    S["Select Element"] --> CO["Define Copy Offset XYZ"]
    CO --> T["Start Transaction"]
    T --> CE["CopyElement()"]
    CE --> I["Returns new ElementIds"]
```
Shows how copying works similarly to moving, but crucially returns the newly created element IDs, which is essential for chained operations.

### Command 04: Rotate Element
```mermaid
flowchart LR
    S["Select Element"] --> AL["Create Axis Line"]
    AL --> AR["Angle in Radians"]
    AR --> T["Start Transaction"]
    T --> RE["RotateElement()"]
```
Teaches creating a rotation axis using `Line` and reinforces that the Revit API always expects angles in radians, not degrees.

### Command 05: Mirror Element
```mermaid
flowchart LR
    S["Select Element"] --> MP["Create Mirror Plane"]
    MP --> T["Start Transaction"]
    T --> ME["MirrorElement()"]
```
Explains how to define a 3D plane using a normal and origin, and demonstrates in-place mirroring vs. copying.

### Command 06: Transform Geometry
```mermaid
flowchart LR
    S["Select FamilyInstance"] --> GT["GetTransform()"]
    GT --> R["Read Origin, BasisX/Y/Z"]
    GT --> OP["OfPoint() to transform coords"]
```
Moves beyond simple `ElementTransformUtils` to accessing the raw math of an element's placement in the model, demonstrating coordinate system transformation.

---

### Command 07: Get Point On Curve

**Why this command exists**: A CurveBased family (beam, inclined framing, MEP element) stores its position as a `LocationCurve`. Once you have the underlying `Curve`, `curve.Evaluate(t, normalized: true)` lets you get *any* point along it. The `t` parameter is a proportion (0.0 = start, 0.5 = midpoint, 1.0 = end). The direction vector's **dZ component** directly encodes the elevation change per unit length — making this the primary analysis tool for inclined 3D elements.

```mermaid
flowchart TD
    A["Select CurveBased FamilyInstance"] --> B["element.Location as LocationCurve"]
    B --> C{"locCurve == null?"}
    C -- "null" --> D["Return Failed — not CurveBased"]
    C -- "valid" --> E["locCurve.Curve"]
    E --> F["curve.GetEndPoint(0) → Start  t=0.0"]
    E --> G["curve.Evaluate(0.5, true) → Midpoint  t=0.5"]
    E --> H["curve.GetEndPoint(1) → End  t=1.0"]
    E --> I["curve.Length → total length in feet"]
    F & H --> J["direction = (End - Start).Normalize()"]
    J --> K["direction.Z → elevation component"]
    K --> L["Asin(direction.Z) → elevation angle in degrees"]
```

**Architectural unlock**: `curve.Evaluate(t, normalized)` is the universal point-on-curve query tool. The `direction.Z` pattern is how you programmatically detect horizontal vs. inclined vs. vertical elements — essential for structural analysis automation.

---

### Command 08: Point Family Start / End / Direction

**Why this command exists**: A PointBased FamilyInstance (OneLevelBased column, inclined structural element) only exposes a single XYZ via `LocationPoint` — its base origin. You have a point and a length, but **no end point and no direction**. The solution is `GetTransform().BasisZ` — the element's local Z axis expressed in world space. For a plumb column this is `(0,0,1)`. For an inclined element it tilts proportionally. Multiplying `BasisZ × Length` gives the end point. The `BasisZ.Z` component is the sine of the elevation angle.

```mermaid
flowchart TD
    A["Select FamilyInstance"] --> B{"Location Type?"}
    B -- "LocationCurve" --> C["CurveBased: direct start/end from curve"]
    C --> DIR["direction = End - Start / .Normalize()"]
    B -- "LocationPoint" --> D["PointBased: only base point available"]
    D --> D1["startPoint = locPoint.Point"]
    D --> D2["familyInstance.GetTransform().BasisZ"]
    D2 --> D3["TryGetLength: BuiltInParam + named params"]
    D3 --> D4["endPoint = startPoint + BasisZ * Length"]
    D2 & D4 --> DIR
    DIR --> E["dZ component"]
    DIR --> F["XY Angle = Atan2(dY, dX)"]
    DIR --> G["Elevation = Asin(dZ) in degrees"]
    E --> H{"dZ value?"}
    H -- "≈ 0" --> H1["Horizontal"]
    H -- "≈ ±1" --> H2["Vertical"]
    H -- "between" --> H3["Inclined in 3D"]
```

**Architectural unlock**: `GetTransform().BasisZ` answers *"In which direction does this element extend in 3D world space?"* for any PointBased FamilyInstance. This is the only API path to the 3D axis direction when `LocationCurve` is not available — critical for structural, MEP, and any inclined element analysis.

---

### Command 09: Divide Curve By Distance (Custom Intervals & Stations)

**Why this command exists**: Many real-world BIM automation workflows require placing elements or sampling geometry at fixed distance intervals along an element rather than just at the midpoint or ends. Examples include placing structural stiffeners, MEP pipe/duct hangers every 6 feet, rebar stirrups, or analyzing clearances at regular stations. This command shows how to map any physical distance $d$ (e.g. 3 ft along a 12 ft curve) to the normalized parameter domain $t \in [0.0, 1.0]$, evaluate 3D points, and retrieve the tangent vectors using `ComputeDerivatives()`.

```mermaid
flowchart TD
    A["Select CurveBased Element (12 ft Beam)"] --> B["locCurve.Curve"]
    B --> C["Total Length L = curve.Length (12.0 ft)"]
    C --> D["Define Step Interval d = 3.0 ft"]
    D --> E["Loop: dist = 0, 3, 6, 9, 12 ft"]
    E --> F["Normalized Param t = dist / L"]
    F --> G["curve.Evaluate(t, true) → 3D Point"]
    F --> H["curve.ComputeDerivatives(t, true) → Local Transform"]
    H --> I["Transform.BasisX → Tangent Vector"]
```

**Architectural unlock**: `t = dist / totalLength` paired with `curve.Evaluate(t, true)` enables universal equidistant sampling across any curve geometry in Revit.

---

## 7. Deep Dive: `Curve.Evaluate(param, normalized)` — True vs. False & Custom Spacing

### 1. How to Get the Middle of a Curve

There are two primary ways to compute the midpoint of a curve in Revit API:

#### Method A: Normalized Parameter (Recommended)
```csharp
// normalized = true maps curve start to 0.0 and end to 1.0
XYZ midPoint = curve.Evaluate(0.5, normalized: true);
```

#### Method B: Raw / Natural Parameter
```csharp
// Query raw parameter bounds from the geometry kernel
double rawStart = curve.GetEndParameter(0);
double rawEnd   = curve.GetEndParameter(1);
double rawMid   = (rawStart + rawEnd) / 2.0;

// normalized = false uses the raw geometric parameter domain
XYZ midPoint = curve.Evaluate(rawMid, normalized: false);
```
> Both methods produce the exact same 3D point `(X, Y, Z)` in space. Method A is significantly cleaner and less error-prone.

---

### 2. When to Use `true` vs. When to Use `false`

| Property | `normalized: true` (Proportional) | `normalized: false` (Raw Parameter) |
| :--- | :--- | :--- |
| **Parameter Domain** | Strictly $[0.0, 1.0]$ | $[t_{\text{min}}, t_{\text{max}}]$ from `GetEndParameter(0)` / `(1)` |
| **Start Point** | $t = 0.0$ | $t = \text{curve.GetEndParameter}(0)$ |
| **Mid Point** | $t = 0.5$ | $t = (\text{rawStart} + \text{rawEnd}) / 2.0$ |
| **End Point** | $t = 1.0$ | $t = \text{curve.GetEndParameter}(1)$ |
| **Line Behavior** | $t$ is the percentage of total length | $t$ is arc-length distance in feet from line origin |
| **Arc / Circle Behavior** | $t$ is fraction of total arc span $(0 \dots 1)$ | $t$ is angle $\theta$ in radians $(0 \dots 2\pi)$ |
| **NURBS / Spline Behavior** | $t$ is normalized across knot domain | $t$ is raw knot parameter value |
| **When to Use** | • Dividing curves into equal segments ($i/N$)<br/>• Finding midpoints ($0.5$) or quarter points ($0.25$)<br/>• Sampling by distance ratio ($\text{dist} / \text{Length}$)<br/>• General geometry evaluation without knowing curve type | • Handling outputs from `curve.Project(pt).Parameter`<br/>• Handling intersection results `IntersectionResult.Parameter`<br/>• Working directly with radian angles on Arcs/Ellipses<br/>• Custom mathematical curve stepping along knot vectors |

---

### 3. Changing the Value: Sampling Every $X$ Feet (e.g. Every 3 ft on a 12 ft Curve)

To evaluate points at any custom distance $d$ along a curve of total length $L$:

$$\text{Normalized Parameter } t = \frac{\text{Distance Along Curve } d}{\text{Total Curve Length } L}$$

#### Example: 12 ft Beam Sampled Every 3 ft ($L = 12\text{ ft}, \Delta d = 3\text{ ft}$)

| Step | Distance $d$ (ft) | Formula $t = d / L$ | Parameter $t$ | Evaluation Call | Description |
| :---: | :---: | :---: | :---: | :--- | :--- |
| 0 | $0.0\text{ ft}$ | $0 / 12$ | **$0.00$** | `curve.Evaluate(0.00, true)` | **Start Point** (`GetEndPoint(0)`) |
| 1 | $3.0\text{ ft}$ | $3 / 12$ | **$0.25$** | `curve.Evaluate(0.25, true)` | **Quarter Point** (25% span) |
| 2 | $6.0\text{ ft}$ | $6 / 12$ | **$0.50$** | `curve.Evaluate(0.50, true)` | **Midpoint** (50% span) |
| 3 | $9.0\text{ ft}$ | $9 / 12$ | **$0.75$** | `curve.Evaluate(0.75, true)` | **Three-Quarter Point** (75% span) |
| 4 | $12.0\text{ ft}$ | $12 / 12$ | **$1.00$** | `curve.Evaluate(1.00, true)` | **End Point** (`GetEndPoint(1)`) |

#### Robust Sampling Implementation in C#
```csharp
double totalLength = curve.Length; // 12.0 ft
double stepDistance = 3.0;        // 3.0 ft interval

for (double dist = 0.0; dist <= totalLength + 1e-6; dist += stepDistance)
{
    // Clamp to prevent floating-point overshoot past 1.0
    double clampedDist = Math.Min(dist, totalLength);
    double tNormalized = Math.Clamp(clampedDist / totalLength, 0.0, 1.0);
    
    XYZ point = curve.Evaluate(tNormalized, normalized: true);
    
    // Process point (e.g. place family instance, hanger, stiffener)
}
```

---

### 4. Connection Between Curve Evaluation and `Transform`

When evaluating a curve, `curve.ComputeDerivatives(t, normalized)` returns an `Autodesk.Revit.DB.Transform` object representing the full local coordinate frame at that point:

```mermaid
graph TD
    CD["curve.ComputeDerivatives(t, true)"] --> TF["Transform Object"]
    TF --> O["Transform.Origin = 3D Point on Curve"]
    TF --> BX["Transform.BasisX = Tangent Vector (Curve Direction)"]
    TF --> BY["Transform.BasisY = Normal Vector (Curvature)"]
    TF --> BZ["Transform.BasisZ = Binormal Vector"]
```

This means you can calculate both the exact 3D location and the orientation matrix required to rotate and place a 3D family instance along the curve in a single call.

---

## 8. Common Mistakes to Avoid

1. **Modifying Location without a Transaction** — all model changes require a Transaction.
2. **Using degrees instead of radians** for `RotateElement` — always use `Math.PI / 180 * degrees`.
3. **Confusing `MirrorElement` (moves) with `MirrorElements` (creates a copy)**.
4. **Forgetting `GetTransform()` is only on `FamilyInstance`** — not on `Wall`, `Floor`, etc.
5. **Passing a zero-length rotation axis `Line`** — `Line.CreateBound` with identical start/end throws an exception.
6. **Assuming `LocationPoint` gives the full 3D position** — for inclined elements you must also use `GetTransform().BasisZ` to find the direction.
7. **Using `curve.Evaluate(t, normalized: false)` with fractional values ($0.0 \dots 1.0$)** — passing $0.5$ with `normalized: false` evaluates at $0.5$ feet on a line or $0.5$ radians on an arc, NOT the midpoint!
8. **Floating-point overshoot in step loops** — always clamp $t = \text{Math.Clamp}(d / L, 0.0, 1.0)$ to avoid evaluating slightly past $1.0$ (e.g. $1.0000001$).

---

## 9. Transform API Cheat Sheet

| API Symbol | Description | Code Example |
| :--- | :--- | :--- |
| `Element.Location` | Gets physical location of element. | `Location loc = elem.Location;` |
| `LocationPoint` | Point-based location (doors, columns). | `XYZ pt = (loc as LocationPoint).Point;` |
| `LocationCurve` | Curve-based location (walls, beams). | `Curve c = (loc as LocationCurve).Curve;` |
| `curve.GetEndPoint(0)` | Start point of a curve ($t = 0.0$). | `XYZ start = curve.GetEndPoint(0);` |
| `curve.GetEndPoint(1)` | End point of a curve ($t = 1.0$). | `XYZ end = curve.GetEndPoint(1);` |
| `curve.Evaluate(0.5, true)` | Midpoint using normalized proportion ($0.0 \dots 1.0$). | `XYZ mid = curve.Evaluate(0.5, true);` |
| `curve.Evaluate(dist / len, true)` | Custom point at distance `dist` along curve. | `XYZ pt = curve.Evaluate(3.0 / 12.0, true);` |
| `curve.Evaluate(rawParam, false)` | Point using raw parameter $[t_{\text{start}}, t_{\text{end}}]$. | `XYZ pt = curve.Evaluate(rawParam, false);` |
| `curve.GetEndParameter(0)` | Raw start parameter of curve. | `double t0 = curve.GetEndParameter(0);` |
| `curve.GetEndParameter(1)` | Raw end parameter of curve. | `double t1 = curve.GetEndParameter(1);` |
| `curve.ComputeDerivatives(t, true)` | Returns `Transform` with Origin & Tangent (`BasisX`). | `Transform tf = curve.ComputeDerivatives(0.5, true);` |
| `curve.Length` | Total arc length of the curve in feet. | `double len = curve.Length;` |
| `MoveElement` | Translates an element by an XYZ vector. | `ElementTransformUtils.MoveElement(doc, id, vec);` |
| `CopyElement` | Copies an element, returns new IDs. | `ElementTransformUtils.CopyElement(doc, id, offset);` |
| `RotateElement` | Rotates an element around an axis (radians). | `ElementTransformUtils.RotateElement(doc, id, axis, rad);` |
| `MirrorElement` | Mirrors an element in-place across a plane. | `ElementTransformUtils.MirrorElement(doc, id, plane);` |
| `GetTransform()` | Gets the 4×4 matrix of a FamilyInstance. | `Transform t = inst.GetTransform();` |
| `Transform.BasisZ` | Element's local Z axis = 3D direction in world. | `XYZ dir = inst.GetTransform().BasisZ;` |
| `Transform.OfPoint(pt)` | Converts a local point to world coordinates. | `XYZ world = t.OfPoint(localPt);` |
| `direction.Normalize()` | Returns a unit vector from a direction. | `XYZ unit = (end - start).Normalize();` |
| `Math.Asin(dZ) → degrees` | Elevation angle from direction.Z component. | `double deg = Math.Asin(dir.Z) * 180 / Math.PI;` |
| `Math.Atan2(dY, dX) → degrees` | XY plan rotation angle from direction. | `double deg = Math.Atan2(dir.Y, dir.X) * 180 / Math.PI;` |



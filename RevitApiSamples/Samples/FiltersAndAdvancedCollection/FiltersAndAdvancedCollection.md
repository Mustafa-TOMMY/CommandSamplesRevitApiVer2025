# Module 15 — Filters & Advanced Collection

## 1. Mental Model & Architecture

While **Module 02 (ElementCollection)** introduces basic collection queries (`OfCategory`, `OfClass`, `WhereElementIsNotElementType`), **Module 15 (Filters & Advanced Collection)** focuses on high-performance database filtering, complex boolean composition, parameter rule factories, and **true 3D physical collision detection**.

```mermaid
flowchart TD
    Collector["new FilteredElementCollector(doc)"]
    
    subgraph FastFilters ["1. Fast Native Pre-Filtering (Quick Filters)"]
        F1["Category & Class Filters"]
        F2["BoundingBoxIntersectsFilter (Outline)"]
        F3["ExclusionFilter (Exclude Self / Selected)"]
    end

    subgraph BooleanLogic ["2. Logical Composition"]
        B1["LogicalAndFilter / LogicalOrFilter"]
    end

    subgraph Geometry3D ["3. Exact 3D Collision (Slow Filters)"]
        G1["ElementIntersectsElementFilter (Host Clash)"]
        G2["ElementIntersectsSolidFilter (Clearance / Link)"]
    end

    Collector --> F1
    Collector --> F2
    Collector --> F3
    
    F1 --> B1
    F2 --> B1
    F3 --> B1
    
    B1 --> G1
    B1 --> G2
    
    G1 --> Results["ToElements() - Final Filtered Elements"]
    G2 --> Results
```

---

## 2. Quick Filters vs. Slow Filters

In the Revit API, every filter inherits from the abstract base class `ElementFilter`. Understanding whether a filter is **Quick** or **Slow** is vital for writing performant add-ins on large enterprise models:

| Filter Type | Base Class | Performance | Execution Mechanism | Examples |
| :--- | :--- | :--- | :--- | :--- |
| **Quick Filter** | `ElementQuickFilter` | ⚡ **Ultra Fast** (Microseconds) | Evaluates memory-cached headers in native Revit database without expanding the full element record. | `ElementCategoryFilter`, `ElementClassFilter`, `BoundingBoxIntersectsFilter`, `ExclusionFilter`, `ElementMulticategoryFilter`. |
| **Slow Filter** | `ElementSlowFilter` | 🐢 **Heavy / Precise** (Milliseconds) | Expands the full element definition, reads non-indexed parameters, or extracts 3D solid geometry for Boolean tests. | `ElementParameterFilter`, `ElementIntersectsElementFilter`, `ElementIntersectsSolidFilter`, `ElementLevelFilter`. |

> [!TIP]
> **Golden Rule of Collector Chaining:**
> Always apply **Quick Filters FIRST** to aggressively eliminate 90%+ of irrelevant elements before applying a **Slow 3D Geometry Filter**.

---

## 3. Deep Dive: `LogicalAndFilter` vs. `LogicalOrFilter`

Revit allows you to build complex Boolean query trees in unmanaged C++ memory using `LogicalAndFilter` and `LogicalOrFilter`.

### Conceptual Comparison

```mermaid
flowchart TD
    subgraph LogicalOrTree ["LogicalOrFilter (Union ∪) — Widens Scope"]
        OR["LogicalOrFilter"]
        F_Wall["Filter A: Walls"]
        F_Col["Filter B: Columns"]
        F_Wall --> OR
        F_Col --> OR
    end

    subgraph LogicalAndTree ["LogicalAndFilter (Intersection ∩) — Narrows Scope"]
        AND["LogicalAndFilter"]
        F_Level["Filter C: Level 1"]
        OR --> AND
        F_Level --> AND
    end

    AND --> Final["Result: (Walls OR Columns) on Level 1"]
```

### Direct Feature Comparison

| Feature | `LogicalAndFilter` | `LogicalOrFilter` |
| :--- | :--- | :--- |
| **Set Theory Operation** | **Intersection ($\cap$)** — Must satisfy all criteria | **Union ($\cup$)** — Must satisfy at least one criteria |
| **C# Equivalent** | `conditionA && conditionB` | `conditionA \|\| conditionB` |
| **Effect on Result Count** | **Narrows / Reduces** candidate element count | **Widens / Increases** candidate element count |
| **Constructor Overloads** | 1. `new LogicalAndFilter(filterA, filterB)`<br/>2. `new LogicalAndFilter(IList<ElementFilter>)` | 1. `new LogicalOrFilter(filterA, filterB)`<br/>2. `new LogicalOrFilter(IList<ElementFilter>)` |
| **Collector Chaining** | Calling `.WherePasses(F1).WherePasses(F2)` implicitly acts as an **AND** | Chaining multiple `.WherePasses()` cannot do OR; you **must** use `LogicalOrFilter` |
| **Primary Use Case** | Combining different criteria types (e.g., `Category == Wall` **AND** `Length >= 10ft`) | Grouping multiple alternatives (e.g., `Category == Ducts` **OR** `Category == Pipes`) |

---

## 4. Master Comparison Matrix: Similar Functions & When to Use Which

To help you decide which filter or query API to choose in different scenarios:

### 1. Category Filtering Approaches

| Method | API Class | Performance | When to Use |
| :--- | :--- | :--- | :--- |
| **Single Category** | `.OfCategory(BuiltInCategory)` | ⚡ Ultra Fast | When you only need elements of one category. |
| **Multiple Categories (Native)** | `new ElementMulticategoryFilter(categoriesList)` | ⚡ Ultra Fast | **Recommended:** When querying multiple categories in one single database scan. |
| **Multiple Categories (Composite)** | `new LogicalOrFilter(categoryFilterList)` | ⚡ Fast | Functional equivalent to `ElementMulticategoryFilter`, but slightly more verbose to construct. |
| **LINQ Merge (Anti-Pattern)** | Multiple collectors merged with `.Union()` in C# | 🐢 Slow | Avoid: Loads all objects across multiple passes into .NET memory. |

---

### 2. Spatial & Collision Checking Approaches

| Approach | Main API | Precision | Performance | Best Use Case |
| :--- | :--- | :--- | :--- | :--- |
| **Bounding Box Overlap** | `BoundingBoxIntersectsFilter` | 🔲 Coarse (AABB box) | ⚡ Fast (Quick) | Pre-filtering candidate clash sets before 3D solid checks. |
| **Strict Bounding Box Containment** | `BoundingBoxIsInsideFilter` | 🔲 Coarse (AABB box) | ⚡ Fast (Quick) | Checking if an element is completely inside a rectangular room/zone. |
| **Point in Bounding Box** | `BoundingBoxContainsPointFilter` | 📍 Point vs. Box | ⚡ Fast (Quick) | Finding elements whose bounding box encompasses a given 3D coordinate. |
| **Host Element 3D Collision** | `ElementIntersectsElementFilter` | 🧊 Exact (3D Solid) | 🐢 Heavy (Slow) | True physical clash check against a live element in the host model. |
| **Custom Solid / Clearance** | `ElementIntersectsSolidFilter` | 🧊 Exact (3D Solid) | 🐢 Heavy (Slow) | Clearance buffers (+50mm offset envelope) or non-standard 3D volumes. |
| **Linked Model Collision** | `ElementIntersectsSolidFilter` + `SolidUtils.CreateTransformed` | 🧊 Exact (3D Solid) | 🐢 Heavy (Slow) | Checking clashes between host elements and linked model elements across coordinate spaces. |

---

### 3. Database Parameter Filtering vs. C# LINQ

| Aspect | `ElementParameterFilter` (Native) | C# LINQ `.Where(e => ...)` (Managed) |
| :--- | :--- | :--- |
| **Execution Layer** | Native unmanaged C++ Revit core | Managed .NET CLR runtime |
| **Memory Allocation** | Zero .NET object allocations for non-matching elements | Creates a managed `Element` proxy for **every** element in the database |
| **Speed on Large Models** | Up to **10x – 50x faster** | Noticeable UI lag / high garbage collection overhead |
| **Verdict** | Always use for initial database querying | Use only for complex business logic after native filtering |

---

## 5. Deep Dive: 3D Geometry Intersection Filters

```mermaid
classDiagram
    class ElementFilter {
        <<abstract>>
    }
    class ElementSlowFilter {
        <<abstract>>
    }
    class ElementIntersectsFilter {
        <<abstract>>
    }
    class ElementIntersectsElementFilter {
        +Element TargetElement
        +bool Inverted
    }
    class ElementIntersectsSolidFilter {
        +Solid TargetSolid
        +bool Inverted
    }
    
    ElementFilter <|-- ElementSlowFilter
    ElementSlowFilter <|-- ElementIntersectsFilter
    ElementIntersectsFilter <|-- ElementIntersectsElementFilter
    ElementIntersectsFilter <|-- ElementIntersectsSolidFilter
```

### The 4 Intersection Classes

1. **`ElementIntersectsFilter`**
   * **Role:** The **abstract base class** for 3D geometry filters. It cannot be instantiated directly; it serves as the common polymorphic parent.
2. **`ElementIntersectsElementFilter`**
   * **Role:** Evaluates physical 3D solid collisions against an active model element in the same document.
   * **Best used for:** Direct intra-document interference checks (e.g., Pipe vs. Duct, Beam vs. Wall).
3. **`ElementIntersectsSolidFilter`**
   * **Role:** Evaluates physical 3D collisions against an explicit in-memory `Solid`.
   * **Best used for:** 
     * **Clearance Zones:** Enlarged buffer envelopes (e.g. +50mm around MEP services).
     * **Cross-Model Linked Clashes:** Solids extracted from a `RevitLinkInstance` and transformed into host world space.
     * **Virtual Spaces:** Room solids, corridor bounds, or construction zones.
4. **`ElementIntersection`**
   * **Role:** Geometric intersection result evaluation / classification helper.

---

## 6. Architectural Guide: Cross-Model Clash Detection

In multi-discipline BIM environments, elements to be checked often reside in **Revit Links** (e.g., Structural Model linked into MEP Model).

### The Coordinate Challenge:
`ElementIntersectsElementFilter` cannot directly query across different documents because each linked model has its own local coordinate system and `Document` instance.

### The Standard Solution Workflow:

```mermaid
flowchart TD
    A["1. Pick Element in Linked Model<br/>(ObjectType.LinkedElement)"] --> B["2. Retrieve RevitLinkInstance<br/>and Link Document"]
    B --> C["3. Extract 3D Solid from Linked Element<br/>(element.get_Geometry)"]
    C --> D["4. Transform Solid into Host World Coordinates<br/>SolidUtils.CreateTransformed(solid, linkTransform)"]
    D --> E["5. Create ElementIntersectsSolidFilter(transformedSolid)"]
    E --> F["6. Execute Collector on Host Document<br/>FilteredElementCollector(hostDoc)"]
    F --> G["7. Retrieve Host Elements Intersecting Linked Geometry"]
```

---

## 7. Learning Progression (Commands 01–08)

| # | Command File | Class Name | Main API | What It Teaches |
| :--- | :--- | :--- | :--- | :--- |
| **01** | [LogicalFiltersCommand.cs](Commands/LogicalFiltersCommand.cs) | `LogicalFiltersCommand` | `LogicalAndFilter`, `LogicalOrFilter` | Combining multiple search rules using boolean logic trees in native C++ memory. |
| **02** | [ParameterRuleFilterCommand.cs](Commands/ParameterRuleFilterCommand.cs) | `ParameterRuleFilterCommand` | `ElementParameterFilter`, `ParameterFilterRuleFactory` | Evaluating string, numeric, and inverted parameter conditions without loading elements into C#. |
| **03** | [MultiCategoryFilterCommand.cs](Commands/MultiCategoryFilterCommand.cs) | `MultiCategoryFilterCommand` | `ElementMulticategoryFilter` | Querying multiple categories simultaneously in a single native collector pass. |
| **04** | [ExclusionFilterCommand.cs](Commands/ExclusionFilterCommand.cs) | `ExclusionFilterCommand` | `ExclusionFilter` | Natively excluding specific element IDs (e.g., selected or already processed elements). |
| **05** | [BoundingBoxSpatialFilterCommand.cs](Commands/BoundingBoxSpatialFilterCommand.cs) | `BoundingBoxSpatialFilterCommand` | `BoundingBoxIntersectsFilter`, `Outline` | Fast Axis-Aligned Bounding Box (AABB) spatial queries for pre-filtering candidate clash sets. |
| **06** | [ElementIntersectsElementCommand.cs](Commands/ElementIntersectsElementCommand.cs) | `ElementIntersectsElementCommand` | `ElementIntersectsElementFilter` | 3D solid collision detection against a selected host element. |
| **07** | [ElementIntersectsSolidCommand.cs](Commands/ElementIntersectsSolidCommand.cs) | `ElementIntersectsSolidCommand` | `ElementIntersectsSolidFilter`, `GeometryCreationUtilities` | Clearance envelope creation and custom 3D solid interference testing. |
| **08** | [LinkedModelIntersectionCommand.cs](Commands/LinkedModelIntersectionCommand.cs) | `LinkedModelIntersectionCommand` | `ElementIntersectsSolidFilter`, `RevitLinkInstance`, `SolidUtils` | Cross-model clash detection: Transforming linked element solids to host world space for collision querying. |

---

## 8. Summary of Best Practices & Common Pitfalls

1. **Avoid LINQ for Base Filtering:** Always prefer `ElementParameterFilter` or `ParameterFilterRuleFactory` over `.Where(x => x.LookupParameter(...))` whenever possible.
2. **Exclude Self in Collision Checks:** When checking clashes against a target element, always pass an `ExclusionFilter([target.Id])` to prevent the element from reporting a collision with itself.
3. **Bounding Box Pre-Pass:** Before applying `ElementIntersectsElementFilter` or `ElementIntersectsSolidFilter`, always apply a `BoundingBoxIntersectsFilter` with the target's bounding box `Outline` to discard non-proximate elements instantly.
4. **Always Transform Linked Solids:** Never pass a raw solid extracted from a linked model directly to `ElementIntersectsSolidFilter` without transforming it with `linkInstance.GetTotalTransform()`.

using Autodesk.Revit.DB;

namespace RevitApiSamples.Samples.ModelCreation.Helpers
{
    // ============================================================================
    // CurveLoopBuilder
    //
    // A reusable helper that converts an arbitrary collection of Curves into
    // one or more valid Revit CurveLoops.
    //
    // This helper is element-agnostic — it works with any IList<Curve>.
    // Walls, ModelCurves, DetailCurves, Edges, etc. should be converted to
    // Curves before calling Build().
    //
    // Intended consumers:
    //   - CreateFloorFromWalls
    //   - CreateFloorFromCurves
    //   - CreateRoof
    //   - CreateCeiling
    //   - FilledRegion
    //   - Openings
    //
    // High-level algorithm:
    //
    //   Input Curves
    //       ↓
    //   Build connectivity graph (adjacency by endpoint proximity)
    //       ↓
    //   Find connected components (BFS)
    //       ↓
    //   For each component:
    //       → Order curves into a chain
    //       → Reverse inconsistent curve directions
    //       → Validate (≥3 curves, continuous, closed, planar)
    //       → Normalize winding direction (counter-clockwise)
    //       → Create CurveLoop
    //       ↓
    //   Return List<CurveLoop>
    //
    // ============================================================================
    public static class CurveLoopBuilder
    {
        // ====================================================================
        // Public API
        // ====================================================================

        /// <summary>
        /// Converts an arbitrary collection of <see cref="Curve"/> objects into
        /// all valid closed <see cref="CurveLoop"/> instances that can be
        /// formed from the input.
        /// <para>
        /// Curves may arrive in any order and with inconsistent directions.
        /// Multiple independent closed loops are detected and returned.
        /// Open chains, isolated curves, and groups that do not form a valid
        /// closed planar loop are silently ignored.
        /// </para>
        /// </summary>
        /// <param name="curves">
        /// The input curves. The collection is not modified.
        /// </param>
        /// <returns>
        /// A list of valid, closed, planar <see cref="CurveLoop"/> instances.
        /// Each loop has consistent counter-clockwise winding.
        /// Returns an empty list if no valid loops can be formed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="curves"/> is <c>null</c>.
        /// </exception>
        public static List<CurveLoop> Build(IList<Curve> curves)
        {
            if (curves == null)
                throw new ArgumentNullException(nameof(curves));

            List<CurveLoop> result = new List<CurveLoop>();

            // Nothing to do with fewer than 3 curves — a closed loop
            // requires at least 3 segments (triangle).
            if (curves.Count < 3)
                return result;

            // ----------------------------------------------------------
            // Step 1: Copy the input so we never modify the caller's list.
            // ----------------------------------------------------------
            List<Curve> workingCurves = CopyInputCurves(curves);

            // ----------------------------------------------------------
            // Step 2: Split curves into connected components.
            //         Two curves are "connected" if an endpoint of one
            //         is coincident with an endpoint of the other.
            // ----------------------------------------------------------
            List<List<Curve>> components = ExtractConnectedComponents(workingCurves);

            // ----------------------------------------------------------
            // Step 3: Process each component independently.
            // ----------------------------------------------------------
            foreach (List<Curve> component in components)
            {
                // A closed loop needs at least 3 curves.
                if (component.Count < 3)
                    continue;

                // Try to order the curves into a continuous chain.
                List<Curve> ordered = OrderCurves(component);

                // OrderCurves returns null if no continuous chain exists.
                if (ordered == null)
                    continue;

                // Validate the ordered chain: closed, continuous, planar.
                if (!ValidateLoop(ordered))
                    continue;

                // Normalize winding direction to counter-clockwise.
                List<Curve> normalized = NormalizeLoopDirection(ordered);

                // Build the Revit CurveLoop object.
                CurveLoop loop = CreateCurveLoop(normalized);

                if (loop != null)
                    result.Add(loop);
            }

            return result;
        }

        // ====================================================================
        // Private Helpers
        // ====================================================================

        // --------------------------------------------------------------------
        // CopyInputCurves
        //
        // Creates a shallow copy of the input list so the original collection
        // is never modified. Null entries are excluded.
        // --------------------------------------------------------------------
        private static List<Curve> CopyInputCurves(IList<Curve> curves)
        {
            List<Curve> copy = new List<Curve>(curves.Count);

            foreach (Curve curve in curves)
            {
                if (curve != null)
                    copy.Add(curve);
            }

            return copy;
        }

        // ====================================================================
        // Connected Component Extraction
        // ====================================================================

        // --------------------------------------------------------------------
        // ExtractConnectedComponents
        //
        // Builds a connectivity graph where each curve is a node.
        // Two nodes share an edge if any endpoint of one curve is coincident
        // (within Revit tolerance) with any endpoint of the other curve.
        //
        // Uses BFS to discover all connected components.
        // --------------------------------------------------------------------
        private static List<List<Curve>> ExtractConnectedComponents(List<Curve> curves)
        {
            int count = curves.Count;

            // Adjacency list: for each curve index, store the indices
            // of all curves that share a coincident endpoint.
            List<List<int>> adjacency = BuildAdjacencyList(curves);

            // Track which curves have been assigned to a component.
            bool[] visited = new bool[count];

            List<List<Curve>> components = new List<List<Curve>>();

            // BFS from every unvisited node to discover components.
            for (int i = 0; i < count; i++)
            {
                if (visited[i])
                    continue;

                List<int> componentIndices = BreadthFirstTraversal(i, adjacency, visited);

                // Convert indices back to Curve objects.
                List<Curve> component = new List<Curve>(componentIndices.Count);

                foreach (int index in componentIndices)
                {
                    component.Add(curves[index]);
                }

                components.Add(component);
            }

            return components;
        }

        // --------------------------------------------------------------------
        // BuildAdjacencyList
        //
        // For each curve, extracts both endpoints. Two curves are adjacent
        // if any of the four endpoint pairs (start-start, start-end,
        // end-start, end-end) are coincident.
        // --------------------------------------------------------------------
        private static List<List<int>> BuildAdjacencyList(List<Curve> curves)
        {
            int count = curves.Count;

            // Pre-extract endpoints for efficiency — avoids redundant
            // GetEndPoint calls during pairwise comparison.
            XYZ[] starts = new XYZ[count];
            XYZ[] ends = new XYZ[count];

            for (int i = 0; i < count; i++)
            {
                starts[i] = curves[i].GetEndPoint(0);
                ends[i] = curves[i].GetEndPoint(1);
            }

            // Initialize empty adjacency lists.
            List<List<int>> adjacency = new List<List<int>>(count);

            for (int i = 0; i < count; i++)
            {
                adjacency.Add(new List<int>());
            }

            // Compare every pair of curves.
            // O(n²) is acceptable here because Revit models rarely have
            // thousands of loose curves passed to this helper.
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (AreEndpointsConnected(starts[i], ends[i], starts[j], ends[j]))
                    {
                        adjacency[i].Add(j);
                        adjacency[j].Add(i);
                    }
                }
            }

            return adjacency;
        }

        // --------------------------------------------------------------------
        // AreEndpointsConnected
        //
        // Returns true if any endpoint of curve A is coincident with
        // any endpoint of curve B.
        //
        // Uses Revit's built-in IsAlmostEqualTo which respects the
        // application's tolerance settings.
        // --------------------------------------------------------------------
        private static bool AreEndpointsConnected(
            XYZ startA, XYZ endA,
            XYZ startB, XYZ endB)
        {
            // Four possible connections:
            //   A.start == B.start
            //   A.start == B.end
            //   A.end   == B.start
            //   A.end   == B.end
            return startA.IsAlmostEqualTo(startB)
                || startA.IsAlmostEqualTo(endB)
                || endA.IsAlmostEqualTo(startB)
                || endA.IsAlmostEqualTo(endB);
        }

        // --------------------------------------------------------------------
        // BreadthFirstTraversal
        //
        // Standard BFS starting from a given node. Returns all node indices
        // reachable from the start node. Marks visited nodes in the shared
        // visited array.
        // --------------------------------------------------------------------
        private static List<int> BreadthFirstTraversal(
            int startIndex,
            List<List<int>> adjacency,
            bool[] visited)
        {
            List<int> component = new List<int>();
            Queue<int> queue = new Queue<int>();

            visited[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                component.Add(current);

                foreach (int neighbor in adjacency[current])
                {
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return component;
        }

        // ====================================================================
        // Curve Ordering
        // ====================================================================

        // --------------------------------------------------------------------
        // OrderCurves
        //
        // Given a set of curves that belong to the same connected component,
        // attempts to arrange them into a single continuous chain where the
        // end of each curve meets the start of the next.
        //
        // Reverses individual curves as needed so that:
        //   curve[i].GetEndPoint(1) == curve[i+1].GetEndPoint(0)
        //
        // Returns null if a continuous chain cannot be formed (e.g., the
        // component has branches / T-intersections).
        // --------------------------------------------------------------------
        private static List<Curve> OrderCurves(List<Curve> curves)
        {
            // Work on a copy so we can consume curves as we place them.
            List<Curve> remaining = new List<Curve>(curves);

            List<Curve> ordered = new List<Curve>(curves.Count);

            // Start with the first curve (arbitrary choice).
            Curve first = remaining[0];
            remaining.RemoveAt(0);
            ordered.Add(first);

            // Iteratively find the next curve whose start or end
            // matches the current chain's tail endpoint.
            while (remaining.Count > 0)
            {
                XYZ chainEnd = ordered[ordered.Count - 1].GetEndPoint(1);

                Curve next = FindAndRemoveNextCurve(remaining, chainEnd);

                // If no connecting curve was found, the chain is broken.
                // This component cannot form a single continuous loop.
                if (next == null)
                    return null;

                ordered.Add(next);
            }

            return ordered;
        }

        // --------------------------------------------------------------------
        // FindAndRemoveNextCurve
        //
        // Searches the remaining curves for one whose start or end point
        // matches the given connection point. If a match is found on the
        // end point (meaning the curve is oriented backwards), the curve
        // is reversed before returning.
        //
        // The matched curve is removed from the remaining list.
        // Returns null if no match is found.
        // --------------------------------------------------------------------
        private static Curve FindAndRemoveNextCurve(
            List<Curve> remaining,
            XYZ connectionPoint)
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                Curve candidate = remaining[i];

                Curve oriented = ReverseCurveIfNeeded(candidate, connectionPoint);

                if (oriented != null)
                {
                    remaining.RemoveAt(i);
                    return oriented;
                }
            }

            return null;
        }

        // --------------------------------------------------------------------
        // ReverseCurveIfNeeded
        //
        // Checks whether the curve's start point matches the connection
        // point. If so, returns the curve as-is. If the end point matches
        // instead, returns a reversed copy. If neither matches, returns null.
        //
        // This ensures the returned curve starts at the connection point.
        // --------------------------------------------------------------------
        private static Curve ReverseCurveIfNeeded(Curve curve, XYZ connectionPoint)
        {
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);

            // Curve already oriented correctly — start matches connection.
            if (start.IsAlmostEqualTo(connectionPoint))
                return curve;

            // Curve is backwards — end matches connection, so reverse it.
            if (end.IsAlmostEqualTo(connectionPoint))
                return curve.CreateReversed();

            // Neither endpoint matches — not a neighbor.
            return null;
        }

        // ====================================================================
        // Validation
        // ====================================================================

        // --------------------------------------------------------------------
        // ValidateLoop
        //
        // Performs four checks on the ordered curve chain:
        //
        //   1. Minimum curve count (≥ 3 for a valid polygon).
        //   2. Continuity — each curve's end matches the next curve's start.
        //   3. Closure — the last curve's end matches the first curve's start.
        //   4. Planarity — all curves lie on a single plane.
        //
        // Returns true only if all checks pass. Invalid geometry is silently
        // skipped — no exceptions are thrown.
        // --------------------------------------------------------------------
        private static bool ValidateLoop(List<Curve> orderedCurves)
        {
            // Check 1: Minimum curve count.
            if (orderedCurves.Count < 3)
                return false;

            // Check 2: Continuity — each end meets the next start.
            if (!IsContinuous(orderedCurves))
                return false;

            // Check 3: Closure — last end meets first start.
            if (!IsClosed(orderedCurves))
                return false;

            // Check 4: Planarity — all curve endpoints lie on one plane.
            if (!IsPlanar(orderedCurves))
                return false;

            return true;
        }

        // --------------------------------------------------------------------
        // IsContinuous
        //
        // Verifies that for every adjacent pair of curves, the end point
        // of curve[i] is coincident with the start point of curve[i+1].
        // --------------------------------------------------------------------
        private static bool IsContinuous(List<Curve> curves)
        {
            for (int i = 0; i < curves.Count - 1; i++)
            {
                XYZ currentEnd = curves[i].GetEndPoint(1);
                XYZ nextStart = curves[i + 1].GetEndPoint(0);

                if (!currentEnd.IsAlmostEqualTo(nextStart))
                    return false;
            }

            return true;
        }

        // --------------------------------------------------------------------
        // IsClosed
        //
        // Verifies that the chain forms a closed loop — the end of the last
        // curve must be coincident with the start of the first curve.
        // --------------------------------------------------------------------
        private static bool IsClosed(List<Curve> curves)
        {
            XYZ lastEnd = curves[curves.Count - 1].GetEndPoint(1);
            XYZ firstStart = curves[0].GetEndPoint(0);

            return lastEnd.IsAlmostEqualTo(firstStart);
        }

        // --------------------------------------------------------------------
        // IsPlanar
        //
        // Checks planarity by collecting distinct vertices from the loop
        // and verifying they all lie on the same plane.
        //
        // Strategy:
        //   - Collect all unique start points (in a closed loop, each
        //     curve's start is the previous curve's end).
        //   - If we have 3 or fewer distinct points, they are trivially
        //     coplanar.
        //   - Otherwise, define a plane from the first 3 non-collinear
        //     points and verify all remaining points lie on that plane.
        // --------------------------------------------------------------------
        private static bool IsPlanar(List<Curve> curves)
        {
            // Collect unique vertices from the loop.
            List<XYZ> vertices = CollectUniqueVertices(curves);

            // Three or fewer distinct points are always coplanar.
            if (vertices.Count <= 3)
                return true;

            // Find three non-collinear points to define the plane.
            XYZ normal = ComputePlaneNormal(vertices);

            // If we could not find a valid normal, all points are
            // collinear — this is degenerate, not a valid loop.
            if (normal == null)
                return false;

            // The plane passes through the first vertex with the
            // computed normal. Check every other vertex against it.
            XYZ origin = vertices[0];

            for (int i = 3; i < vertices.Count; i++)
            {
                XYZ toPoint = vertices[i] - origin;
                double distance = Math.Abs(toPoint.DotProduct(normal));

                // Tolerance: Revit's internal precision is ~1e-9,
                // but we use a slightly larger tolerance to account
                // for floating-point drift across many operations.
                if (distance > 1.0e-6)
                    return false;
            }

            return true;
        }

        // --------------------------------------------------------------------
        // CollectUniqueVertices
        //
        // Extracts the start point of each curve in the ordered chain.
        // In a properly closed loop, the start points form the complete
        // set of vertices (each curve's end is the next curve's start).
        // Duplicate points are filtered out.
        // --------------------------------------------------------------------
        private static List<XYZ> CollectUniqueVertices(List<Curve> curves)
        {
            List<XYZ> vertices = new List<XYZ>(curves.Count);

            foreach (Curve curve in curves)
            {
                XYZ point = curve.GetEndPoint(0);

                // Only add if not already present (handles degenerate
                // cases where two curves share the same start point).
                bool isDuplicate = false;

                foreach (XYZ existing in vertices)
                {
                    if (existing.IsAlmostEqualTo(point))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                    vertices.Add(point);
            }

            return vertices;
        }

        // --------------------------------------------------------------------
        // ComputePlaneNormal
        //
        // Finds three non-collinear points among the vertices and computes
        // the unit normal of the plane they define.
        //
        // Returns null if all points are collinear (degenerate).
        // --------------------------------------------------------------------
        private static XYZ ComputePlaneNormal(List<XYZ> vertices)
        {
            XYZ p0 = vertices[0];

            // Try each pair of subsequent vertices until we find a
            // non-zero cross product (i.e., non-collinear triple).
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    XYZ v1 = vertices[i] - p0;
                    XYZ v2 = vertices[j] - p0;
                    XYZ cross = v1.CrossProduct(v2);

                    double length = cross.GetLength();

                    if (length > 1.0e-9)
                    {
                        // Normalize and return.
                        return cross.Normalize();
                    }
                }
            }

            // All points are collinear — no valid plane.
            return null;
        }

        // ====================================================================
        // Winding Direction Normalization
        // ====================================================================

        // --------------------------------------------------------------------
        // NormalizeLoopDirection
        //
        // Ensures the loop has a consistent counter-clockwise winding
        // when viewed from the direction of its outward-facing normal.
        //
        // Uses the Shoelace formula (signed area) projected onto the
        // loop's own plane to determine the current winding direction,
        // then reverses if necessary.
        //
        // Counter-clockwise is the standard Revit convention for outer
        // boundary loops (floors, ceilings, roofs, etc.).
        // --------------------------------------------------------------------
        private static List<Curve> NormalizeLoopDirection(List<Curve> orderedCurves)
        {
            double signedArea = ComputeSignedArea(orderedCurves);

            // Positive signed area = counter-clockwise (our target).
            // Negative signed area = clockwise (needs reversal).
            if (signedArea >= 0)
            {
                // Already counter-clockwise — return a copy.
                return new List<Curve>(orderedCurves);
            }

            // Reverse the loop: reverse the list order, then reverse
            // each individual curve's direction.
            return ReverseLoop(orderedCurves);
        }

        // --------------------------------------------------------------------
        // ComputeSignedArea
        //
        // Computes the signed area of the polygon formed by the curve
        // endpoints, projected onto the loop's dominant plane.
        //
        // For a general 3D planar loop, we:
        //   1. Compute the loop's plane normal.
        //   2. Choose the axis-aligned plane that best captures the
        //      loop's shape (the one most perpendicular to the normal).
        //   3. Compute the 2D signed area using the Shoelace formula
        //      projected onto that plane.
        //
        // The sign of the result indicates winding direction relative
        // to the plane normal.
        // --------------------------------------------------------------------
        private static double ComputeSignedArea(List<Curve> curves)
        {
            // Collect vertices (start points of each curve).
            List<XYZ> vertices = new List<XYZ>(curves.Count);

            foreach (Curve curve in curves)
            {
                vertices.Add(curve.GetEndPoint(0));
            }

            // Determine the loop's plane normal.
            XYZ normal = ComputePlaneNormal(vertices);

            // Fallback: if we cannot determine a normal (degenerate),
            // assume counter-clockwise to avoid unnecessary reversal.
            if (normal == null)
                return 1.0;

            // Choose projection axes based on the dominant normal component.
            // We project onto the 2D plane that is most perpendicular to the
            // normal, giving the least-distorted area measurement.
            int axisU;
            int axisV;
            ChooseProjectionAxes(normal, out axisU, out axisV);

            // Determine sign convention: if our chosen projection plane's
            // natural normal is opposite to the loop normal, we need to
            // flip the sign of the area calculation.
            double signFlip = GetProjectionSign(normal, axisU, axisV);

            // Shoelace formula in 2D.
            double area = 0.0;

            for (int i = 0; i < vertices.Count; i++)
            {
                XYZ current = vertices[i];
                XYZ next = vertices[(i + 1) % vertices.Count];

                double u0 = GetCoordinate(current, axisU);
                double v0 = GetCoordinate(current, axisV);
                double u1 = GetCoordinate(next, axisU);
                double v1 = GetCoordinate(next, axisV);

                area += (u0 * v1) - (u1 * v0);
            }

            // The Shoelace formula gives 2× the signed area.
            return area * 0.5 * signFlip;
        }

        // --------------------------------------------------------------------
        // ChooseProjectionAxes
        //
        // Selects the two coordinate axes (0=X, 1=Y, 2=Z) onto which the
        // polygon should be projected for the signed area calculation.
        //
        // We drop the axis most aligned with the normal, since projecting
        // along that axis gives the truest 2D representation of the loop.
        // --------------------------------------------------------------------
        private static void ChooseProjectionAxes(XYZ normal, out int axisU, out int axisV)
        {
            double absX = Math.Abs(normal.X);
            double absY = Math.Abs(normal.Y);
            double absZ = Math.Abs(normal.Z);

            if (absZ >= absX && absZ >= absY)
            {
                // Normal is mostly along Z — project onto XY plane.
                axisU = 0; // X
                axisV = 1; // Y
            }
            else if (absY >= absX && absY >= absZ)
            {
                // Normal is mostly along Y — project onto XZ plane.
                axisU = 0; // X
                axisV = 2; // Z
            }
            else
            {
                // Normal is mostly along X — project onto YZ plane.
                axisU = 1; // Y
                axisV = 2; // Z
            }
        }

        // --------------------------------------------------------------------
        // GetProjectionSign
        //
        // Determines whether the projection plane's natural normal is
        // aligned or anti-aligned with the loop's actual normal.
        //
        // This ensures the signed area correctly reflects the winding
        // direction relative to the loop's own normal, not the projection
        // plane's arbitrary normal.
        // --------------------------------------------------------------------
        private static double GetProjectionSign(XYZ normal, int axisU, int axisV)
        {
            // The "natural" normal of the projection plane is along the
            // dropped axis. Its sign determines whether our 2D Shoelace
            // result matches the 3D winding direction.
            //
            //   Dropped axis Z → natural normal is +Z → sign = normal.Z
            //   Dropped axis Y → natural normal is +Y → sign = normal.Y
            //   Dropped axis X → natural normal is +X → sign = normal.X

            int droppedAxis = 3 - axisU - axisV; // 0+1+2=3, so dropped = 3 - U - V

            double component = GetCoordinate(normal, droppedAxis);

            return component >= 0 ? 1.0 : -1.0;
        }

        // --------------------------------------------------------------------
        // GetCoordinate
        //
        // Returns the X, Y, or Z component of a point by axis index.
        //   0 = X,  1 = Y,  2 = Z
        // --------------------------------------------------------------------
        private static double GetCoordinate(XYZ point, int axis)
        {
            switch (axis)
            {
                case 0: return point.X;
                case 1: return point.Y;
                case 2: return point.Z;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(axis),
                        "Axis index must be 0 (X), 1 (Y), or 2 (Z).");
            }
        }

        // --------------------------------------------------------------------
        // ReverseLoop
        //
        // Creates a reversed copy of the curve chain. Both the list order
        // and each individual curve direction are reversed, producing a
        // chain with the opposite winding direction.
        //
        // The original list is not modified.
        // --------------------------------------------------------------------
        private static List<Curve> ReverseLoop(List<Curve> curves)
        {
            List<Curve> reversed = new List<Curve>(curves.Count);

            // Walk backwards through the list.
            for (int i = curves.Count - 1; i >= 0; i--)
            {
                reversed.Add(curves[i].CreateReversed());
            }

            return reversed;
        }

        // ====================================================================
        // CurveLoop Creation
        // ====================================================================

        // --------------------------------------------------------------------
        // CreateCurveLoop
        //
        // Assembles a Revit CurveLoop from the ordered, validated, and
        // direction-normalized curve list.
        //
        // Returns null if CurveLoop construction throws (defensive guard
        // against unexpected Revit API edge cases).
        // --------------------------------------------------------------------
        private static CurveLoop CreateCurveLoop(List<Curve> curves)
        {
            try
            {
                CurveLoop loop = new CurveLoop();

                foreach (Curve curve in curves)
                {
                    loop.Append(curve);
                }

                return loop;
            }
            catch
            {
                // If Revit rejects the loop for any reason we did not
                // anticipate, skip it gracefully rather than crashing.
                return null;
            }
        }
    }
}

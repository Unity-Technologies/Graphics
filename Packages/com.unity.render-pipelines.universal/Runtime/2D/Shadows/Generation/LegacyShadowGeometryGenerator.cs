using Unity.Collections;
using static UnityEngine.Rendering.Universal.ShadowUtility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The original 2D shadow geometry algorithm, lifted verbatim out of <see cref="ShadowMesh2D"/>.
    /// </summary>
    /// <remarks>
    /// This is the behaviour of record: every reference image in the 2D graphics test suite was
    /// captured against it. Treat it as frozen. Quirks below are load bearing and are called out
    /// individually — fix them in a new path, not here.
    /// </remarks>
    [System.Serializable]
    class LegacyShadowGeometryGenerator : ShadowGeometryGenerator
    {
        internal const string k_Id = "Unity.Legacy";

        internal override string id => k_Id;

        // Bump only if this path's output changes -- it is frozen, so it should stay at 1.
        internal override int version => 1;

        internal override string displayName => "Legacy";

        // Matches the Attributes struct in Shaders/2D/Include/ShadowProjectVertex.hlsl:
        // POSITION / TANGENT / TEXCOORD0. This is the layout UploadMesh actually uses, and the
        // one a shader compatibility check would validate against. Cached: it is read per upload.
        static VertexAttributeDescriptor[] s_VertexLayout = CreateVertexLayout();

        static VertexAttributeDescriptor[] CreateVertexLayout()
        {
            return new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position,   VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Tangent,    VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0,  VertexAttributeFormat.Float32, 2),
            };
        }

#if UNITY_EDITOR
        // Generator instances are static and survive "Enter Play Mode without Domain Reload",
        // so rebuild the cached array rather than trusting the field initializer to re-run.
        [RuntimeInitializeOnLoadMethod]
        static void ResetStaticsOnLoad()
        {
            s_VertexLayout = CreateVertexLayout();
        }
#endif

        internal override VertexAttributeDescriptor[] vertexLayout => s_VertexLayout;

        internal override void Build(ref ShadowShapeInput input, ref ShadowGeometryResult result)
        {
            switch (input.kind)
            {
                case ShadowShapeKind.RadiiWithTransform:
                    BuildFromRadii(ref input, ref result, applyOutputTransform: true);
                    break;
                case ShadowShapeKind.Radii:
                    BuildFromRadii(ref input, ref result, applyOutputTransform: false);
                    break;
                case ShadowShapeKind.Topology:
                    BuildFromTopology(ref input, ref result);
                    break;
                case ShadowShapeKind.Direct:
                    BuildDirect(ref input, ref result);
                    break;
            }
        }

        internal override void UploadMesh(ref Mesh mesh, in ShadowGeometryResult result)
        {
            ShadowUtility.GenerateShadowMesh(ref mesh, result.vertices, result.indices, vertexLayout, result.localBounds);
        }

        // ------------------------------------------------------------------------------------
        // Radii entry points (ShadowShape2D.SetShape overloads taking a radii array)
        // ------------------------------------------------------------------------------------

        void BuildFromRadii(ref ShadowShapeInput input, ref ShadowGeometryResult result, bool applyOutputTransform)
        {
            // The transform overload always claims transformability; the other one honours
            // inWorldSpace. Not obviously intentional, but it is what shipped.
            result.isTransformable = applyOutputTransform || !input.inWorldSpace;

            bool reverseWindingOrder = input.windingOrder == ShadowShape2D.WindingOrder.CounterClockwise;

            if (result.trimEdge == ShadowMesh2D.k_TrimEdgeUninitialized)
                result.trimEdge = input.initialTrim;

            NativeArray<Vector3> generatedVertices;
            NativeArray<int> generatedIndices;

            if (input.indices.Length == 0)
            {
                result.cleared = true;
                generatedIndices = new NativeArray<int>();
                generatedVertices = new NativeArray<Vector3>();
            }
            else
            {
                ShadowShapeExpansion.GenerateShapeWithRadii(input.vertices, input.indices, input.radii, reverseWindingOrder, out generatedVertices, out generatedIndices);
            }

            if (applyOutputTransform)
            {
                for (int i = 0; i < generatedVertices.Length; i++)
                    generatedVertices[i] = input.outputTransform.MultiplyPoint(generatedVertices[i]);
            }

            ProcessShapeWithRadii(ref input, ref result, generatedVertices, generatedIndices, reverseWindingOrder);

            generatedVertices.Dispose();
            generatedIndices.Dispose();
        }

        void ProcessShapeWithRadii(ref ShadowShapeInput input, ref ShadowGeometryResult result, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder)
        {
            ShadowUtility.CalculateEdgesFromLines(ref generatedIndices, out var calculatedEdges, out var calculatedStartingEdges, out var calculatedIsClosedArray);

            if (reverseWindingOrder)
                ShadowUtility.ReverseWindingOrder(ref calculatedStartingEdges, ref calculatedEdges);

            // Clipping is unconditional now that EdgeProcessing.None has been removed. The old
            // alternative skipped ClipEdges here and contracted in the vertex shader instead.
            ShadowUtility.ClipEdges(ref generatedVertices, ref calculatedEdges, ref calculatedStartingEdges, ref calculatedIsClosedArray, result.trimEdge, out var clippedVertices, out var clippedEdges, out var clippedStartingIndices);

            if (clippedStartingIndices.Length > 0)
            {
                result.localBounds = ShadowUtility.GenerateShadowGeometry(ref result.vertices, ref result.indices, clippedVertices, clippedEdges, clippedStartingIndices, input.createInteriorGeometry, ShadowShape2D.OutlineTopology.Lines);
                result.localBoundsAssigned = true;
                result.markDirty = true;
            }
            else
            {
                // Clipped away to nothing: drop the geometry rather than leaving the previous
                // mesh rendering. Clear() marks the mesh dirty, so markDirty is not set here.
                result.localBounds = new Bounds();
                result.localBoundsAssigned = true;
                result.cleared = true;
            }

            clippedVertices.Dispose();
            clippedEdges.Dispose();
            clippedStartingIndices.Dispose();

            calculatedEdges.Dispose();
            calculatedIsClosedArray.Dispose();
            calculatedStartingEdges.Dispose();
        }

        // ------------------------------------------------------------------------------------
        // Outline entry point (ShadowShape2D.SetShape taking an OutlineTopology)
        // ------------------------------------------------------------------------------------

        static bool AreDegenerateVertices(NativeArray<Vector3> vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return true;

            // This should is a trade off between perfomance and accuracy. This may need to be refined later if we find cases where this is not good enough.
            int prevIndex = vertices.Length - 1;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[prevIndex].x != vertices[i].x || vertices[prevIndex].y != vertices[i].y)
                    return false;

                prevIndex = i;
            }

            return true;
        }

        void BuildFromTopology(ref ShadowShapeInput input, ref ShadowGeometryResult result)
        {
            result.isTransformable = !input.inWorldSpace;

            if (AreDegenerateVertices(input.vertices) || input.indices == null || input.indices.Length == 0)
            {
                result.cleared = true;
                return;
            }

            if (result.trimEdge == ShadowMesh2D.k_TrimEdgeUninitialized)
                result.trimEdge = input.initialTrim;

            var vertices = input.vertices;
            bool disposeVertices = false;
            NativeArray<ShadowEdge> edges;
            NativeArray<int> shapeStartingIndices;
            NativeArray<bool> shapeIsClosedArray;

            if (input.topology == ShadowShape2D.OutlineTopology.Triangles)
            {
                ShadowUtility.CalculateEdgesFromTriangles(ref vertices, ref input.indices, true, out var newVertices, out edges, out shapeStartingIndices, out shapeIsClosedArray);

                disposeVertices = true;
                vertices = newVertices;
            }
            else // if (outlineTopology == ShadowShape2D.OutlineTopology.Lines)
            {
                ShadowUtility.CalculateEdgesFromLines(ref input.indices, out edges, out shapeStartingIndices, out shapeIsClosedArray);
            }

            if (input.windingOrder == ShadowShape2D.WindingOrder.CounterClockwise)
                ShadowUtility.ReverseWindingOrder(ref shapeStartingIndices, ref edges);

            // It would be better if we don't have to rerun SetShape after a trimEdge change.
            if (input.allowTrimming)
            {
                ShadowUtility.ClipEdges(ref vertices, ref edges, ref shapeStartingIndices, ref shapeIsClosedArray, result.trimEdge, out var clippedVertices, out var clippedEdges, out var clippedStartingIndices);

                // Note there is no empty check here, unlike the radii path above.
                result.localBounds = ShadowUtility.GenerateShadowGeometry(ref result.vertices, ref result.indices, clippedVertices, clippedEdges, clippedStartingIndices, input.createInteriorGeometry, input.topology);
                result.localBoundsAssigned = true;
                result.markDirty = true;

                clippedVertices.Dispose();
                clippedEdges.Dispose();
                clippedStartingIndices.Dispose();
            }
            else
            {
                result.localBounds = ShadowUtility.GenerateShadowGeometry(ref result.vertices, ref result.indices, vertices, edges, shapeStartingIndices, input.createInteriorGeometry, input.topology);
                result.localBoundsAssigned = true;
                result.markDirty = true;
            }

            result.uploadMesh = true;

            if (disposeVertices)
                vertices.Dispose();

            edges.Dispose();
            shapeStartingIndices.Dispose();
            shapeIsClosedArray.Dispose();
        }

        // ------------------------------------------------------------------------------------
        // Direct entry point (ShadowShape2D.SetShapeDirect)
        // ------------------------------------------------------------------------------------

        void BuildDirect(ref ShadowShapeInput input, ref ShadowGeometryResult result)
        {
            result.isTransformable = !input.inWorldSpace;

            NativeArray<ShadowEdge> edges = input.indices.Reinterpret<ShadowEdge>(sizeof(int));

            // Create a shape starting indices array indicating a single shape starting at edge 0
            NativeArray<int> shapeStartingIndices = new NativeArray<int>(1, Allocator.Temp);
            shapeStartingIndices[0] = 0;

            result.localBounds = ShadowUtility.GenerateShadowGeometry(ref result.vertices, ref result.indices, input.vertices, edges, shapeStartingIndices, false, ShadowShape2D.OutlineTopology.Lines);
            result.localBoundsAssigned = true;
            result.markDirty = true;
            result.uploadMesh = true;

            shapeStartingIndices.Dispose();
        }

#if UNITY_EDITOR
        internal override void DrawPreviewOutline(Mesh mesh, Matrix4x4 previewMat, float trimionDistance)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector4[] tangents = mesh.tangents;

            Handles.color = Color.white;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                Vector3 pt0 = vertices[v0];
                Vector3 pt1 = vertices[v1];
                Vector3 pt2 = vertices[v2];

                Vector4 tan0 = tangents[v0];
                Vector4 tan1 = tangents[v1];
                Vector4 tan2 = tangents[v2];

                Vector3 trimPt0 = new Vector3(pt0.x + trimionDistance * tan0.x, pt0.y + trimionDistance * tan0.y, 0);
                Vector3 trimPt1 = new Vector3(pt1.x + trimionDistance * tan1.x, pt1.y + trimionDistance * tan1.y, 0);
                Vector3 trimPt2 = new Vector3(pt2.x + trimionDistance * tan2.x, pt2.y + trimionDistance * tan2.y, 0);

                trimPt0 = previewMat.MultiplyPoint(trimPt0);
                trimPt1 = previewMat.MultiplyPoint(trimPt1);
                trimPt2 = previewMat.MultiplyPoint(trimPt2);

                if (pt0.z == 0 && pt1.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt0, trimPt1 });
                if (pt1.z == 0 && pt2.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt1, trimPt2 });
                if (pt2.z == 0 && pt0.z == 0)
                    Handles.DrawAAPolyLine(4, new Vector3[] { trimPt2, trimPt0 });
            }
        }
#endif
    }
}

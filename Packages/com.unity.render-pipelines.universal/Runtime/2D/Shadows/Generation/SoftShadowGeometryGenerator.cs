using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.Universal.ShadowUtility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Soft shadow geometry: hard body quads plus a trailing penumbra band, a fin at every
    /// silhouette pole, and fans that round the convex corners, the concave corners and the
    /// fin-to-band joins.
    /// </summary>
    /// <remarks>
    /// The mesh is light-independent. Every vertex carries a 13-float payload describing its own
    /// caster-space position, its four outline neighbours, its role and its fan parameter; the vertex
    /// program derives the projection, the facing tests, the silhouette-pole test, the corner
    /// classification, the radius budget and the fillet from those alone. Roles that do not apply for
    /// the current light collapse onto a degenerate position, so their triangles have zero area — at
    /// the default segment count roughly two thirds of vertices collapse on a typical caster.
    ///
    /// The design and its verification live in
    /// <c>docs/shadow-fins-rework/simulation-reports/</c>: report 24 (handoff v2) is the spec,
    /// reports 27 to 29 correct it. Where they disagree, <c>sim/sim_shader.py</c> wins — it is the
    /// reference this was transliterated from, and
    /// <c>SoftShadowVertexReference</c> / <c>softshadow_goldens.txt</c> in the
    /// UniversalGraphicsTest_2D project pin the agreement.
    ///
    /// Not Burst-compiled yet, deliberately: correctness first. Everything is <c>NativeArray</c>
    /// so the path is already GC-free, and the kernels are shaped to move into a
    /// <c>[BurstCompile]</c> type without restructuring.
    /// </remarks>
    [System.Serializable]
    class SoftShadowGeometryGenerator : ShadowGeometryGenerator
    {
        internal const string k_Id = "Unity.SoftShadow";

        internal override string id => k_Id;

        internal override int version => 1;

        internal override string displayName => "Soft Shadow";

        // The payload is large relative to the outline it comes from -- 52 bytes across 11 + 4*S
        // vertices per outline vertex -- and it would be stored as YAML in every scene and prefab
        // holding a caster. Rebuilding on load is cheaper than carrying that, and handoff v2 section
        // 12 asks for a regeneration-on-load path regardless.
        internal override bool persistsGeometry => false;

        internal override bool usesFanSegments => true;

        // Vertex roles. Stored as a float in tangent.x, so these are the values the shader compares
        // against and the integers the golden file records. Interior fill reuses Outline: it wants
        // exactly what Outline produces -- the unprojected position, fully opaque.
        internal const int k_RoleOutline = 0;
        internal const int k_RoleHard = 1;
        internal const int k_RoleBandIn = 2;
        internal const int k_RoleBandOut = 3;
        internal const int k_RoleFinL = 4;
        internal const int k_RoleFinR = 5;
        internal const int k_RoleJoinIn = 6;
        internal const int k_RoleJoinOut = 7;
        internal const int k_RoleConvex = 8;
        internal const int k_RoleConcave = 9;
        internal const int k_RoleHardOpq = 10;

        // Roles that exist exactly once per outline vertex, before the four fans.
        const int k_FixedRolesPerVertex = 7;

        internal const int k_FloatsPerVertex = 14;

        /// <summary>
        /// The per-vertex payload. Field order is the vertex layout's order and the two must agree —
        /// <c>SetVertexBufferData</c> takes its element size from <c>sizeof(T)</c> and knows nothing
        /// about the declared attributes, so a mismatch throws at upload.
        /// </summary>
        /// <remarks>
        /// Handoff v2 section 9 specifies 13 floats and notes that <c>windingSign</c> is constant per
        /// caster and "could move to a uniform, saving a float". It is kept per-vertex here anyway,
        /// for a reason the spec does not raise: winding is derived from a ring's signed area, and a
        /// caster may carry several rings. One uniform would be wrong for any ring whose winding
        /// differs — and an inverted winding inverts the concave/convex classification, which
        /// shortens bands at convex corners and leaves holes (handoff v2 section 6). Outlines with
        /// holes are out of scope today, so a uniform would probably survive; it would fail silently
        /// when that changed. Four bytes is a cheap price for a self-contained payload.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SoftShadowMeshVertex
        {
            public float3 position;    // v.xy, 0        -- z stays 0; POSITION-only consumers read it
            public float4 tangent;     // role, fanParam, prev.xy
            public float3 next;        // next.xy, windingSign
            public float4 neighbor2;   // prev2.xy, next2.xy
        }

        static VertexAttributeDescriptor[] s_VertexLayout = CreateVertexLayout();

        static VertexAttributeDescriptor[] CreateVertexLayout()
        {
            // One interleaved stream, 52 bytes. TEXCOORD0 and COLOR are deliberately left free:
            // Shadow2DCasterVertex.hlsl claims both under SHADOW_SPRITE_CASTER, and the caster passes
            // are shared verbatim with this generator's shader. Every semantic here is one a
            // ShaderGraph SubTarget can request, which matters for the eventual master node.
            return new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position,  VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Tangent,   VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
            };
        }

#if UNITY_EDITOR
        // Generator instances are static and survive "Enter Play Mode without Domain Reload", so
        // rebuild rather than trusting the field initializer to re-run.
        [RuntimeInitializeOnLoadMethod]
        static void ResetStaticsOnLoad()
        {
            s_VertexLayout = CreateVertexLayout();
        }
#endif

        internal override VertexAttributeDescriptor[] vertexLayout => s_VertexLayout;

        // ------------------------------------------------------------------------------------
        // Payload slot layout
        //
        // Deterministic, so an index is arithmetic rather than a dictionary lookup. The ORDER differs
        // from sim_shader.build_mesh's -- nothing depends on vertex order, and the golden test keys on
        // (vertexIndex, role, k) rather than position in the buffer.
        // ------------------------------------------------------------------------------------

        static int VerticesPerOutlineVertex(int segments) => k_FixedRolesPerVertex + 4 * (segments + 1);

        static int TrianglesPerOutlineVertex(int segments) => 5 + 4 * segments;

        static int SlotOf(int role, int k, int segments)
        {
            switch (role)
            {
                case k_RoleOutline:  return 0;
                case k_RoleHard:     return 1;
                case k_RoleHardOpq:  return 2;
                case k_RoleBandIn:   return 3;
                case k_RoleBandOut:  return 4;
                case k_RoleFinL:     return 5;
                case k_RoleFinR:     return 6;
                case k_RoleJoinIn:   return 7 + k;
                case k_RoleJoinOut:  return 8 + segments + k;
                case k_RoleConvex:   return 9 + 2 * segments + k;
                case k_RoleConcave:  return 10 + 3 * segments + k;
                default:             return 0;
            }
        }

        // ------------------------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------------------------

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

        void BuildFromRadii(ref ShadowShapeInput input, ref ShadowGeometryResult result,
                            bool applyOutputTransform)
        {
            result.isTransformable = applyOutputTransform || !input.inWorldSpace;

            bool reverseWindingOrder = input.windingOrder == ShadowShape2D.WindingOrder.CounterClockwise;

            if (result.trimEdge == ShadowMesh2D.k_TrimEdgeUninitialized)
                result.trimEdge = input.initialTrim;

            if (input.indices.Length == 0)
            {
                result.cleared = true;
                return;
            }

            ShadowShapeExpansion.GenerateShapeWithRadii(
                input.vertices, input.indices, input.radii, reverseWindingOrder,
                out NativeArray<Vector3> generatedVertices, out NativeArray<int> generatedIndices);

            if (applyOutputTransform)
            {
                for (int i = 0; i < generatedVertices.Length; i++)
                    generatedVertices[i] = input.outputTransform.MultiplyPoint(generatedVertices[i]);
            }

            ShadowUtility.CalculateEdgesFromLines(ref generatedIndices, out var edges,
                                                  out var shapeStarts, out var isClosed);

            if (reverseWindingOrder)
                ShadowUtility.ReverseWindingOrder(ref shapeStarts, ref edges);

            ShadowUtility.ClipEdges(ref generatedVertices, ref edges, ref shapeStarts, ref isClosed,
                                    result.trimEdge, out var clippedVertices, out var clippedEdges,
                                    out var clippedStarts);

            EmitOrClear(ref input, ref result, clippedVertices, clippedEdges, clippedStarts);

            clippedVertices.Dispose();
            clippedEdges.Dispose();
            clippedStarts.Dispose();
            edges.Dispose();
            shapeStarts.Dispose();
            isClosed.Dispose();
            generatedVertices.Dispose();
            generatedIndices.Dispose();
        }

        void BuildFromTopology(ref ShadowShapeInput input, ref ShadowGeometryResult result)
        {
            result.isTransformable = !input.inWorldSpace;

            if (input.vertices.Length == 0 || !input.indices.IsCreated || input.indices.Length == 0)
            {
                result.cleared = true;
                return;
            }

            if (result.trimEdge == ShadowMesh2D.k_TrimEdgeUninitialized)
                result.trimEdge = input.initialTrim;

            NativeArray<Vector3> vertices = input.vertices;
            bool disposeVertices = false;
            NativeArray<ShadowEdge> edges;
            NativeArray<int> shapeStarts;
            NativeArray<bool> isClosed;

            if (input.topology == ShadowShape2D.OutlineTopology.Triangles)
            {
                ShadowUtility.CalculateEdgesFromTriangles(ref vertices, ref input.indices, true,
                                                          out var newVertices, out edges,
                                                          out shapeStarts, out isClosed);
                vertices = newVertices;
                disposeVertices = true;
            }
            else
            {
                ShadowUtility.CalculateEdgesFromLines(ref input.indices, out edges, out shapeStarts,
                                                      out isClosed);
            }

            if (input.windingOrder == ShadowShape2D.WindingOrder.CounterClockwise)
                ShadowUtility.ReverseWindingOrder(ref shapeStarts, ref edges);

            if (input.allowTrimming)
            {
                ShadowUtility.ClipEdges(ref vertices, ref edges, ref shapeStarts, ref isClosed,
                                        result.trimEdge, out var clippedVertices,
                                        out var clippedEdges, out var clippedStarts);
                EmitOrClear(ref input, ref result, clippedVertices, clippedEdges, clippedStarts);
                clippedVertices.Dispose();
                clippedEdges.Dispose();
                clippedStarts.Dispose();
            }
            else
            {
                EmitOrClear(ref input, ref result, vertices, edges, shapeStarts);
            }

            result.uploadMesh = true;

            if (disposeVertices)
                vertices.Dispose();
            edges.Dispose();
            shapeStarts.Dispose();
            isClosed.Dispose();
        }

        void BuildDirect(ref ShadowShapeInput input, ref ShadowGeometryResult result)
        {
            result.isTransformable = !input.inWorldSpace;

            NativeArray<ShadowEdge> edges = input.indices.Reinterpret<ShadowEdge>(sizeof(int));
            NativeArray<int> shapeStarts = new NativeArray<int>(1, Allocator.Temp);
            shapeStarts[0] = 0;

            EmitOrClear(ref input, ref result, input.vertices, edges, shapeStarts);

            result.uploadMesh = true;
            shapeStarts.Dispose();
        }

        void EmitOrClear(ref ShadowShapeInput input, ref ShadowGeometryResult result,
                         NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges,
                         NativeArray<int> shapeStarts)
        {
            if (shapeStarts.Length == 0 || edges.Length < 3)
            {
                // Clipped away to nothing, or too few edges to form a ring. Drop the geometry rather
                // than leaving the previous mesh rendering. Clear() marks the mesh dirty itself, so
                // markDirty is deliberately not set here.
                result.localBounds = new Bounds();
                result.localBoundsAssigned = true;
                result.cleared = true;
                return;
            }

            Emit(ref input, ref result, vertices, edges, shapeStarts);
            result.localBoundsAssigned = true;
            result.markDirty = true;
        }

        // ------------------------------------------------------------------------------------
        // Emission
        // ------------------------------------------------------------------------------------

        void Emit(ref ShadowShapeInput input, ref ShadowGeometryResult result,
                  NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges,
                  NativeArray<int> shapeStarts)
        {
            // Clamped again here even though ShadowMesh2D.fanSegments clamps: the value can also
            // arrive straight off disk, and zero is not merely coarse but wrong -- see
            // ShadowMesh2D.k_MinFanSegments.
            int segments = Mathf.Max(ShadowMesh2D.k_MinFanSegments, input.fanSegments);

            int shapeCount = shapeStarts.Length;
            int perVertex = VerticesPerOutlineVertex(segments);
            int perVertexTris = TrianglesPerOutlineVertex(segments);

            int ringTotal = 0;
            for (int s = 0; s < shapeCount; s++)
                ringTotal += RingLength(s, edges.Length, shapeStarts);

            int mainVertexCount = ringTotal * perVertex;
            int mainIndexCount = ringTotal * perVertexTris * 3;

            // Interior fill, tessellated up front so the total is known before allocating. Needed
            // only by the caster passes -- Self, UnshadowMark and UnshadowUnmark read POSITION alone,
            // and every projected role shares POSITION with the outline vertex, so without this the
            // caster passes would draw a wholly degenerate mesh and UnshadowMark would mark no
            // stencil. The Collider provider turns createInteriorGeometry on exactly when there is no
            // Renderer, which is exactly when those passes draw this mesh.
            NativeArray<float2> interiorVertices = default;
            NativeArray<int> interiorIndices = default;
            int interiorVertexCount = 0, interiorIndexCount = 0;
            if (input.createInteriorGeometry)
            {
                TessellateInterior(vertices, edges, out interiorVertices, out interiorIndices,
                                   out interiorVertexCount, out interiorIndexCount);
            }

            int vertexCount = mainVertexCount + interiorVertexCount;
            int indexCount = mainIndexCount + interiorIndexCount;

            var floats = new NativeArray<float>(vertexCount * k_FloatsPerVertex,
                                                Allocator.Persistent,
                                                NativeArrayOptions.UninitializedMemory);
            var payloads = floats.Reinterpret<SoftShadowMeshVertex>(sizeof(float));
            var indices = new NativeArray<int>(indexCount, Allocator.Persistent,
                                               NativeArrayOptions.UninitializedMemory);

            int vertexBase = 0;
            int indexHead = 0;
            for (int s = 0; s < shapeCount; s++)
            {
                int start = shapeStarts[s];
                int n = RingLength(s, edges.Length, shapeStarts);
                if (n < 3)
                    continue;

                EmitShape(vertices, edges, start, n, segments, perVertex, payloads, vertexBase,
                          indices, ref indexHead);
                vertexBase += n * perVertex;
            }

            // Interior fill appended after the projected geometry, reusing the Outline role so the
            // vertex program needs no extra branch: Outline returns the unprojected position at full
            // opacity, which is exactly what a caster-pass fill wants.
            for (int i = 0; i < interiorVertexCount; i++)
            {
                // Neighbours are set to the vertex itself and the winding to 1 rather than left
                // uninitialised: the Outline role returns before reading either, but a zero-length
                // neighbour difference is the safe value if that ever stops being true, and the
                // buffer is allocated uninitialised.
                float2 p = interiorVertices[i];
                payloads[vertexBase + i] = MakePayload(p, p, p, p, p, 1.0f, k_RoleOutline, 0.0f);
            }
            for (int i = 0; i < interiorIndexCount; i++)
                indices[indexHead + i] = vertexBase + interiorIndices[i];
            indexHead += interiorIndexCount;

            if (interiorVertices.IsCreated) interiorVertices.Dispose();
            if (interiorIndices.IsCreated) interiorIndices.Dispose();

            // Ownership travels in and back out through the result: dispose what we were handed.
            if (result.customVertices.IsCreated)
                result.customVertices.Dispose();
            if (result.indices.IsCreated)
                result.indices.Dispose();
            if (result.vertices.IsCreated)
                result.vertices.Dispose();

            result.customVertices = floats;
            result.customVertexCount = vertexCount;
            result.vertices = default;
            result.indices = indices;
            result.localBounds = OutlineBounds(vertices, edges);
        }

        static int RingLength(int shape, int edgeCount, NativeArray<int> shapeStarts)
        {
            int start = shapeStarts[shape];
            int end = shape + 1 < shapeStarts.Length ? shapeStarts[shape + 1] : edgeCount;
            return end - start;
        }

        void EmitShape(NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges,
                       int edgeStart, int n, int segments, int perVertex,
                       NativeArray<SoftShadowMeshVertex> payloads, int vertexBase,
                       NativeArray<int> indices, ref int indexHead)
        {
            // Winding sign from the signed area of the ring, matching sim_shader.build_mesh. Constant
            // per shape, which is why the shader takes it as a uniform rather than a payload float.
            double area = 0.0;
            for (int i = 0; i < n; i++)
            {
                float2 a = Ring(vertices, edges, edgeStart, n, i);
                float2 b = Ring(vertices, edges, edgeStart, n, i + 1);
                area += (double)a.x * b.y - (double)b.x * a.y;
            }
            float winding = area < 0.0 ? -1.0f : 1.0f;

            for (int i = 0; i < n; i++)
            {
                float2 v = Ring(vertices, edges, edgeStart, n, i);
                float2 prev = Ring(vertices, edges, edgeStart, n, i - 1);
                float2 next = Ring(vertices, edges, edgeStart, n, i + 1);
                float2 prev2 = Ring(vertices, edges, edgeStart, n, i - 2);
                float2 next2 = Ring(vertices, edges, edgeStart, n, i + 2);

                int b = vertexBase + i * perVertex;

                payloads[b + SlotOf(k_RoleOutline, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleOutline, 0.0f);
                payloads[b + SlotOf(k_RoleHard, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleHard, 0.0f);
                payloads[b + SlotOf(k_RoleHardOpq, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleHardOpq, 0.0f);
                payloads[b + SlotOf(k_RoleBandIn, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleBandIn, 0.0f);
                payloads[b + SlotOf(k_RoleBandOut, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleBandOut, 0.0f);
                payloads[b + SlotOf(k_RoleFinL, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleFinL, 0.0f);
                payloads[b + SlotOf(k_RoleFinR, 0, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleFinR, 0.0f);

                // fanParam is baked as k / segments rather than raw k, so the shader never needs the
                // segment count. With the count being per caster, a uniform could otherwise desync
                // from the mesh on a per-caster basis.
                for (int k = 0; k <= segments; k++)
                {
                    float t = (float)k / segments;
                    payloads[b + SlotOf(k_RoleJoinIn, k, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleJoinIn, t);
                    payloads[b + SlotOf(k_RoleJoinOut, k, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleJoinOut, t);
                    payloads[b + SlotOf(k_RoleConvex, k, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleConvex, t);
                    payloads[b + SlotOf(k_RoleConcave, k, segments)] = MakePayload(v, prev, next, prev2, next2, winding, k_RoleConcave, t);
                }
            }

            // Indices. Body and band are per edge; fin and the three fans are per vertex.
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int bi = vertexBase + i * perVertex;
                int bj = vertexBase + j * perVertex;

                int oa = bi + SlotOf(k_RoleOutline, 0, segments);
                int ob = bj + SlotOf(k_RoleOutline, 0, segments);
                int ha = bi + SlotOf(k_RoleHard, 0, segments);
                int hb = bj + SlotOf(k_RoleHard, 0, segments);

                // body quad
                Tri(indices, ref indexHead, oa, ha, hb);
                Tri(indices, ref indexHead, hb, ob, oa);

                // band quad. Its inner corners are (0,1) and the body's are (0,0), so they cannot
                // share a vertex -- hence HardOpq at the same position as Hard. Sharing them cost
                // max |diff| 1.0 over ~17000 pixels on a plain square (handoff v2 section 9b).
                int qa = bi + SlotOf(k_RoleHardOpq, 0, segments);
                int qb = bj + SlotOf(k_RoleHardOpq, 0, segments);
                int outA = bi + SlotOf(k_RoleBandOut, 0, segments);
                int inB = bj + SlotOf(k_RoleBandIn, 0, segments);
                Tri(indices, ref indexHead, qa, qb, inB);
                Tri(indices, ref indexHead, inB, outA, qa);
            }

            for (int i = 0; i < n; i++)
            {
                int b = vertexBase + i * perVertex;
                int o = b + SlotOf(k_RoleOutline, 0, segments);
                int h = b + SlotOf(k_RoleHardOpq, 0, segments);

                // fin
                Tri(indices, ref indexHead, o,
                    b + SlotOf(k_RoleFinL, 0, segments),
                    b + SlotOf(k_RoleFinR, 0, segments));

                // join fans. k = 0 coincides with the fin's exterior corner and starts no triangle.
                for (int k = 1; k <= segments; k++)
                {
                    Tri(indices, ref indexHead, h,
                        b + SlotOf(k_RoleJoinIn, k - 1, segments),
                        b + SlotOf(k_RoleJoinIn, k, segments));
                    Tri(indices, ref indexHead, h,
                        b + SlotOf(k_RoleJoinOut, k - 1, segments),
                        b + SlotOf(k_RoleJoinOut, k, segments));
                }

                for (int k = 0; k < segments; k++)
                {
                    Tri(indices, ref indexHead, h,
                        b + SlotOf(k_RoleConvex, k, segments),
                        b + SlotOf(k_RoleConvex, k + 1, segments));
                    Tri(indices, ref indexHead, h,
                        b + SlotOf(k_RoleConcave, k, segments),
                        b + SlotOf(k_RoleConcave, k + 1, segments));
                }
            }
        }

        static void Tri(NativeArray<int> indices, ref int head, int a, int b, int c)
        {
            indices[head++] = a;
            indices[head++] = b;
            indices[head++] = c;
        }

        // The i-th vertex of a closed ring, wrapping. Edges within a shape are ordered
        // (v0,v1),(v1,v2),...,(vn-1,v0), so taking v0 of each in turn walks the ring.
        static float2 Ring(NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges,
                           int edgeStart, int n, int i)
        {
            int wrapped = ((i % n) + n) % n;
            Vector3 p = vertices[edges[edgeStart + wrapped].v0];
            return new float2(p.x, p.y);
        }

        static SoftShadowMeshVertex MakePayload(float2 v, float2 prev, float2 next,
                                                float2 prev2, float2 next2, float winding,
                                                int role, float fanParam)
        {
            return new SoftShadowMeshVertex
            {
                position = new float3(v.x, v.y, 0.0f),
                tangent = new float4(role, fanParam, prev.x, prev.y),
                next = new float3(next.x, next.y, winding),
                neighbor2 = new float4(prev2.x, prev2.y, next2.x, next2.y),
            };
        }

        // Bounds from the outline, not from the emitted mesh. Every role shares POSITION with its
        // caster-space outline vertex and interior fill is tessellated inside the outline, so the
        // outline's AABB is exactly the mesh's extent -- exact and independent of the segment count.
        static Bounds OutlineBounds(NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges)
        {
            Vector3 first = vertices[edges[0].v0];
            float minX = first.x, minY = first.y, maxX = first.x, maxY = first.y;
            for (int e = 0; e < edges.Length; e++)
            {
                Vector3 p = vertices[edges[e].v0];
                minX = Mathf.Min(minX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
            }
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(minX, minY, 0.0f), new Vector3(maxX, maxY, 0.0f));
            return bounds;
        }

        // Mirrors ShadowUtility.GenerateInteriorMesh's tessellation. Not a call to it: that function
        // is Burst-typed to ShadowMeshVertex and appends into Legacy's buffers.
        static void TessellateInterior(NativeArray<Vector3> vertices, NativeArray<ShadowEdge> edges,
                                       out NativeArray<float2> outVertices,
                                       out NativeArray<int> outIndices,
                                       out int outVertexCount, out int outIndexCount)
        {
            int edgeCount = edges.Length;
            var tessInEdges = new NativeArray<int2>(edgeCount, Allocator.Temp,
                                                    NativeArrayOptions.UninitializedMemory);
            var tessInVertices = new NativeArray<float2>(edgeCount, Allocator.Temp,
                                                         NativeArrayOptions.ClearMemory);
            for (int i = 0; i < edgeCount; i++)
            {
                var edge = new int2(edges[i].v0, edges[i].v1);
                tessInEdges[i] = edge;
                Vector3 p = vertices[edge.x];
                tessInVertices[edge.x] = new float2(p.x, p.y);
            }

            outVertices = new NativeArray<float2>(edgeCount * 4, Allocator.Persistent,
                                                  NativeArrayOptions.UninitializedMemory);
            outIndices = new NativeArray<int>(edgeCount * 8, Allocator.Persistent,
                                              NativeArrayOptions.UninitializedMemory);
            var tessOutEdges = new NativeArray<int2>(edgeCount * 4, Allocator.Temp,
                                                     NativeArrayOptions.UninitializedMemory);
            outVertexCount = 0;
            outIndexCount = 0;

#if USING_2DCOMMON
            UnityEngine.U2D.Common.UTess.ModuleHandle.Tessellate(
                Allocator.Temp, tessInVertices, tessInEdges, ref outVertices, out outVertexCount,
                ref outIndices, out outIndexCount, ref tessOutEdges, out _, false);
#endif

            tessInEdges.Dispose();
            tessInVertices.Dispose();
            tessOutEdges.Dispose();
        }

        // ------------------------------------------------------------------------------------
        // Upload
        // ------------------------------------------------------------------------------------

        internal override void UploadMesh(ref Mesh mesh, in ShadowGeometryResult result)
        {
            if (mesh == null)
                mesh = new Mesh();

            if (!result.customVertices.IsCreated || result.customVertexCount == 0 ||
                !result.indices.IsCreated || result.indices.Length == 0)
            {
                mesh.Clear();
                return;
            }

            mesh.SetVertexBufferParams(result.customVertexCount, vertexLayout);

            // Reinterpret aliases the same allocation and shares its safety handle, so this view is
            // transient and must never be disposed -- ShadowMesh2D owns the float buffer.
            var typed = result.customVertices.Reinterpret<SoftShadowMeshVertex>(sizeof(float));
            mesh.SetVertexBufferData(typed, 0, 0, result.customVertexCount);

            mesh.SetIndexBufferParams(result.indices.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(result.indices, 0, 0, result.indices.Length);

            var subMesh = new SubMeshDescriptor(0, result.indices.Length);
            subMesh.bounds = result.localBounds;
            mesh.SetSubMesh(0, subMesh, MeshUpdateFlags.DontRecalculateBounds);
            mesh.subMeshCount = 1;
            mesh.bounds = result.localBounds;
        }

#if UNITY_EDITOR
        internal override void DrawPreviewOutline(Mesh mesh, Matrix4x4 previewMat, float trimDistance)
        {
            if (mesh == null || mesh.vertexCount == 0)
                return;

            // Decodes this generator's own encoding: an outline edge is one joining two vertices
            // whose role is Outline. Every other role shares POSITION with its outline vertex, so
            // filtering on the role rather than the position is what keeps this from drawing the
            // whole soup.
            Vector3[] positions = mesh.vertices;
            Vector4[] tangents = mesh.tangents;
            int[] triangles = mesh.triangles;
            if (tangents == null || tangents.Length != positions.Length)
                return;

            Handles.color = Color.white;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i], v1 = triangles[i + 1], v2 = triangles[i + 2];
                DrawIfOutlineEdge(positions, tangents, previewMat, v0, v1);
                DrawIfOutlineEdge(positions, tangents, previewMat, v1, v2);
                DrawIfOutlineEdge(positions, tangents, previewMat, v2, v0);
            }
        }

        static void DrawIfOutlineEdge(Vector3[] positions, Vector4[] tangents, Matrix4x4 previewMat,
                                      int a, int b)
        {
            if ((int)tangents[a].x != k_RoleOutline || (int)tangents[b].x != k_RoleOutline)
                return;
            if (positions[a] == positions[b])
                return;

            Handles.DrawAAPolyLine(4,
                new[] { previewMat.MultiplyPoint(positions[a]), previewMat.MultiplyPoint(positions[b]) });
        }
#endif
    }
}

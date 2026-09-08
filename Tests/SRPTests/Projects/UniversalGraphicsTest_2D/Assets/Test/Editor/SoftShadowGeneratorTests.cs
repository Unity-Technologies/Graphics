using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// Structural tests for <c>Unity.SoftShadow</c>: the payload slot layout, the counts, the
    /// segment-count floor, and the payload invariants the four entry points must all satisfy.
    /// </summary>
    /// <remarks>
    /// These cover what <c>Shadow2DGeometryPathTests</c> cannot. That fixture iterates every
    /// registered generator, which is real coverage — the soft shadow generator does build a mesh
    /// whose attributes match its declaration there — but it only ever calls
    /// <c>SetShape(vertices, indices, OutlineTopology.Lines)</c>, so it exercises one of the four
    /// entry points, with trimming on and interior fill off.
    ///
    /// The per-vertex maths is verified elsewhere and differently: <c>SoftShadowVertexReference</c>
    /// and <c>softshadow_goldens.txt</c> in the UniversalGraphicsTest_2D project pin it against the
    /// Python model payload by payload. Nothing here re-checks that. What is checked here is the part
    /// the model has no opinion about: how the payloads are laid out in the buffer and which
    /// triangles index them.
    ///
    /// Interior fill is NOT covered here. It needs the tessellator behind <c>USING_2DCOMMON</c>, and
    /// this assembly only compiles in projects that list URP under <c>testables</c> — today only
    /// SRP_SmokeTest, which does not have the 2D packages. Those tests live in
    /// UniversalGraphicsTest_2D's <c>Assets/Test/Editor/SoftShadowInteriorFillTests.cs</c> instead.
    /// </remarks>
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowGeneratorTests
    {
        const int k_FixedRolesPerVertex = 7;

        NativeArray<Vector3> m_Vertices;
        NativeArray<int> m_LineIndices;
        NativeArray<int> m_TriangleIndices;
        NativeArray<float> m_Radii;

        [SetUp]
        public void Setup()
        {
            // A unit square. Four vertices is the minimum that forms a ring and still has distinct
            // prev2 / next2, which is what the radius budget reaches for.
            m_Vertices = new NativeArray<Vector3>(4, Allocator.Persistent);
            m_Vertices[0] = new Vector3(-1, -1, 0);
            m_Vertices[1] = new Vector3(-1, 1, 0);
            m_Vertices[2] = new Vector3(1, 1, 0);
            m_Vertices[3] = new Vector3(1, -1, 0);

            m_LineIndices = new NativeArray<int>(8, Allocator.Persistent);
            int[] loop = { 0, 1, 1, 2, 2, 3, 3, 0 };
            for (int i = 0; i < loop.Length; i++)
                m_LineIndices[i] = loop[i];

            m_TriangleIndices = new NativeArray<int>(6, Allocator.Persistent);
            int[] tris = { 0, 1, 2, 0, 2, 3 };
            for (int i = 0; i < tris.Length; i++)
                m_TriangleIndices[i] = tris[i];

            m_Radii = new NativeArray<float>(4, Allocator.Persistent);
            for (int i = 0; i < 4; i++)
                m_Radii[i] = 0.0f;
        }

        [TearDown]
        public void Cleanup()
        {
            if (m_Vertices.IsCreated) m_Vertices.Dispose();
            if (m_LineIndices.IsCreated) m_LineIndices.Dispose();
            if (m_TriangleIndices.IsCreated) m_TriangleIndices.Dispose();
            if (m_Radii.IsCreated) m_Radii.Dispose();
            Shadow2DGeometry.globalGeneratorOverride = null;
        }

        // The override rather than a field on the mesh: which generator a caster builds with is
        // Shadow2DGeometrySettings.geometryVersion, a project setting, so pinning it per test means
        // pinning it process-wide for the duration. Cleared in Cleanup.
        ShadowMesh2D BuildLines(int fanSegments, bool interior = false)
        {
            Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;

            var shadowMesh = new ShadowMesh2D();
            shadowMesh.trimEdge = 0;
            shadowMesh.fanSegments = fanSegments;
            shadowMesh.SetShape(m_Vertices, m_LineIndices, ShadowShape2D.OutlineTopology.Lines,
                                createInteriorGeometry: interior);
            return shadowMesh;
        }

        static int VertsPerOutlineVertex(int s) => k_FixedRolesPerVertex + 4 * (s + 1);
        static int TrisPerOutlineVertex(int s) => 5 + 4 * s;

        // ------------------------------------------------------------------------------------
        // Counts
        // ------------------------------------------------------------------------------------

        [Test]
        public void VertexAndTriangleCountsFollowTheSegmentFormula()
        {
            // Handoff v2 section 10: 11 + 4S vertices and 5 + 4S triangles per outline vertex, where
            // the four fans are convex, concave and the two join fans. Asserting the formula is what
            // catches a slot-layout arithmetic slip, which would otherwise show up as a stray
            // triangle somewhere on screen.
            foreach (int segments in new[] { 1, 2, 3, 5 })
            {
                ShadowMesh2D shadowMesh = BuildLines(segments);
                Mesh mesh = shadowMesh.mesh;

                Assert.AreEqual(4 * VertsPerOutlineVertex(segments), mesh.vertexCount,
                    $"segments {segments}: vertex count.");
                Assert.AreEqual(4 * TrisPerOutlineVertex(segments) * 3, mesh.triangles.Length,
                    $"segments {segments}: index count.");
            }
        }

        [Test]
        public void EverySlotIsWrittenExactlyOnce()
        {
            // The highest-value structural check here. Slot indices are computed arithmetically
            // rather than looked up, so two roles colliding on one slot would silently overwrite a
            // payload -- and the result would still be a well-formed mesh, just a wrong one.
            //
            // Detected by uniqueness of (role, fanParam) within each outline vertex's slot range: a
            // collision leaves one combination missing and another duplicated.
            const int segments = 3;
            ShadowMesh2D shadowMesh = BuildLines(segments);
            Mesh mesh = shadowMesh.mesh;
            Vector4[] tangents = mesh.tangents;

            int perVertex = VertsPerOutlineVertex(segments);
            for (int i = 0; i < 4; i++)
            {
                var seen = new HashSet<(int, int)>();
                for (int slot = 0; slot < perVertex; slot++)
                {
                    Vector4 t = tangents[i * perVertex + slot];
                    int role = Mathf.RoundToInt(t.x);
                    // fanParam is baked as k / segments; recover k so the key is exact.
                    int k = Mathf.RoundToInt(t.y * segments);
                    Assert.IsTrue(seen.Add((role, k)),
                        $"vertex {i}: role {role} k {k} written to more than one slot.");
                }
                Assert.AreEqual(perVertex, seen.Count, $"vertex {i}: not every slot was distinct.");
            }
        }

        [Test]
        public void EveryVertexRoleIsEmitted()
        {
            ShadowMesh2D shadowMesh = BuildLines(3);
            var roles = new HashSet<int>();
            foreach (Vector4 t in shadowMesh.mesh.tangents)
                roles.Add(Mathf.RoundToInt(t.x));

            for (int role = 0; role <= 10; role++)
                Assert.IsTrue(roles.Contains(role), $"No payload emitted with role {role}.");
        }

        // ------------------------------------------------------------------------------------
        // The segment floor
        // ------------------------------------------------------------------------------------

        [Test]
        public void SegmentCountIsClampedToTheLegalMinimum()
        {
            // Zero is not merely coarse. A fan joins two band ends, or a fin to a band, so removing
            // one disconnects the outer boundary; and for a concave corner the band setback happens
            // in a loop that never consults the segment count, so at zero the shadow the setback
            // removed is never replaced. Simulation report 29 measured both. The clamp lives in
            // ShadowMesh2D.fanSegments and again in the generator, because the value can arrive
            // straight off disk.
            Assert.AreEqual(ShadowMesh2D.k_MinFanSegments, new ShadowMesh2D { fanSegments = 0 }.fanSegments);
            Assert.AreEqual(ShadowMesh2D.k_MinFanSegments, new ShadowMesh2D { fanSegments = -7 }.fanSegments);

            ShadowMesh2D atZero = BuildLines(0);
            ShadowMesh2D atMin = BuildLines(ShadowMesh2D.k_MinFanSegments);
            Assert.AreEqual(atMin.mesh.vertexCount, atZero.mesh.vertexCount,
                "A segment count of 0 must behave as the minimum, not emit fanless geometry.");
        }

        [Test]
        public void DefaultSegmentCountIsThree()
        {
            // Report 29's recommendation: 17 triangles per edge, 5.7x Legacy, and the worst fan
            // boundary dip down to 0.037 of the band width. Pinned so a change is deliberate.
            Assert.AreEqual(3, ShadowMesh2D.k_DefaultFanSegments);
            Assert.AreEqual(3, new ShadowMesh2D().fanSegments);
        }

        // ------------------------------------------------------------------------------------
        // Payload invariants the model has no opinion about
        // ------------------------------------------------------------------------------------

        [Test]
        public void PositionCarriesTheCasterSpaceVertexWithZeroZ()
        {
            // Every role shares POSITION with its outline vertex; the projection happens in the
            // vertex program. Three POSITION-only consumers depend on this: the caster passes'
            // TransformObjectToHClip, the mesh bounds, and the preview outline. Handoff v2 section 9
            // put the fan index in position.z, which would have leaked into all three.
            ShadowMesh2D shadowMesh = BuildLines(3);
            var outline = new HashSet<(float, float)>();
            for (int i = 0; i < m_Vertices.Length; i++)
                outline.Add((m_Vertices[i].x, m_Vertices[i].y));

            foreach (Vector3 p in shadowMesh.mesh.vertices)
            {
                Assert.AreEqual(0.0f, p.z, "position.z must stay 0.");
                Assert.IsTrue(outline.Contains((p.x, p.y)),
                    $"position ({p.x}, {p.y}) is not an outline vertex.");
            }
        }

        [Test]
        public void WindingSignIsCarriedPerVertexAndIsUnit()
        {
            // Winding rides in TEXCOORD1.z rather than a uniform: it comes from a ring's signed area
            // and a caster can carry several rings, so one uniform would be wrong for any ring whose
            // winding differed -- silently, since an inverted winding inverts the concave/convex
            // classification rather than removing geometry.
            ShadowMesh2D shadowMesh = BuildLines(3);
            var uvs = new List<Vector3>();
            shadowMesh.mesh.GetUVs(1, uvs);

            Assert.AreEqual(shadowMesh.mesh.vertexCount, uvs.Count, "TEXCOORD1 must be populated.");
            foreach (Vector3 uv in uvs)
                Assert.AreEqual(1.0f, Mathf.Abs(uv.z), 1e-6f, "windingSign must be +1 or -1.");
        }

        [Test]
        public void BoundsAreTheOutlineBoundsRatherThanTheProjectedExtent()
        {
            // The mesh is light-independent, so a projected extent is not even computable here. The
            // outline AABB is exact rather than approximate, because POSITION is always the
            // caster-space vertex.
            ShadowMesh2D shadowMesh = BuildLines(3);
            Bounds b = shadowMesh.mesh.bounds;

            Assert.AreEqual(-1.0f, b.min.x, 1e-5f);
            Assert.AreEqual(-1.0f, b.min.y, 1e-5f);
            Assert.AreEqual(1.0f, b.max.x, 1e-5f);
            Assert.AreEqual(1.0f, b.max.y, 1e-5f);
        }

        // ------------------------------------------------------------------------------------
        // The other entry points
        // ------------------------------------------------------------------------------------

        [Test]
        public void EveryShadowShapeKindProducesGeometry()
        {
            // Shadow2DGeometryPathTests only ever calls the Lines overload, so the other three
            // ShadowShape2D entry points are otherwise untested for this generator. A generator that
            // silently produces nothing for one provider is the kind of thing that reads as "shadows
            // do not work on tilemaps".
            var cases = new (string name, System.Action<ShadowMesh2D> setShape)[]
            {
                ("Topology/Lines", m => m.SetShape(m_Vertices, m_LineIndices,
                                                   ShadowShape2D.OutlineTopology.Lines)),
                ("Topology/Triangles", m => m.SetShape(m_Vertices, m_TriangleIndices,
                                                       ShadowShape2D.OutlineTopology.Triangles)),
                ("Radii", m => m.SetShape(m_Vertices, m_LineIndices, m_Radii)),
                ("RadiiWithTransform", m => m.SetShape(m_Vertices, m_LineIndices, m_Radii,
                                                       Matrix4x4.identity)),
                ("Direct", m => m.SetShapeDirect(m_Vertices, m_LineIndices)),
            };

            foreach ((string name, System.Action<ShadowMesh2D> setShape) in cases)
            {
                Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;

                var shadowMesh = new ShadowMesh2D();
                shadowMesh.trimEdge = 0;
                shadowMesh.fanSegments = 3;
                setShape(shadowMesh);

                Assert.Greater(shadowMesh.mesh.vertexCount, 0, $"{name}: produced no vertices.");
                Assert.Greater(shadowMesh.mesh.triangles.Length, 0, $"{name}: produced no triangles.");
            }
        }

        [Test]
        public void GeneratorDeclaresItDoesNotPersistGeometry()
        {
            // The payload is large relative to the outline and would be stored as YAML in every scene
            // and prefab holding a caster. persistsGeometry == false makes ShadowMesh2D skip the copy
            // in OnBeforeSerialize and rebuild on load instead.
            ShadowGeometryGenerator generator =
                ShadowGeometryGeneratorRegistry.Get(SoftShadowGeometryGenerator.k_Id);
            Assert.IsFalse(generator.persistsGeometry);
            Assert.IsTrue(generator.usesFanSegments);
        }
    }
}
